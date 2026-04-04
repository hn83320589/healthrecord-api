using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.HealthRecord;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Services;

public class HealthRecordService(AppDbContext db) : IHealthRecordService
{
    public async Task<PagedResult<HealthRecordResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to)
    {
        var query = db.HealthRecords.Where(h => h.UserId == userId);
        if (from.HasValue) query = query.Where(h => h.RecordedAt >= from.Value);
        if (to.HasValue)
        {
            var end = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.AddDays(1) : to.Value;
            query = query.Where(h => h.RecordedAt < end);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.RecordedAt)
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

    public async Task<HealthRecordResponse> GetByIdAsync(int userId, int id)
    {
        var record = await db.HealthRecords
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new KeyNotFoundException("Health record not found.");
        return Map(record);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var record = await db.HealthRecords
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new KeyNotFoundException("Health record not found.");

        if (record.Source != "nhi_import")
            throw new InvalidOperationException("Only NHI-imported records can be deleted.");

        db.HealthRecords.Remove(record); // CASCADE handles details
        await db.SaveChangesAsync();
    }

    private static HealthRecordResponse Map(HealthRecordEntity h) => new(
        h.Id,
        h.RecordType,
        h.RecordedAt,
        h.Note,
        h.Source,
        h.NhiInstitution,
        h.NhiInstitutionCode,
        h.CreatedAt);
}
