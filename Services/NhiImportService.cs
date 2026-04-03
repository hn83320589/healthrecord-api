using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    // r7.10 values to skip (non-clinical items)
    private static readonly string[] SkipPatterns =
        ["Appearance", "顏色", "混濁度", "GENERAL URINE EXAMINATION"];

    public async Task<NhiImportResponse> ImportAsync(int userId, string json, string? fileName)
    {
        // Strip BOM if present
        json = json.TrimStart('\uFEFF');

        // SHA256 hash check for duplicate imports
        var fileHash = ComputeSha256(json);
        var duplicate = await db.NhiImportLogs
            .AnyAsync(l => l.UserId == userId && l.FileHash == fileHash);
        if (duplicate)
            throw new InvalidOperationException("This file has already been imported.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var dataDateStr = NhiJsonParser.GetDataDate(root);
        var dataDate = DateOnly.TryParseExact(dataDateStr, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dd) ? dd : (DateOnly?)null;

        var visitGroups = NhiJsonParser.ParseR1(root);
        var labGroups = NhiJsonParser.ParseR7(root);

        // Create import log first
        var log = new NhiImportLog
        {
            UserId = userId,
            FileHash = fileHash,
            FileName = fileName,
            DataDate = dataDate,
            ImportedAt = DateTime.UtcNow
        };
        db.NhiImportLogs.Add(log);
        await db.SaveChangesAsync();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Pre-load UserLabItems for O(1) lookup
            var userItemDict = await db.UserLabItems
                .Where(i => i.UserId == userId)
                .ToDictionaryAsync(i => (Normalize(i.ItemCode), Normalize(i.ItemName)));

            int visitCount = 0, medCount = 0, labCount = 0;
            int skippedLabCount = 0, duplicateLabCount = 0, newItemCount = 0;
            var allDates = new List<DateOnly>();

            // ── r1 → HealthRecords(visit) + VisitDetails ──────────────
            // visitDetailByKey: track created VisitDetails for medication linking
            var visitDetailByKey = new Dictionary<string, VisitDetail>();

            foreach (var visit in visitGroups)
            {
                var visitDateOnly = DateOnly.FromDateTime(visit.VisitDate);
                allDates.Add(visitDateOnly);

                var record = new HealthRecordEntity
                {
                    UserId = userId,
                    RecordType = "visit",
                    RecordedAt = visit.VisitDate,
                    Source = "nhi_import",
                    NhiImportLogId = log.Id,
                    NhiInstitution = visit.InstitutionName,
                    NhiInstitutionCode = visit.InstitutionCode,
                    NhiVisitSeq = visit.VisitSeq,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.HealthRecords.Add(record);
                await db.SaveChangesAsync();

                var visitDetail = new VisitDetail
                {
                    HealthRecordId = record.Id,
                    VisitTypeCode = GetVisitTypeField(visit),
                    DiagnosisCode1 = visit.PrimaryIcdCode,
                    DiagnosisName1 = visit.PrimaryDiagnosis,
                    CopaymentCode = GetCopaymentCode(visit),
                    MedicalCost = GetMedicalCost(visit),
                    NhiRawData = SerializeVisitRaw(visit)
                };
                // Parse secondary diagnoses into VisitDetail fields
                FillSecondaryDiagnoses(visitDetail, visit.SecondaryDiagnosesJson);

                db.VisitDetails.Add(visitDetail);
                await db.SaveChangesAsync();
                visitCount++;

                // Key for linking medications to their visit
                var visitKey = $"{visit.VisitDate:yyyyMMdd}|{visit.InstitutionCode}|{visit.VisitSeq}";
                visitDetailByKey[visitKey] = visitDetail;

                // ── r1_1 → classify and create medications ────────────
                foreach (var med in visit.MedicationItems)
                {
                    if (!IsDrug(med.Code)) continue; // skip exam/service codes

                    var medRecord = new HealthRecordEntity
                    {
                        UserId = userId,
                        RecordType = "medication",
                        RecordedAt = visit.VisitDate,
                        Source = "nhi_import",
                        NhiImportLogId = log.Id,
                        NhiInstitution = visit.InstitutionName,
                        NhiInstitutionCode = visit.InstitutionCode,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.HealthRecords.Add(medRecord);
                    await db.SaveChangesAsync();

                    db.MedicationDetails.Add(new MedicationDetail
                    {
                        HealthRecordId = medRecord.Id,
                        VisitDetailId = visitDetail.Id,
                        MedicationName = med.DrugName,
                        NhiDrugCode = med.Code,
                        Quantity = med.Quantity,
                        Copayment = med.Days.HasValue ? med.Days.Value : null,
                        IsActive = false
                    });
                    medCount++;
                }
            }

            // ── r7 → HealthRecords(lab_result) + LabResultDetails ─────
            // Group labs by (date+institution) → one HealthRecord per group
            // Dedup within each group by (r7.5+r7.8+r7.10+r7.11)
            var dedupSet = new HashSet<string>();

            foreach (var labGroup in labGroups)
            {
                var labDateOnly = DateOnly.FromDateTime(labGroup.VisitDate);
                allDates.Add(labDateOnly);

                // Create one HealthRecord per lab group
                HealthRecordEntity? labRecord = null;
                var labDetails = new List<LabResultDetail>();

                foreach (var item in labGroup.Items)
                {
                    if (string.IsNullOrEmpty(item.NhiCode)) continue;

                    var normalizedItemName = Normalize(item.NhiItemName ?? "");

                    // Filter non-clinical items
                    if (ShouldSkipLabItem(normalizedItemName))
                    {
                        skippedLabCount++;
                        continue;
                    }

                    // Dedup by (date+code+name+value)
                    var dedupKey = $"{labGroup.VisitDate:yyyyMMdd}|{item.NhiCode}|{normalizedItemName}|{item.RawValue}";
                    if (!dedupSet.Add(dedupKey))
                    {
                        duplicateLabCount++;
                        continue;
                    }

                    // Lazily create the HealthRecord for this lab group
                    if (labRecord == null)
                    {
                        labRecord = new HealthRecordEntity
                        {
                            UserId = userId,
                            RecordType = "lab_result",
                            RecordedAt = labGroup.VisitDate,
                            Source = "nhi_import",
                            NhiImportLogId = log.Id,
                            NhiInstitutionCode = labGroup.InstitutionCode,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.HealthRecords.Add(labRecord);
                        await db.SaveChangesAsync();
                    }

                    var itemCode = item.NhiCode!;
                    var itemName = item.NhiItemName ?? "";
                    var normalizedCode = Normalize(itemCode);
                    var normalizedName = Normalize(itemName);
                    var lookupKey = (normalizedCode, normalizedName);

                    // Lookup or auto-create UserLabItem
                    if (!userItemDict.TryGetValue(lookupKey, out var userItem))
                    {
                        userItem = new UserLabItem
                        {
                            UserId = userId,
                            ItemCode = itemCode,
                            ItemName = itemName,
                            Unit = "",
                            Category = "其他",
                            IsPreset = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.UserLabItems.Add(userItem);
                        await db.SaveChangesAsync();
                        userItemDict[lookupKey] = userItem;
                        newItemCount++;
                    }

                    labDetails.Add(new LabResultDetail
                    {
                        HealthRecordId = labRecord.Id,
                        UserLabItemId = userItem.Id,
                        ItemCode = itemCode,
                        ItemName = itemName,
                        IsNumeric = item.IsNumeric,
                        ValueNumeric = item.NumericValue,
                        ValueText = item.IsNumeric ? null : item.RawValue,
                        Unit = userItem.Unit,
                        ReferenceRange = item.RawRange,
                        NhiOrderName = null, // r7.9 not yet in parser output
                        NhiRawValue = item.RawValue
                    });
                    labCount++;
                }

                if (labDetails.Count > 0)
                {
                    db.LabResultDetails.AddRange(labDetails);
                }
            }

            // Update log counts
            log.RecordCount = visitCount + medCount + labCount;
            log.VisitCount = visitCount;
            log.MedicationCount = medCount;
            log.LabCount = labCount;
            log.SkippedLabCount = skippedLabCount;
            log.DuplicateLabCount = duplicateLabCount;
            log.NewItemCount = newItemCount;

            if (allDates.Count > 0)
            {
                log.DateRangeStart = allDates.Min();
                log.DateRangeEnd = allDates.Max();
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new NhiImportResponse(
                log.Id, log.ImportedAt, log.DataDate,
                log.DateRangeStart, log.DateRangeEnd,
                log.RecordCount, log.VisitCount, log.MedicationCount,
                log.LabCount, log.SkippedLabCount, log.DuplicateLabCount,
                log.NewItemCount);
        }
        catch
        {
            await transaction.RollbackAsync();
            // Clean up the log entry created before the transaction
            db.NhiImportLogs.Remove(log);
            await db.SaveChangesAsync();
            throw;
        }
    }

    public async Task<List<NhiImportLogResponse>> GetLogsAsync(int userId)
    {
        return await db.NhiImportLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ImportedAt)
            .Select(l => new NhiImportLogResponse(
                l.Id, l.ImportedAt, l.FileName, l.DataDate,
                l.DateRangeStart, l.DateRangeEnd,
                l.RecordCount, l.VisitCount, l.MedicationCount,
                l.LabCount, l.SkippedLabCount, l.DuplicateLabCount,
                l.NewItemCount))
            .ToListAsync();
    }

    public async Task RevokeAsync(int userId, int logId)
    {
        var log = await db.NhiImportLogs
            .FirstOrDefaultAsync(l => l.Id == logId && l.UserId == userId)
            ?? throw new KeyNotFoundException("Import log not found.");

        // Delete all HealthRecords with this import log — CASCADE handles details
        await db.HealthRecords
            .Where(h => h.NhiImportLogId == logId)
            .ExecuteDeleteAsync();

        // Don't delete auto-created UserLabItems
        db.NhiImportLogs.Remove(log);
        await db.SaveChangesAsync();
    }

    // ── Drug classification ─────────────────────────────────────────

    /// <summary>
    /// Drug codes are 10 chars starting with A/B/C/N.
    /// Exam codes are <=6 chars with prefix 06-12. Service codes have prefix 00-05.
    /// </summary>
    private static bool IsDrug(string? code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        if (code.Length == 10 && char.IsLetter(code[0]))
        {
            var first = char.ToUpperInvariant(code[0]);
            return first is 'A' or 'B' or 'C' or 'N';
        }
        return false;
    }

    // ── Lab filtering ───────────────────────────────────────────────

    private static bool ShouldSkipLabItem(string normalizedItemName)
    {
        foreach (var pattern in SkipPatterns)
        {
            if (normalizedItemName.Contains(Normalize(pattern), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ── Full-width/half-width normalization ──────────────────────────

    private static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            // Full-width ASCII range: U+FF01 to U+FF5E → U+0021 to U+007E
            if (c >= '\uFF01' && c <= '\uFF5E')
                sb.Append((char)(c - 0xFEE0));
            // Full-width space
            else if (c == '\u3000')
                sb.Append(' ');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    // ── SHA256 ──────────────────────────────────────────────────────

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    // ── Visit field extraction helpers ──────────────────────────────

    private static string? GetVisitTypeField(NhiVisitGroup visit)
    {
        // visit_type_code would need to come from r1.1
        // The current parser doesn't extract r1.1 separately, so return null
        return null;
    }

    private static string? GetCopaymentCode(NhiVisitGroup visit)
    {
        // In the current parser, Copay is parsed as int from r1.12
        // In v2, r1.12 is copayment_code (string), but parser returns int
        // Return as string for now
        return visit.Copay?.ToString();
    }

    private static decimal? GetMedicalCost(NhiVisitGroup visit)
    {
        // r1.13 = TotalPoints in old parser = medical_cost in v2
        return visit.TotalPoints.HasValue ? visit.TotalPoints.Value : null;
    }

    private static string? SerializeVisitRaw(NhiVisitGroup visit)
    {
        // Store raw visit data for debug purposes
        return null;
    }

    private static void FillSecondaryDiagnoses(VisitDetail detail, string? secondaryJson)
    {
        if (string.IsNullOrEmpty(secondaryJson)) return;

        try
        {
            using var doc = JsonDocument.Parse(secondaryJson);
            var items = doc.RootElement.EnumerateArray().ToList();

            if (items.Count > 0)
            {
                detail.DiagnosisCode2 = GetJsonString(items[0], "code");
                detail.DiagnosisName2 = GetJsonString(items[0], "name");
            }
            if (items.Count > 1)
            {
                detail.DiagnosisCode3 = GetJsonString(items[1], "code");
                detail.DiagnosisName3 = GetJsonString(items[1], "name");
            }
            if (items.Count > 2)
            {
                detail.DiagnosisCode4 = GetJsonString(items[2], "code");
                detail.DiagnosisName4 = GetJsonString(items[2], "name");
            }
            if (items.Count > 3)
            {
                detail.DiagnosisCode5 = GetJsonString(items[3], "code");
                detail.DiagnosisName5 = GetJsonString(items[3], "name");
            }
        }
        catch
        {
            // Malformed JSON — ignore secondary diagnoses
        }
    }

    private static string? GetJsonString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }
}
