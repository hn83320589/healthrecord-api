using System.Text.Json;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.MedicationReminder;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class MedicationReminderService(AppDbContext db) : IMedicationReminderService
{
    private static readonly Dictionary<string, List<string>> FrequencyTimeMap = new()
    {
        ["QD"] = ["08:00"],
        ["BID"] = ["08:00", "20:00"],
        ["TID"] = ["08:00", "14:00", "20:00"],
        ["QID"] = ["08:00", "12:00", "18:00", "22:00"],
    };

    public async Task<List<ReminderResponse>> GetAllAsync(int userId)
    {
        var reminders = await db.MedicationReminders
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.IsEnabled)
            .ThenBy(r => r.MedicationName)
            .ToListAsync();

        return reminders.Select(Map).ToList();
    }

    public async Task<ReminderResponse> CreateAsync(int userId, CreateReminderRequest request)
    {
        var times = request.RemindTimes ?? DetermineTimesFromFrequency(request.Frequency);

        var entity = new MedicationReminder
        {
            UserId = userId,
            MedicationName = request.MedicationName,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            RemindTimes = JsonSerializer.Serialize(times),
            DaysOfWeek = request.DaysOfWeek ?? "MON,TUE,WED,THU,FRI,SAT,SUN",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MedicationReminders.Add(entity);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<ReminderResponse> CreateFromMedicationAsync(
        int userId, int medicationDetailId, CreateReminderFromMedRequest? request)
    {
        var med = await db.MedicationDetails
            .Include(m => m.HealthRecord)
            .FirstOrDefaultAsync(m => m.Id == medicationDetailId && m.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Medication not found.");

        var times = request?.RemindTimes ?? DetermineTimesFromFrequency(med.Frequency);

        var entity = new MedicationReminder
        {
            UserId = userId,
            MedicationDetailId = medicationDetailId,
            MedicationName = med.MedicationName,
            Dosage = med.Dosage,
            Frequency = med.Frequency,
            RemindTimes = JsonSerializer.Serialize(times),
            DaysOfWeek = request?.DaysOfWeek ?? "MON,TUE,WED,THU,FRI,SAT,SUN",
            StartDate = request?.StartDate ?? med.StartDate,
            EndDate = request?.EndDate ?? med.EndDate,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MedicationReminders.Add(entity);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<ReminderResponse> UpdateAsync(int userId, int id, UpdateReminderRequest request)
    {
        var entity = await db.MedicationReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
            ?? throw new KeyNotFoundException("Reminder not found.");

        if (request.MedicationName != null) entity.MedicationName = request.MedicationName;
        if (request.Dosage != null) entity.Dosage = request.Dosage;
        if (request.RemindTimes != null) entity.RemindTimes = JsonSerializer.Serialize(request.RemindTimes);
        if (request.DaysOfWeek != null) entity.DaysOfWeek = request.DaysOfWeek;
        if (request.StartDate.HasValue) entity.StartDate = request.StartDate;
        if (request.EndDate.HasValue) entity.EndDate = request.EndDate;
        if (request.IsEnabled.HasValue) entity.IsEnabled = request.IsEnabled.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await db.MedicationReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
            ?? throw new KeyNotFoundException("Reminder not found.");

        // Also delete pending logs for this reminder
        var pendingLogs = await db.MedicationLogs
            .Where(l => l.ReminderId == id && l.Status == "pending")
            .ToListAsync();
        db.MedicationLogs.RemoveRange(pendingLogs);

        db.MedicationReminders.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<ReminderResponse> ToggleAsync(int userId, int id)
    {
        var entity = await db.MedicationReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
            ?? throw new KeyNotFoundException("Reminder not found.");

        entity.IsEnabled = !entity.IsEnabled;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Map(entity);
    }

    private static List<string> DetermineTimesFromFrequency(string? frequency)
    {
        if (frequency != null && FrequencyTimeMap.TryGetValue(frequency.ToUpperInvariant(), out var times))
            return times;
        return ["08:00"];
    }

    private static ReminderResponse Map(MedicationReminder r) => new(
        r.Id,
        r.MedicationDetailId,
        r.MedicationName,
        r.Dosage,
        r.Frequency,
        JsonSerializer.Deserialize<List<string>>(r.RemindTimes) ?? ["08:00"],
        r.DaysOfWeek,
        r.StartDate,
        r.EndDate,
        r.IsEnabled,
        r.CreatedAt);
}
