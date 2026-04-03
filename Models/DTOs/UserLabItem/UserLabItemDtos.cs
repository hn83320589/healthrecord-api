namespace HealthRecord.API.Models.DTOs.UserLabItem;

public record UserLabItemResponse(
    int Id,
    string ItemCode,
    string ItemName,
    string? DisplayName,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax,
    int SortOrder,
    bool IsPreset,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateUserLabItemRequest(
    string ItemCode,
    string ItemName,
    string? DisplayName,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax);

public record UpdateUserLabItemRequest(
    string? DisplayName,
    string? Unit,
    string? Category,
    decimal? NormalMin,
    decimal? NormalMax,
    int? SortOrder);
