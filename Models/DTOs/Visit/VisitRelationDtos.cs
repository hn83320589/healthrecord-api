using HealthRecord.API.Models.DTOs.Symptom;

namespace HealthRecord.API.Models.DTOs.Visit;

// ── GET /visits/{id}/related ──────────────────────────────────

public record VisitRelatedResponse(
    VisitInfoDto Visit,
    List<VisitMedicationDto> Medications,
    List<LabResultWithStatusDto> LabResults,
    List<BloodPressureWithDateDto> BloodPressures,
    List<SymptomNearVisitDto> Symptoms,
    List<ActiveReminderDto> ActiveReminders,
    VisitSummaryDto Summary);

public record VisitInfoDto(
    int Id,
    DateTime RecordedAt,
    string? Institution,
    string? InstitutionCode,
    string? VisitTypeCode,
    List<DiagnosisDto> Diagnoses,
    string? CopaymentCode,
    decimal? MedicalCost,
    string Source);

public record DiagnosisDto(string Code, string Name);

public record VisitMedicationDto(
    int Id,
    string MedicationName,
    string? GenericName,
    string? NhiDrugCode,
    decimal? Quantity,
    decimal? Copayment,
    string? Dosage,
    string? Frequency,
    int? DurationDays);

public record LabResultWithStatusDto(
    int Id,
    string ItemCode,
    string ItemName,
    string? DisplayName,
    string? Category,
    decimal? Value,
    string? ValueText,
    bool IsNumeric,
    string? Unit,
    string? ReferenceRange,
    decimal? NormalMin,
    decimal? NormalMax,
    string Status); // "normal" | "high" | "low" | "unknown"

public record BloodPressureWithDateDto(
    int Id,
    DateTime RecordedAt,
    int Systolic,
    int Diastolic,
    int? Pulse,
    int DaysFromVisit);

public record VisitSummaryDto(
    int LabCount,
    int LabAbnormalCount,
    int MedicationCount,
    int BpCount,
    int? BpAvgSystolic,
    int? BpAvgDiastolic,
    int SymptomCount,
    int ActiveReminderCount);

// ── GET /visits/timeline ──────────────────────────────────────

public record VisitTimelineItemDto(
    int VisitId,
    DateTime RecordedAt,
    string? Institution,
    string? PrimaryDiagnosis,
    int MedicationCount,
    int LabCount,
    int LabAbnormalCount,
    BloodPressureSimpleDto? BpOnDay,
    List<KeyLabDto> KeyLabs);

public record BloodPressureSimpleDto(int Systolic, int Diastolic, int? Pulse);

public record KeyLabDto(string? DisplayName, decimal? Value, string? Unit, string Status);

// ── Active Reminders ─────────────────────────────────────────

public record ActiveReminderDto(int Id, string MedicationName, string? Dosage, string? Frequency);
