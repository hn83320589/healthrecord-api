using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.MedicationReminder;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("medication-reminders")]
public class MedicationReminderController(IMedicationReminderService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReminderResponse>>>> GetAll()
    {
        var result = await service.GetAllAsync(CurrentUserId);
        return Ok(ApiResponse<List<ReminderResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReminderResponse>>> Create(
        [FromBody] CreateReminderRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<ReminderResponse>.Ok(result));
    }

    [HttpPost("from-medication/{medicationDetailId}")]
    public async Task<ActionResult<ApiResponse<ReminderResponse>>> CreateFromMedication(
        int medicationDetailId, [FromBody] CreateReminderFromMedRequest? request = null)
    {
        var result = await service.CreateFromMedicationAsync(CurrentUserId, medicationDetailId, request);
        return StatusCode(201, ApiResponse<ReminderResponse>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ReminderResponse>>> Update(
        int id, [FromBody] UpdateReminderRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<ReminderResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }

    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<ApiResponse<ReminderResponse>>> Toggle(int id)
    {
        var result = await service.ToggleAsync(CurrentUserId, id);
        return Ok(ApiResponse<ReminderResponse>.Ok(result));
    }
}
