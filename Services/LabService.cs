using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Lab;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Services;

public class LabService(AppDbContext db) : ILabService
{
    public async Task<PagedResult<LabResultResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to, string? itemCode)
    {
        var query = db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .Where(l => l.HealthRecord.UserId == userId);

        if (from.HasValue) query = query.Where(l => l.HealthRecord.RecordedAt >= from.Value);
        if (to.HasValue)
        {
            var end = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.AddDays(1) : to.Value;
            query = query.Where(l => l.HealthRecord.RecordedAt < end);
        }
        if (itemCode != null) query = query.Where(l => l.ItemCode == itemCode);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.HealthRecord.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<LabResultResponse>
        {
            Items = items.Select(Map),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LabResultResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .FirstOrDefaultAsync(l => l.Id == id && l.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Lab result not found.");
        return Map(entity);
    }

    public async Task<List<LabResultResponse>> CreateAsync(int userId, CreateLabResultsRequest request)
    {
        // Create one HealthRecord for the batch
        var record = new HealthRecordEntity
        {
            UserId = userId,
            RecordType = "lab_result",
            RecordedAt = request.RecordedAt,
            Note = request.Note,
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.HealthRecords.Add(record);
        await db.SaveChangesAsync();

        // Pre-load UserLabItems for matching
        var userItems = await db.UserLabItems
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => (i.ItemCode, i.ItemName));

        var details = new List<LabResultDetail>();
        foreach (var item in request.Items)
        {
            userItems.TryGetValue((item.ItemCode, item.ItemName), out var userLabItem);

            var detail = new LabResultDetail
            {
                UserId = userId,
                HealthRecordId = record.Id,
                UserLabItemId = userLabItem?.Id,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                IsNumeric = item.IsNumeric,
                ValueNumeric = item.ValueNumeric,
                ValueText = item.ValueText,
                Unit = item.Unit,
                ReferenceRange = item.ReferenceRange
            };
            details.Add(detail);
        }

        db.LabResultDetails.AddRange(details);
        await db.SaveChangesAsync();

        // Reload with navigation properties for mapping
        var detailIds = details.Select(d => d.Id).ToList();
        var loaded = await db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .Where(l => detailIds.Contains(l.Id))
            .ToListAsync();

        return loaded.Select(Map).ToList();
    }

    public async Task<LabResultResponse> UpdateAsync(int userId, int id, UpdateLabResultRequest request)
    {
        var entity = await db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .FirstOrDefaultAsync(l => l.Id == id && l.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Lab result not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be updated.");

        if (request.ValueNumeric.HasValue) entity.ValueNumeric = request.ValueNumeric;
        if (request.ValueText != null) entity.ValueText = request.ValueText;
        if (request.ReferenceRange != null) entity.ReferenceRange = request.ReferenceRange;
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
        var entity = await db.LabResultDetails
            .Include(l => l.HealthRecord)
            .FirstOrDefaultAsync(l => l.Id == id && l.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Lab result not found.");

        if (entity.HealthRecord.Source != "manual")
            throw new InvalidOperationException("Only manual records can be deleted.");

        // If this is the last detail on the HealthRecord, remove the parent too
        var siblingCount = await db.LabResultDetails
            .CountAsync(l => l.HealthRecordId == entity.HealthRecordId);

        if (siblingCount <= 1)
            db.HealthRecords.Remove(entity.HealthRecord); // CASCADE deletes detail
        else
            db.LabResultDetails.Remove(entity);

        await db.SaveChangesAsync();
    }

    public async Task<List<LabResultsByDateGroup>> GetByDateAsync(int userId, DateTime? from, DateTime? to)
    {
        var query = db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .Where(l => l.HealthRecord.UserId == userId);

        if (from.HasValue) query = query.Where(l => l.HealthRecord.RecordedAt >= from.Value);
        if (to.HasValue)
        {
            var end = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.AddDays(1) : to.Value;
            query = query.Where(l => l.HealthRecord.RecordedAt < end);
        }

        var records = await query
            .OrderByDescending(l => l.HealthRecord.RecordedAt)
            .ToListAsync();

        return records
            .GroupBy(l => DateOnly.FromDateTime(l.HealthRecord.RecordedAt))
            .OrderByDescending(g => g.Key)
            .Select(g => new LabResultsByDateGroup(g.Key, g.Select(Map).ToList()))
            .ToList();
    }

    public async Task<List<LabTrendPoint>> GetTrendAsync(int userId, string itemCode, string itemName)
    {
        return await db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Where(l => l.HealthRecord.UserId == userId
                && l.ItemCode == itemCode
                && l.ItemName == itemName
                && l.IsNumeric
                && l.ValueNumeric != null)
            .OrderBy(l => l.HealthRecord.RecordedAt)
            .Select(l => new LabTrendPoint(l.HealthRecord.RecordedAt, l.ValueNumeric!.Value))
            .ToListAsync();
    }

    private static LabResultResponse Map(LabResultDetail l) => new(
        l.Id,
        l.HealthRecordId,
        l.HealthRecord.RecordedAt,
        l.ItemCode,
        l.ItemName,
        l.UserLabItem?.DisplayName,
        l.IsNumeric,
        l.ValueNumeric,
        l.ValueText,
        l.Unit ?? l.UserLabItem?.Unit,
        l.ReferenceRange,
        l.UserLabItem?.NormalMin,
        l.UserLabItem?.NormalMax,
        l.UserLabItem?.Category,
        l.HealthRecord.Source,
        l.HealthRecord.Note,
        l.HealthRecord.CreatedAt);
}
