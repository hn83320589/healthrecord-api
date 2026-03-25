using System.Text.Json;
using HealthRecord.API.Common.Constants;
using HealthRecord.API.Common.Helpers;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Nhi;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Services;

public class NhiImportService(AppDbContext db) : INhiImportService
{
    public async Task<NhiImportResponse> ImportAsync(int userId, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var dataDate = NhiJsonParser.GetDataDate(root);
        var visitGroups = NhiJsonParser.ParseR1(root);
        var labGroups   = NhiJsonParser.ParseR7(root);

        var allDates = visitGroups.Select(v => v.VisitDate)
            .Concat(labGroups.Select(l => l.VisitDate))
            .ToList();

        if (allDates.Count == 0)
            throw new ArgumentException("No valid dates found in NHI data.");

        var rangeStart = DateOnly.FromDateTime(allDates.Min());
        var rangeEnd   = DateOnly.FromDateTime(allDates.Max());
        var startDt    = rangeStart.ToDateTime(TimeOnly.MinValue);
        var endDt      = rangeEnd.ToDateTime(TimeOnly.MaxValue);

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Delete existing nhi_import records in the date range (新蓋舊)
            await db.HealthRecords
                .Where(h => h.UserId == userId && h.Source == "nhi_import"
                    && h.ClinicDate >= startDt && h.ClinicDate <= endDt)
                .ExecuteDeleteAsync();
            await db.BloodPressureDetails
                .Where(b => b.UserId == userId && b.Source == "nhi_import"
                    && b.RecordedAt >= startDt && b.RecordedAt <= endDt)
                .ExecuteDeleteAsync();
            await db.LabResultDetails
                .Where(l => l.UserId == userId && l.Source == "nhi_import"
                    && l.RecordedAt >= startDt && l.RecordedAt <= endDt)
                .ExecuteDeleteAsync();
            await db.MedicationDetails
                .Where(m => m.UserId == userId && m.Source == "nhi_import"
                    && m.RecordedAt >= startDt && m.RecordedAt <= endDt)
                .ExecuteDeleteAsync();

            // Create import log first (need the generated id)
            var log = new NhiImportLog
            {
                UserId = userId,
                ImportedAt = DateTime.UtcNow,
                DataDate = dataDate,
                DateRangeStart = rangeStart,
                DateRangeEnd = rangeEnd
            };
            db.NhiImportLogs.Add(log);
            await db.SaveChangesAsync();

            int healthRecordCount = 0, medCount = 0, labCount = 0, skippedLabs = 0;

            // r1 → HealthRecords, r1_1 → MedicationDetails
            foreach (var visit in visitGroups)
            {
                var healthRecord = new HealthRecordEntity
                {
                    UserId = userId,
                    ClinicDate = visit.VisitDate,
                    Hospital = visit.InstitutionName,
                    HospitalCode = visit.InstitutionCode,
                    VisitSeq = visit.VisitSeq,
                    PrimaryIcdCode = visit.PrimaryIcdCode,
                    PrimaryDiagnosis = visit.PrimaryDiagnosis,
                    SecondaryDiagnoses = visit.SecondaryDiagnosesJson,
                    Copay = visit.Copay,
                    TotalPoints = visit.TotalPoints,
                    Source = "nhi_import",
                    NhiImportLogId = log.Id,
                    CreatedAt = DateTime.UtcNow
                };
                db.HealthRecords.Add(healthRecord);
                await db.SaveChangesAsync(); // flush to get generated id
                healthRecordCount++;

                foreach (var med in visit.MedicationItems)
                {
                    db.MedicationDetails.Add(new MedicationDetail
                    {
                        UserId = userId,
                        HealthRecordId = healthRecord.Id,
                        NhiImportLogId = log.Id,
                        RecordedAt = visit.VisitDate,
                        DrugName = med.DrugName,
                        NhiDrugCode = med.Code,
                        Quantity = med.Quantity,
                        Days = med.Days,
                        DrugType = NhiJsonParser.ClassifyDrugType(med.Code),
                        Source = "nhi_import",
                        CreatedAt = DateTime.UtcNow
                    });
                    medCount++;
                }
            }

            // r7 → LabResultDetails (match against LabItemPresets)
            // HealthRecords are already in Local cache — no extra DB round trip needed
            foreach (var labGroup in labGroups)
            {
                var matchedRecord = db.HealthRecords.Local
                    .FirstOrDefault(h => h.UserId == userId
                        && h.NhiImportLogId == log.Id
                        && h.ClinicDate == labGroup.VisitDate
                        && (labGroup.InstitutionCode == null
                            || h.HospitalCode == labGroup.InstitutionCode));

                foreach (var item in labGroup.Items)
                {
                    var preset = LabItemPresets.FindByNhiCode(item.NhiCode, item.NhiItemName);
                    if (preset == null)
                    {
                        skippedLabs++;
                        continue;
                    }

                    var isAbnormal = DetermineAbnormal(item.NumericValue, preset.NormalMin, preset.NormalMax);

                    db.LabResultDetails.Add(new LabResultDetail
                    {
                        UserId = userId,
                        HealthRecordId = matchedRecord?.Id,
                        NhiImportLogId = log.Id,
                        RecordedAt = labGroup.VisitDate,
                        ItemName = preset.ItemName,
                        ItemCode = preset.ItemCode,
                        Unit = preset.Unit,
                        Category = preset.Category,
                        NormalMin = preset.NormalMin,
                        NormalMax = preset.NormalMax,
                        IsNumeric = item.IsNumeric,
                        ValueNumeric = item.NumericValue,
                        ValueText = item.IsNumeric ? null : item.RawValue,
                        IsAbnormal = isAbnormal,
                        NhiCode = item.NhiCode,
                        NhiItemName = item.NhiItemName,
                        NhiRawValue = item.RawValue,
                        NhiRawRange = item.RawRange,
                        Source = "nhi_import",
                        CreatedAt = DateTime.UtcNow
                    });
                    labCount++;
                }
            }

            log.HealthRecordCount = healthRecordCount;
            log.MedicationCount = medCount;
            log.LabCount = labCount;
            log.SkippedLabs = skippedLabs;
            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            return new NhiImportResponse(log.Id, log.ImportedAt, log.DataDate,
                log.DateRangeStart, log.DateRangeEnd,
                healthRecordCount, medCount, labCount, skippedLabs);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<NhiImportLogResponse>> GetLogsAsync(int userId)
    {
        return await db.NhiImportLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ImportedAt)
            .Select(l => new NhiImportLogResponse(
                l.Id, l.ImportedAt, l.DataDate, l.DateRangeStart, l.DateRangeEnd,
                l.HealthRecordCount, l.MedicationCount, l.LabCount, l.SkippedLabs))
            .ToListAsync();
    }

    public async Task RevokeAsync(int userId, int logId)
    {
        var log = await db.NhiImportLogs
            .FirstOrDefaultAsync(l => l.Id == logId && l.UserId == userId)
            ?? throw new KeyNotFoundException("Import log not found.");

        await db.HealthRecords.Where(h => h.NhiImportLogId == logId).ExecuteDeleteAsync();
        await db.BloodPressureDetails.Where(b => b.NhiImportLogId == logId).ExecuteDeleteAsync();
        await db.LabResultDetails.Where(l => l.NhiImportLogId == logId).ExecuteDeleteAsync();
        await db.MedicationDetails.Where(m => m.NhiImportLogId == logId).ExecuteDeleteAsync();

        db.NhiImportLogs.Remove(log);
        await db.SaveChangesAsync();
    }

    private static bool DetermineAbnormal(decimal? value, decimal? min, decimal? max)
    {
        if (value == null) return false;
        if (min.HasValue && value < min) return true;
        if (max.HasValue && value > max) return true;
        return false;
    }
}
