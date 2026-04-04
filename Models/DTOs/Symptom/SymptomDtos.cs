namespace HealthRecord.API.Models.DTOs.Symptom;

public record SymptomResponse(
    int Id,
    DateTime LoggedAt,
    string SymptomType,
    int Severity,
    string? BodyLocation,
    int? DurationMinutes,
    string? Triggers,
    string? Note,
    DateTime CreatedAt);

public record CreateSymptomRequest(
    DateTime LoggedAt,
    string SymptomType,
    int Severity,
    string? BodyLocation,
    int? DurationMinutes,
    string? Triggers,
    string? Note);

public record UpdateSymptomRequest(
    DateTime? LoggedAt,
    string? SymptomType,
    int? Severity,
    string? BodyLocation,
    int? DurationMinutes,
    string? Triggers,
    string? Note);

// ── Summary ──────────────────────────────────────────

public record SymptomSummaryResponse(
    SymptomPeriodDto Period,
    int TotalCount,
    List<TypeStatDto> ByType,
    List<WeekTrendDto> SeverityTrend,
    List<string> TopTriggers,
    List<CalendarDayDto> Calendar);

public record SymptomPeriodDto(DateOnly Start, DateOnly End);
public record TypeStatDto(string Type, int Count, double AvgSeverity);
public record WeekTrendDto(string Week, double AvgSeverity);
public record CalendarDayDto(DateOnly Date, int Count, int MaxSeverity);

// ── For visit relation ───────────────────────────────

public record SymptomNearVisitDto(
    int Id,
    DateTime LoggedAt,
    string SymptomType,
    int Severity,
    string? BodyLocation,
    int DaysFromVisit);
