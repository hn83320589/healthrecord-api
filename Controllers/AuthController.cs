using HealthRecord.API.Models.DTOs.Auth;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

public class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }
}
