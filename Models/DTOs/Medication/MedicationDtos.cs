namespace HealthRecord.API.Models.DTOs.Medication;

public record MedicationResponse(
    int Id,
    int HealthRecordId,
    DateTime RecordedAt,
    string MedicationName,
    string? GenericName,
    string? NhiDrugCode,
    decimal? Quantity,
    decimal? Copayment,
    string? Dosage,
    string? Frequency,
    string? Route,
    int? DurationDays,
    bool IsActive,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Source,
    string? Note,
    DateTime CreatedAt);

public record CreateMedicationRequest(
    DateTime RecordedAt,
    string MedicationName,
    string? GenericName,
    string? Dosage,
    string? Frequency,
    string? Route,
    int? DurationDays,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Note);

public record UpdateMedicationRequest(
    string MedicationName,
    string? GenericName,
    string? Dosage,
    string? Frequency,
    string? Route,
    int? DurationDays,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Note);
