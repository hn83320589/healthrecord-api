namespace HealthRecord.API.Models.DTOs.UserLabItem;

public record UserLabItemResponse(
    int Id,
    string ItemCode,
    string ItemName,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax,
    bool IsPreset,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateUserLabItemRequest(
    string ItemCode,
    string ItemName,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax);

public record UpdateUserLabItemRequest(
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax);
