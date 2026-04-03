namespace HealthRecord.API.Models.DTOs.Nhi;

public record NhiImportResponse(
    int LogId,
    DateTime ImportedAt,
    DateOnly? DataDate,
    DateOnly? DateRangeStart,
    DateOnly? DateRangeEnd,
    int RecordCount,
    int VisitCount,
    int MedicationCount,
    int LabCount,
    int SkippedLabCount,
    int DuplicateLabCount,
    int NewItemCount);

public record NhiImportLogResponse(
    int Id,
    DateTime ImportedAt,
    string? FileName,
    DateOnly? DataDate,
    DateOnly? DateRangeStart,
    DateOnly? DateRangeEnd,
    int RecordCount,
    int VisitCount,
    int MedicationCount,
    int LabCount,
    int SkippedLabCount,
    int DuplicateLabCount,
    int NewItemCount);
