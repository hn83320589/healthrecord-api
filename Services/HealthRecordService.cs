using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.HealthRecord;
using HealthRecord.API.Models.DTOs.Lab;
using HealthRecord.API.Models.DTOs.Medication;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class HealthRecordService(AppDbContext db) : IHealthRecordService
{
    public async Task<PagedResult<HealthRecordResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to)
    {
        var query = db.HealthRecords.Where(h => h.UserId == userId);
        if (from.HasValue) query = query.Where(h => h.ClinicDate >= from.Value);
        if (to.HasValue) query = query.Where(h => h.ClinicDate <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.ClinicDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => Map(h))
            .ToListAsync();

        return new PagedResult<HealthRecordResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<HealthRecordDetailResponse> GetByIdAsync(int userId, int id)
    {
        var record = await db.HealthRecords
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new KeyNotFoundException("Health record not found.");

        var medications = await db.MedicationDetails
            .Where(m => m.HealthRecordId == id)
            .OrderBy(m => m.DrugName)
            .Select(m => MapMed(m))
            .ToListAsync();

        var labs = await db.LabResultDetails
            .Where(l => l.HealthRecordId == id)
            .OrderBy(l => l.ItemName)
            .Select(l => MapLab(l))
            .ToListAsync();

        return new HealthRecordDetailResponse(
            record.Id, record.ClinicDate, record.Hospital, record.HospitalCode,
            record.PrimaryIcdCode, record.PrimaryDiagnosis, record.SecondaryDiagnoses,
            record.Copay, record.TotalPoints, record.Source, record.Note, record.CreatedAt,
            medications, labs);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var record = await db.HealthRecords
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new KeyNotFoundException("Health record not found.");

        if (record.Source != "nhi_import")
            throw new InvalidOperationException("Only NHI-imported records can be deleted in Phase 1.");

        db.HealthRecords.Remove(record);
        await db.SaveChangesAsync();
    }

    private static HealthRecordResponse Map(Models.Entities.HealthRecord h) => new(
        h.Id, h.ClinicDate, h.Hospital, h.HospitalCode, h.PrimaryIcdCode,
        h.PrimaryDiagnosis, h.SecondaryDiagnoses, h.Copay, h.TotalPoints,
        h.Source, h.Note, h.CreatedAt);

    private static MedicationResponse MapMed(Models.Entities.MedicationDetail m) => new(
        m.Id, m.HealthRecordId, m.RecordedAt, m.DrugName, m.NhiDrugCode,
        m.Quantity, m.Days, m.DrugType, m.Source, m.Note, m.CreatedAt);

    private static LabResultResponse MapLab(Models.Entities.LabResultDetail l) => new(
        l.Id, l.HealthRecordId, l.RecordedAt, l.ItemName, l.ItemCode, l.Unit, l.Category,
        l.NormalMin, l.NormalMax, l.IsNumeric, l.ValueNumeric, l.ValueText, l.IsAbnormal,
        l.Source, l.Note, l.CreatedAt);
}
