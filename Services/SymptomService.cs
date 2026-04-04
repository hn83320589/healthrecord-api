using System.Globalization;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Symptom;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class SymptomService(AppDbContext db) : ISymptomService
{
    private static readonly string[] PresetTypes =
    [
        "關節痛", "水腫", "疲倦", "皮疹", "發燒", "口腔潰瘍",
        "掉髮", "肌肉痠痛", "頭痛", "噁心", "食慾不振", "失眠", "其他"
    ];

    public async Task<PagedResult<SymptomResponse>> GetListAsync(
        int userId, int page, int pageSize, DateTime? startDate, DateTime? endDate, string? type)
    {
        var query = db.SymptomLogs.Where(s => s.UserId == userId);
        if (startDate.HasValue) query = query.Where(s => s.LoggedAt >= startDate.Value);
        if (endDate.HasValue)
        {
            var end = endDate.Value.TimeOfDay == TimeSpan.Zero ? endDate.Value.AddDays(1) : endDate.Value;
            query = query.Where(s => s.LoggedAt < end);
        }
        if (!string.IsNullOrEmpty(type)) query = query.Where(s => s.SymptomType == type);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.LoggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => Map(s))
            .ToListAsync();

        return new PagedResult<SymptomResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<SymptomResponse> GetByIdAsync(int userId, int id)
    {
        var entity = await db.SymptomLogs
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)
            ?? throw new KeyNotFoundException("Symptom log not found.");
        return Map(entity);
    }

    public async Task<SymptomResponse> CreateAsync(int userId, CreateSymptomRequest request)
    {
        var entity = new SymptomLog
        {
            UserId = userId,
            LoggedAt = request.LoggedAt,
            SymptomType = request.SymptomType,
            Severity = request.Severity,
            BodyLocation = request.BodyLocation,
            DurationMinutes = request.DurationMinutes,
            Triggers = request.Triggers,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };
        db.SymptomLogs.Add(entity);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<SymptomResponse> UpdateAsync(int userId, int id, UpdateSymptomRequest request)
    {
        var entity = await db.SymptomLogs
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)
            ?? throw new KeyNotFoundException("Symptom log not found.");

        if (request.LoggedAt.HasValue) entity.LoggedAt = request.LoggedAt.Value;
        if (request.SymptomType != null) entity.SymptomType = request.SymptomType;
        if (request.Severity.HasValue) entity.Severity = request.Severity.Value;
        if (request.BodyLocation != null) entity.BodyLocation = request.BodyLocation;
        if (request.DurationMinutes.HasValue) entity.DurationMinutes = request.DurationMinutes.Value;
        if (request.Triggers != null) entity.Triggers = request.Triggers;
        if (request.Note != null) entity.Note = request.Note;

        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.SymptomLogs
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)
            ?? throw new KeyNotFoundException("Symptom log not found.");
        db.SymptomLogs.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<SymptomSummaryResponse> GetSummaryAsync(int userId, int months)
    {
        var end = DateTime.UtcNow;
        var start = end.AddMonths(-months);
        var logs = await db.SymptomLogs
            .Where(s => s.UserId == userId && s.LoggedAt >= start && s.LoggedAt <= end)
            .OrderBy(s => s.LoggedAt)
            .ToListAsync();

        var byType = logs
            .GroupBy(s => s.SymptomType)
            .Select(g => new TypeStatDto(g.Key, g.Count(), Math.Round(g.Average(s => s.Severity), 1)))
            .OrderByDescending(t => t.Count)
            .ToList();

        var severityTrend = logs
            .GroupBy(s => ISOWeek.GetYear(s.LoggedAt) + "-W" +
                          ISOWeek.GetWeekOfYear(s.LoggedAt).ToString("D2"))
            .Select(g => new WeekTrendDto(g.Key, Math.Round(g.Average(s => s.Severity), 1)))
            .OrderBy(w => w.Week)
            .ToList();

        var topTriggers = logs
            .Where(s => !string.IsNullOrWhiteSpace(s.Triggers))
            .SelectMany(s => s.Triggers!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var calendar = logs
            .GroupBy(s => DateOnly.FromDateTime(s.LoggedAt))
            .Select(g => new CalendarDayDto(g.Key, g.Count(), g.Max(s => s.Severity)))
            .OrderByDescending(c => c.Date)
            .ToList();

        return new SymptomSummaryResponse(
            new PeriodDto(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end)),
            logs.Count, byType, severityTrend, topTriggers, calendar);
    }

    public async Task<List<string>> GetTypesAsync(int userId)
    {
        var userTypes = await db.SymptomLogs
            .Where(s => s.UserId == userId)
            .Select(s => s.SymptomType)
            .Distinct()
            .ToListAsync();

        return PresetTypes.Union(userTypes).Distinct().ToList();
    }

    private static SymptomResponse Map(SymptomLog s) => new(
        s.Id, s.LoggedAt, s.SymptomType, s.Severity,
        s.BodyLocation, s.DurationMinutes, s.Triggers, s.Note, s.CreatedAt);
}
