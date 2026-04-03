using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Medication;
using HealthRecord.API.Models.DTOs.Visit;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Services;

public class VisitService(AppDbContext db) : IVisitService
{
    public async Task<PagedResult<VisitResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to)
    {
        var query = db.VisitDetails
            .Include(v => v.HealthRecord)
            .Where(v => v.HealthRecord.UserId == userId);

        if (from.HasValue) query = query.Where(v => v.HealthRecord.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(v => v.HealthRecord.RecordedAt <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.HealthRecord.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<VisitResponse>
        {
            Items = items.Select(MapSummary),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<VisitDetailResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .Include(v => v.Medications)
                .ThenInclude(m => m.HealthRecord)
            .FirstOrDefaultAsync(v => v.Id == id && v.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Visit not found.");

        return MapDetail(entity);
    }

    public async Task<VisitResponse> CreateAsync(int userId, CreateVisitRequest request)
    {
        var record = new HealthRecordEntity
        {
            UserId = userId,
            RecordType = "visit",
            RecordedAt = request.RecordedAt,
            Note = request.Note,
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.HealthRecords.Add(record);
        await db.SaveChangesAsync();

        var detail = new VisitDetail
        {
            HealthRecordId = record.Id,
            Department = request.Department,
            DoctorName = request.DoctorName,
            DiagnosisCode1 = request.DiagnosisCode1,
            DiagnosisName1 = request.DiagnosisName1
        };
        db.VisitDetails.Add(detail);
        await db.SaveChangesAsync();

        detail.HealthRecord = record;
        return MapSummary(detail);
    }

    public async Task<VisitResponse> UpdateAsync(int userId, int id, UpdateVisitRequest request)
    {
        var entity = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .FirstOrDefaultAsync(v => v.Id == id && v.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Visit not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be updated.");

        entity.HealthRecord.RecordedAt = request.RecordedAt;
        entity.HealthRecord.Note = request.Note;
        entity.HealthRecord.UpdatedAt = DateTime.UtcNow;
        entity.Department = request.Department;
        entity.DoctorName = request.DoctorName;
        entity.DiagnosisCode1 = request.DiagnosisCode1;
        entity.DiagnosisName1 = request.DiagnosisName1;

        await db.SaveChangesAsync();
        return MapSummary(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .FirstOrDefaultAsync(v => v.Id == id && v.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Visit not found.");

        if (entity.HealthRecord.Source != "manual" && entity.HealthRecord.Source != "nhi_import")
            throw new InvalidOperationException("Record cannot be deleted.");

        db.HealthRecords.Remove(entity.HealthRecord); // CASCADE deletes detail
        await db.SaveChangesAsync();
    }

    private static VisitResponse MapSummary(VisitDetail v) => new(
        v.Id,
        v.HealthRecordId,
        v.HealthRecord.RecordedAt,
        v.HealthRecord.NhiInstitution,
        v.HealthRecord.NhiInstitutionCode,
        v.VisitType,
        v.VisitTypeCode,
        v.Department,
        v.DoctorName,
        v.DiagnosisCode1,
        v.DiagnosisName1,
        v.DiagnosisCode2,
        v.DiagnosisName2,
        v.DiagnosisCode3,
        v.DiagnosisName3,
        v.CopaymentCode,
        v.MedicalCost,
        v.HealthRecord.Source,
        v.HealthRecord.Note,
        v.HealthRecord.CreatedAt);

    private static VisitDetailResponse MapDetail(VisitDetail v) => new(
        v.Id,
        v.HealthRecordId,
        v.HealthRecord.RecordedAt,
        v.HealthRecord.NhiInstitution,
        v.HealthRecord.NhiInstitutionCode,
        v.VisitType,
        v.VisitTypeCode,
        v.Department,
        v.DoctorName,
        v.DiagnosisCode1,
        v.DiagnosisName1,
        v.DiagnosisCode2,
        v.DiagnosisName2,
        v.DiagnosisCode3,
        v.DiagnosisName3,
        v.DiagnosisCode4,
        v.DiagnosisName4,
        v.DiagnosisCode5,
        v.DiagnosisName5,
        v.CopaymentCode,
        v.MedicalCost,
        v.HealthRecord.Source,
        v.HealthRecord.Note,
        v.HealthRecord.CreatedAt,
        v.Medications.Select(m => new MedicationResponse(
            m.Id,
            m.HealthRecordId,
            m.HealthRecord.RecordedAt,
            m.MedicationName,
            m.GenericName,
            m.NhiDrugCode,
            m.Quantity,
            m.Copayment,
            m.Dosage,
            m.Frequency,
            m.Route,
            m.DurationDays,
            m.IsActive,
            m.StartDate,
            m.EndDate,
            m.HealthRecord.Source,
            m.HealthRecord.Note,
            m.HealthRecord.CreatedAt)).ToList());
}
