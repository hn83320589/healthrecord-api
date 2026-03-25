namespace HealthRecord.API.Models.DTOs.Nhi;

public record NhiImportResponse(
    int LogId,
    DateTime ImportedAt,
    string DataDate,
    DateOnly DateRangeStart,
    DateOnly DateRangeEnd,
    int HealthRecordCount,
    int MedicationCount,
    int LabCount,
    int SkippedLabs);

public record NhiImportLogResponse(
    int Id,
    DateTime ImportedAt,
    string DataDate,
    DateOnly DateRangeStart,
    DateOnly DateRangeEnd,
    int HealthRecordCount,
    int MedicationCount,
    int LabCount,
    int SkippedLabs);
