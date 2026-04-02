using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Lab;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class LabService(AppDbContext db) : ILabService
{
    public async Task<PagedResult<LabResultResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? from, DateTime? to, string? itemCode)
    {
        var query = db.LabResultDetails.Where(l => l.UserId == userId);
        if (from.HasValue) query = query.Where(l => l.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.RecordedAt <= to.Value);
        if (itemCode != null) query = query.Where(l => l.ItemCode == itemCode);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => Map(l))
            .ToListAsync();

        return new PagedResult<LabResultResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LabResultResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.LabResultDetails
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId)
            ?? throw new KeyNotFoundException("Lab result not found.");
        return Map(entity);
    }

    public async Task<List<LabResultResponse>> CreateAsync(int userId, CreateLabResultsRequest request)
    {
        var entities = request.Items.Select(item => new LabResultDetail
        {
            UserId = userId,
            HealthRecordId = item.HealthRecordId,
            RecordedAt = item.RecordedAt,
            ItemName = item.ItemName,
            ItemCode = item.ItemCode,
            Unit = item.Unit,
            Category = item.Category,
            NormalMin = item.NormalMin,
            NormalMax = item.NormalMax,
            IsNumeric = item.IsNumeric,
            ValueNumeric = item.ValueNumeric,
            ValueText = item.ValueText,
            IsAbnormal = item.IsAbnormal,
            Note = item.Note,
            Source = "manual",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        db.LabResultDetails.AddRange(entities);
        await db.SaveChangesAsync();
        return entities.Select(Map).ToList();
    }

    public async Task<LabResultResponse> UpdateAsync(int userId, int id, UpdateLabResultRequest request)
    {
        var entity = await db.LabResultDetails
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId)
            ?? throw new KeyNotFoundException("Lab result not found.");

        if (entity.Source != "manual")
            throw new InvalidOperationException("Only manual records can be updated.");

        entity.RecordedAt = request.RecordedAt;
        entity.NormalMin = request.NormalMin;
        entity.NormalMax = request.NormalMax;
        entity.IsNumeric = request.IsNumeric;
        entity.ValueNumeric = request.ValueNumeric;
        entity.ValueText = request.ValueText;
        entity.IsAbnormal = request.IsAbnormal;
        entity.Note = request.Note;

        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.LabResultDetails
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId)
            ?? throw new KeyNotFoundException("Lab result not found.");

        if (entity.Source != "manual")
            throw new InvalidOperationException("Only manual records can be deleted.");

        db.LabResultDetails.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<LabResultsByDateGroup>> GetByDateAsync(int userId, DateTime? from, DateTime? to)
    {
        var query = db.LabResultDetails.Where(l => l.UserId == userId);
        if (from.HasValue) query = query.Where(l => l.RecordedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.RecordedAt <= to.Value);

        var records = await query.OrderByDescending(l => l.RecordedAt).ToListAsync();

        return records
            .GroupBy(l => DateOnly.FromDateTime(l.RecordedAt))
            .OrderByDescending(g => g.Key)
            .Select(g => new LabResultsByDateGroup(g.Key, g.Select(Map).ToList()))
            .ToList();
    }

    public async Task<List<LabTrendPoint>> GetTrendAsync(int userId, string itemCode, string itemName)
    {
        return await db.LabResultDetails
            .Where(l => l.UserId == userId
                && l.ItemCode == itemCode
                && l.ItemName == itemName
                && l.IsNumeric
                && l.ValueNumeric != null)
            .OrderBy(l => l.RecordedAt)
            .Select(l => new LabTrendPoint(l.RecordedAt, l.ValueNumeric!.Value))
            .ToListAsync();
    }

    private static LabResultResponse Map(LabResultDetail l) => new(
        l.Id, l.HealthRecordId, l.RecordedAt, l.ItemName, l.ItemCode, l.Unit, l.Category,
        l.NormalMin, l.NormalMax, l.IsNumeric, l.ValueNumeric, l.ValueText, l.IsAbnormal,
        l.Source, l.Note, l.CreatedAt);
}
