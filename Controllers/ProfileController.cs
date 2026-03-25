using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Profile;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
public class ProfileController(IProfileService profileService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileResponse>>> GetProfile()
    {
        var result = await profileService.GetProfileAsync(CurrentUserId);
        return Ok(ApiResponse<ProfileResponse>.Ok(result));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ProfileResponse>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var result = await profileService.UpdateProfileAsync(CurrentUserId, request);
        return Ok(ApiResponse<ProfileResponse>.Ok(result));
    }

    [HttpGet("emergency-contacts")]
    public async Task<ActionResult<ApiResponse<List<EmergencyContactResponse>>>> GetEmergencyContacts()
    {
        var result = await profileService.GetEmergencyContactsAsync(CurrentUserId);
        return Ok(ApiResponse<List<EmergencyContactResponse>>.Ok(result));
    }

    [HttpPost("emergency-contacts")]
    public async Task<ActionResult<ApiResponse<EmergencyContactResponse>>> CreateEmergencyContact(
        [FromBody] CreateEmergencyContactRequest request)
    {
        var result = await profileService.CreateEmergencyContactAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<EmergencyContactResponse>.Ok(result));
    }

    [HttpPut("emergency-contacts/{id}")]
    public async Task<ActionResult<ApiResponse<EmergencyContactResponse>>> UpdateEmergencyContact(
        int id, [FromBody] UpdateEmergencyContactRequest request)
    {
        var result = await profileService.UpdateEmergencyContactAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<EmergencyContactResponse>.Ok(result));
    }

    [HttpDelete("emergency-contacts/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteEmergencyContact(int id)
    {
        await profileService.DeleteEmergencyContactAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
