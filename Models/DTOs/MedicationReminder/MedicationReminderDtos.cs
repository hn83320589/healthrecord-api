namespace HealthRecord.API.Models.DTOs.MedicationReminder;

// ── Reminder ─────────────────────────────────────────────────

public record ReminderResponse(
    int Id,
    int? MedicationDetailId,
    string MedicationName,
    string? Dosage,
    string? Frequency,
    List<string> RemindTimes,
    string DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsEnabled,
    DateTime CreatedAt);

public record CreateReminderRequest(
    string MedicationName,
    string? Dosage,
    string? Frequency,
    List<string>? RemindTimes,
    string? DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record CreateReminderFromMedRequest(
    List<string>? RemindTimes,
    string? DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record UpdateReminderRequest(
    string? MedicationName,
    string? Dosage,
    List<string>? RemindTimes,
    string? DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool? IsEnabled);

// ── Medication Log ───────────────────────────────────────────

public record MedicationLogResponse(
    int Id,
    int? ReminderId,
    string MedicationName,
    string? Dosage,
    DateTime ScheduledAt,
    DateTime? TakenAt,
    string Status,
    string? Note);

public record SkipNoteRequest(string? Note);

// ── Adherence ────────────────────────────────────────────────

public record AdherenceResponse(
    PeriodDto Period,
    AdherenceOverallDto Overall,
    List<AdherenceByMedDto> ByMedication,
    List<AdherenceDailyDto> DailyTrend);

public record PeriodDto(DateOnly Start, DateOnly End);

public record AdherenceOverallDto(
    int TotalScheduled,
    int Taken,
    int Late,
    int Skipped,
    int Missed,
    double AdherenceRate);

public record AdherenceByMedDto(
    string MedicationName,
    int TotalScheduled,
    int Taken,
    double AdherenceRate);

public record AdherenceDailyDto(
    DateOnly Date,
    int Scheduled,
    int Taken,
    double Rate);
