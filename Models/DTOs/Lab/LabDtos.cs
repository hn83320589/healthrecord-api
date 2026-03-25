namespace HealthRecord.API.Models.DTOs.Lab;

public record LabResultResponse(
    int Id,
    int? HealthRecordId,
    DateTime RecordedAt,
    string ItemName,
    string ItemCode,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax,
    bool IsNumeric,
    decimal? ValueNumeric,
    string? ValueText,
    bool IsAbnormal,
    string? NhiCode,
    string Source,
    string? Note,
    DateTime CreatedAt);

public record CreateLabResultItem(
    DateTime RecordedAt,
    string ItemName,
    string ItemCode,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax,
    bool IsNumeric,
    decimal? ValueNumeric,
    string? ValueText,
    bool IsAbnormal,
    string? Note,
    int? HealthRecordId = null);

public record CreateLabResultsRequest(List<CreateLabResultItem> Items);

public record UpdateLabResultRequest(
    DateTime RecordedAt,
    decimal? NormalMin,
    decimal? NormalMax,
    bool IsNumeric,
    decimal? ValueNumeric,
    string? ValueText,
    bool IsAbnormal,
    string? Note);

public record LabResultsByDateGroup(
    DateOnly Date,
    List<LabResultResponse> Items);

public record LabTrendPoint(DateTime RecordedAt, decimal Value);
