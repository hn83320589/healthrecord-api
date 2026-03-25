namespace HealthRecord.API.Models.DTOs.Medication;

public record MedicationResponse(
    int Id,
    int? HealthRecordId,
    DateTime RecordedAt,
    string DrugName,
    string? NhiDrugCode,
    decimal? Quantity,
    int? Days,
    string DrugType,
    string Source,
    string? Note,
    DateTime CreatedAt);
