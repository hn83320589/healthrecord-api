namespace HealthRecord.API.Models.DTOs.Profile;

public record ProfileResponse(
    int Id,
    string Email,
    string DisplayName,
    DateOnly? BirthDate,
    string? Gender,
    decimal? HeightCm,
    decimal? WeightKg,
    string? BloodType,
    string? ChronicConditions,
    string? Allergies);

public record UpdateProfileRequest(
    string DisplayName,
    DateOnly? BirthDate,
    string? Gender,
    decimal? HeightCm,
    decimal? WeightKg,
    string? BloodType,
    string? ChronicConditions,
    string? Allergies);

public record EmergencyContactResponse(
    int Id,
    string Name,
    string Relationship,
    string Phone,
    string? Note,
    int SortOrder);

public record CreateEmergencyContactRequest(
    string Name,
    string Relationship,
    string Phone,
    string? Note,
    int SortOrder = 0);

public record UpdateEmergencyContactRequest(
    string Name,
    string Relationship,
    string Phone,
    string? Note,
    int SortOrder);
