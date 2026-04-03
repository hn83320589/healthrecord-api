namespace HealthRecord.API.Models.DTOs.Lab;

public record LabResultResponse(
    int Id,
    int HealthRecordId,
    DateTime RecordedAt,
    string ItemCode,
    string ItemName,
    string? DisplayName,
    bool IsNumeric,
    decimal? ValueNumeric,
    string? ValueText,
    string? Unit,
    string? ReferenceRange,
    decimal? NormalMin,
    decimal? NormalMax,
    string? Category,
    string Source,
    string? Note,
    DateTime CreatedAt);

public record CreateLabResultItem(
    string ItemCode,
    string ItemName,
    bool IsNumeric,
    decimal? ValueNumeric,
    string? ValueText,
    string? Unit,
    string? ReferenceRange);

public record CreateLabResultsRequest(
    DateTime RecordedAt,
    string? Note,
    List<CreateLabResultItem> Items);

public record UpdateLabResultRequest(
    decimal? ValueNumeric,
    string? ValueText,
    string? ReferenceRange,
    string? Note);

public record LabResultsByDateGroup(
    DateOnly Date,
    List<LabResultResponse> Items);

public record LabTrendPoint(DateTime RecordedAt, decimal Value);
