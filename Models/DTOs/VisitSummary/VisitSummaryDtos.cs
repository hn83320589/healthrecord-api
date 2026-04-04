namespace HealthRecord.API.Models.DTOs.VisitSummary;

public record VisitSummaryJsonResponse(
    VisitSummaryVisitDto Visit,
    VisitSummaryBpDto BloodPressure,
    VisitSummaryLabDto LabResults,
    List<LabTrendDto> LabTrends,
    VisitSummaryMedDto Medications,
    VisitSummarySymptomDto Symptoms,
    DateTime GeneratedAt);

// ── Visit ────────────────────────────────────────────────────

public record VisitSummaryVisitDto(
    DateOnly Date,
    string? Institution,
    string? Department,
    List<SummaryDiagnosisDto> Diagnoses);

public record SummaryDiagnosisDto(string Code, string Name);

// ── Blood Pressure ───────────────────────────────────────────

public record VisitSummaryBpDto(
    BpValueDto RecentAvg,
    BpValueDto RecentMax,
    BpValueDto RecentMin,
    List<BpWeeklyTrendDto> Trend,
    int TotalMeasurements,
    int PeriodMonths);

public record BpValueDto(int Systolic, int Diastolic);

public record BpWeeklyTrendDto(DateOnly WeekStart, int AvgSystolic, int AvgDiastolic);

// ── Lab Results ──────────────────────────────────────────────

public record VisitSummaryLabDto(
    int AbnormalCount,
    int TotalCount,
    List<LabCategoryGroupDto> ByCategory);

public record LabCategoryGroupDto(
    string Category,
    List<LabItemSummaryDto> Items);

public record LabItemSummaryDto(
    string? DisplayName,
    decimal? Value,
    string? ValueText,
    string? Unit,
    string Status);

// ── Lab Trends ───────────────────────────────────────────────

public record LabTrendDto(
    string? DisplayName,
    string? Unit,
    List<LabTrendPointDto> Points,
    string TrendDirection); // ↑ | ↓ | →

public record LabTrendPointDto(DateOnly Date, decimal Value);

// ── Medications ──────────────────────────────────────────────

public record VisitSummaryMedDto(
    List<MedAdherenceItemDto> Current,
    double OverallAdherenceRate,
    int AdherencePeriodDays);

public record MedAdherenceItemDto(string Name, string? Frequency, double AdherenceRate);

// ── Symptoms ─────────────────────────────────────────────────

public record VisitSummarySymptomDto(
    int PeriodDays,
    int TotalCount,
    List<SymptomTypeCountDto> ByType);

public record SymptomTypeCountDto(string Type, int Count, double AvgSeverity);
