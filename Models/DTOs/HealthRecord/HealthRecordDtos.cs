namespace HealthRecord.API.Models.DTOs.HealthRecord;

public record HealthRecordResponse(
    int Id,
    string RecordType,
    DateTime RecordedAt,
    string? Note,
    string Source,
    string? NhiInstitution,
    string? NhiInstitutionCode,
    DateTime CreatedAt);
