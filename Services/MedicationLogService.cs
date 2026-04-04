using System.Text.Json;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.MedicationReminder;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class MedicationLogService(AppDbContext db) : IMedicationLogService
{
    private static readonly Dictionary<DayOfWeek, string> DayOfWeekMap = new()
    {
        [DayOfWeek.Monday] = "MON",
        [DayOfWeek.Tuesday] = "TUE",
        [DayOfWeek.Wednesday] = "WED",
        [DayOfWeek.Thursday] = "THU",
        [DayOfWeek.Friday] = "FRI",
        [DayOfWeek.Saturday] = "SAT",
        [DayOfWeek.Sunday] = "SUN",
    };

    public async Task<List<MedicationLogResponse>> GetListAsync(
        int userId, DateTime? startDate, DateTime? endDate, string? status)
    {
        var query = db.MedicationLogs.Where(l => l.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(l => l.ScheduledAt >= startDate.Value);
        if (endDate.HasValue)
        {
            var end = endDate.Value.TimeOfDay == TimeSpan.Zero ? endDate.Value.AddDays(1) : endDate.Value;
            query = query.Where(l => l.ScheduledAt < end);
        }
        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        var logs = await query
            .OrderByDescending(l => l.ScheduledAt)
            .ToListAsync();

        return logs.Select(Map).ToList();
    }

    public async Task<List<MedicationLogResponse>> GetTodayAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var todayDow = DayOfWeekMap[today.DayOfWeek];
        var todayDateOnly = DateOnly.FromDateTime(today);

        // 1. Get enabled reminders active today
        var reminders = await db.MedicationReminders
            .Where(r => r.UserId == userId && r.IsEnabled)
            .ToListAsync();

        var activeReminders = reminders
            .Where(r => (r.StartDate == null || r.StartDate <= todayDateOnly)
                     && (r.EndDate == null || r.EndDate >= todayDateOnly)
                     && r.DaysOfWeek.Contains(todayDow))
            .ToList();

        // 2. Get existing logs for today
        var tomorrow = today.AddDays(1);
        var existingLogs = await db.MedicationLogs
            .Where(l => l.UserId == userId && l.ScheduledAt >= today && l.ScheduledAt < tomorrow)
            .ToListAsync();

        // 3. Create missing logs for each reminder x time
        var newLogs = new List<MedicationLog>();
        foreach (var reminder in activeReminders)
        {
            var times = JsonSerializer.Deserialize<List<string>>(reminder.RemindTimes) ?? ["08:00"];
            foreach (var timeStr in times)
            {
                if (!TimeOnly.TryParse(timeStr, out var timeOnly)) continue;

                var scheduledAt = today.Add(timeOnly.ToTimeSpan());

                var exists = existingLogs.Any(l =>
                    l.ReminderId == reminder.Id
                    && l.ScheduledAt == scheduledAt);

                if (!exists)
                {
                    var log = new MedicationLog
                    {
                        UserId = userId,
                        ReminderId = reminder.Id,
                        MedicationName = reminder.MedicationName,
                        Dosage = reminder.Dosage,
                        ScheduledAt = scheduledAt,
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    newLogs.Add(log);
                }
            }
        }

        if (newLogs.Count > 0)
        {
            db.MedicationLogs.AddRange(newLogs);
            await db.SaveChangesAsync();
        }

        // 4. Auto-mark missed: pending logs where ScheduledAt < now - 2 hours
        var missedThreshold = DateTime.UtcNow.AddHours(-2);
        var allTodayLogs = await db.MedicationLogs
            .Where(l => l.UserId == userId && l.ScheduledAt >= today && l.ScheduledAt < tomorrow)
            .ToListAsync();

        var missedLogs = allTodayLogs
            .Where(l => l.Status == "pending" && l.ScheduledAt < missedThreshold)
            .ToList();

        foreach (var log in missedLogs)
            log.Status = "missed";

        if (missedLogs.Count > 0)
            await db.SaveChangesAsync();

        // 5. Return all today's logs ordered by ScheduledAt
        return allTodayLogs.OrderBy(l => l.ScheduledAt).Select(Map).ToList();
    }

    public async Task<MedicationLogResponse> TakeAsync(int userId, int id)
    {
        var log = await db.MedicationLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId)
            ?? throw new KeyNotFoundException("Medication log not found.");

        log.TakenAt = DateTime.UtcNow;
        log.Status = DateTime.UtcNow > log.ScheduledAt.AddHours(1) ? "late" : "taken";

        await db.SaveChangesAsync();
        return Map(log);
    }

    public async Task<MedicationLogResponse> SkipAsync(int userId, int id, string? note)
    {
        var log = await db.MedicationLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId)
            ?? throw new KeyNotFoundException("Medication log not found.");

        log.Status = "skipped";
        log.Note = note;

        await db.SaveChangesAsync();
        return Map(log);
    }

    public async Task<MedicationLogResponse> UndoAsync(int userId, int id)
    {
        var log = await db.MedicationLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId)
            ?? throw new KeyNotFoundException("Medication log not found.");

        log.Status = "pending";
        log.TakenAt = null;

        await db.SaveChangesAsync();
        return Map(log);
    }

    public async Task<AdherenceResponse> GetAdherenceAsync(int userId, int days)
    {
        var now = DateTime.UtcNow;
        var start = now.AddDays(-days).Date;
        var end = now;

        var logs = await db.MedicationLogs
            .Where(l => l.UserId == userId && l.ScheduledAt >= start && l.ScheduledAt <= end)
            .ToListAsync();

        // "missed" = scheduled in the past AND status still "pending"
        var taken = logs.Count(l => l.Status == "taken");
        var late = logs.Count(l => l.Status == "late");
        var skipped = logs.Count(l => l.Status == "skipped");
        var missed = logs.Count(l => l.Status == "missed")
                   + logs.Count(l => l.Status == "pending" && l.ScheduledAt < now);
        var pending = logs.Count(l => l.Status == "pending" && l.ScheduledAt >= now);
        var totalScheduled = logs.Count - pending; // exclude future pending from total

        var adherenceRate = totalScheduled > 0
            ? Math.Round((double)(taken + late) / totalScheduled * 100, 1)
            : 0;

        var byMedication = logs
            .Where(l => !(l.Status == "pending" && l.ScheduledAt >= now)) // exclude future pending
            .GroupBy(l => l.MedicationName)
            .Select(g =>
            {
                var total = g.Count();
                var t = g.Count(l => l.Status == "taken");
                var lt = g.Count(l => l.Status == "late");
                var rate = total > 0 ? Math.Round((double)(t + lt) / total * 100, 1) : 0;
                return new AdherenceByMedDto(g.Key, total, t + lt, rate);
            })
            .OrderBy(m => m.MedicationName)
            .ToList();

        var dailyTrend = logs
            .GroupBy(l => DateOnly.FromDateTime(l.ScheduledAt))
            .Select(g =>
            {
                var scheduled = g.Count(l => !(l.Status == "pending" && l.ScheduledAt >= now));
                var t = g.Count(l => l.Status is "taken" or "late");
                var rate = scheduled > 0 ? Math.Round((double)t / scheduled * 100, 1) : 0;
                return new AdherenceDailyDto(g.Key, scheduled, t, rate);
            })
            .OrderByDescending(d => d.Date)
            .ToList();

        return new AdherenceResponse(
            new PeriodDto(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end.Date)),
            new AdherenceOverallDto(totalScheduled, taken, late, skipped, missed, adherenceRate),
            byMedication,
            dailyTrend);
    }

    private static MedicationLogResponse Map(MedicationLog l) => new(
        l.Id, l.ReminderId, l.MedicationName, l.Dosage,
        l.ScheduledAt, l.TakenAt, l.Status, l.Note);
}
