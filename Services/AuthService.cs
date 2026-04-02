using HealthRecord.API.Common.Constants;
using HealthRecord.API.Common.Helpers;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Auth;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class AuthService(AppDbContext db, JwtHelper jwtHelper) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ArgumentException("Email already registered.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var presets = LabItemPresets.Items
            .Where(p => p.NhiCode != null)
            .Select(p => new UserLabItem
            {
                UserId = user.Id,
                ItemCode = p.NhiCode!,
                ItemName = p.NhiItemName ?? "",
                Unit = p.Unit,
                Category = p.Category,
                NormalMin = p.NormalMin,
                NormalMax = p.NormalMax,
                IsPreset = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        db.UserLabItems.AddRange(presets);
        await db.SaveChangesAsync();

        return await IssueTokens(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokens(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        return await IssueTokens(user);
    }

    private async Task<AuthResponse> IssueTokens(User user)
    {
        var accessToken = jwtHelper.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = JwtHelper.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken, user.Id, user.Email, user.DisplayName);
    }
}
