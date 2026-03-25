namespace HealthRecord.API.Models.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int UserId,
    string Email,
    string DisplayName);
