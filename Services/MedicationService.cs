using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Medication;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class MedicationService(AppDbContext db) : IMedicationService
{
    public async Task<PagedResult<MedicationResponse>> GetListAsync(
        int userId, int page, int pageSize, string? drugType)
    {
        var query = db.MedicationDetails.Where(m => m.UserId == userId);
        if (drugType != null) query = query.Where(m => m.DrugType == drugType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => Map(m))
            .ToListAsync();

        return new PagedResult<MedicationResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<MedicationResponse>> GetCurrentAsync(int userId)
    {
        // Find the most recent health record that has medications
        var latestVisitId = await db.MedicationDetails
            .Where(m => m.UserId == userId && m.HealthRecordId != null && m.DrugType == "medication")
            .OrderByDescending(m => m.RecordedAt)
            .Select(m => m.HealthRecordId)
            .FirstOrDefaultAsync();

        if (latestVisitId == null) return [];

        return await db.MedicationDetails
            .Where(m => m.HealthRecordId == latestVisitId && m.DrugType == "medication")
            .OrderBy(m => m.DrugName)
            .Select(m => Map(m))
            .ToListAsync();
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.MedicationDetails
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId)
            ?? throw new KeyNotFoundException("Medication not found.");

        if (entity.Source != "nhi_import")
            throw new InvalidOperationException("Only NHI-imported medications can be deleted in Phase 1.");

        db.MedicationDetails.Remove(entity);
        await db.SaveChangesAsync();
    }

    private static MedicationResponse Map(Models.Entities.MedicationDetail m) => new(
        m.Id, m.HealthRecordId, m.RecordedAt, m.DrugName, m.NhiDrugCode,
        m.Quantity, m.Days, m.DrugType, m.Source, m.Note, m.CreatedAt);
}
