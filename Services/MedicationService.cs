using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Medication;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Services;

public class MedicationService(AppDbContext db) : IMedicationService
{
    public async Task<PagedResult<MedicationResponse>> GetListAsync(
        int userId, int page, int pageSize)
    {
        var query = db.MedicationDetails
            .Include(m => m.HealthRecord)
            .Where(m => m.HealthRecord.UserId == userId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.HealthRecord.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MedicationResponse>
        {
            Items = items.Select(Map),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MedicationResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.MedicationDetails
            .Include(m => m.HealthRecord)
            .FirstOrDefaultAsync(m => m.Id == id && m.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Medication not found.");
        return Map(entity);
    }

    public async Task<MedicationResponse> CreateAsync(int userId, CreateMedicationRequest request)
    {
        var record = new HealthRecordEntity
        {
            UserId = userId,
            RecordType = "medication",
            RecordedAt = request.RecordedAt,
            Note = request.Note,
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.HealthRecords.Add(record);
        await db.SaveChangesAsync();

        var detail = new MedicationDetail
        {
            HealthRecordId = record.Id,
            MedicationName = request.MedicationName,
            GenericName = request.GenericName,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            Route = request.Route,
            DurationDays = request.DurationDays,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = false
        };
        db.MedicationDetails.Add(detail);
        await db.SaveChangesAsync();

        detail.HealthRecord = record;
        return Map(detail);
    }

    public async Task<MedicationResponse> UpdateAsync(int userId, int id, UpdateMedicationRequest request)
    {
        var entity = await db.MedicationDetails
            .Include(m => m.HealthRecord)
            .FirstOrDefaultAsync(m => m.Id == id && m.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Medication not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be updated.");

        entity.MedicationName = request.MedicationName;
        entity.GenericName = request.GenericName;
        entity.Dosage = request.Dosage;
        entity.Frequency = request.Frequency;
        entity.Route = request.Route;
        entity.DurationDays = request.DurationDays;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        if (request.Note != null)
        {
            entity.HealthRecord.Note = request.Note;
            entity.HealthRecord.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.MedicationDetails
            .Include(m => m.HealthRecord)
            .FirstOrDefaultAsync(m => m.Id == id && m.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Medication not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be deleted.");

        // If this is the last detail on the HealthRecord, remove the parent too
        var siblingCount = await db.MedicationDetails
            .CountAsync(m => m.HealthRecordId == entity.HealthRecordId);

        if (siblingCount <= 1)
            db.HealthRecords.Remove(entity.HealthRecord); // CASCADE deletes detail
        else
            db.MedicationDetails.Remove(entity);

        await db.SaveChangesAsync();
    }

    public async Task<List<MedicationResponse>> GetActiveAsync(int userId)
    {
        // For now, return medications from the most recent visit that has medications
        var latestVisitRecordId = await db.MedicationDetails
            .Include(m => m.HealthRecord)
            .Where(m => m.HealthRecord.UserId == userId && m.VisitDetailId != null)
            .OrderByDescending(m => m.HealthRecord.RecordedAt)
            .Select(m => m.HealthRecordId)
            .FirstOrDefaultAsync();

        if (latestVisitRecordId == 0) return [];

        var meds = await db.MedicationDetails
            .Include(m => m.HealthRecord)
            .Where(m => m.VisitDetailId != null
                && m.HealthRecord.UserId == userId
                && m.HealthRecord.RecordedAt == db.HealthRecords
                    .Where(h => h.Id == latestVisitRecordId)
                    .Select(h => h.RecordedAt)
                    .FirstOrDefault())
            .OrderBy(m => m.MedicationName)
            .ToListAsync();

        return meds.Select(Map).ToList();
    }

    private static MedicationResponse Map(MedicationDetail m) => new(
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
        m.HealthRecord.CreatedAt);
}
