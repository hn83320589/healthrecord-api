using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.MedicationReminder;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("medication-logs")]
public class MedicationLogController(IMedicationLogService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MedicationLogResponse>>>> GetList(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null)
    {
        var result = await service.GetListAsync(CurrentUserId, startDate, endDate, status);
        return Ok(ApiResponse<List<MedicationLogResponse>>.Ok(result));
    }

    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<List<MedicationLogResponse>>>> GetToday()
    {
        var result = await service.GetTodayAsync(CurrentUserId);
        return Ok(ApiResponse<List<MedicationLogResponse>>.Ok(result));
    }

    [HttpPost("{id}/take")]
    public async Task<ActionResult<ApiResponse<MedicationLogResponse>>> Take(int id)
    {
        var result = await service.TakeAsync(CurrentUserId, id);
        return Ok(ApiResponse<MedicationLogResponse>.Ok(result));
    }

    [HttpPost("{id}/skip")]
    public async Task<ActionResult<ApiResponse<MedicationLogResponse>>> Skip(
        int id, [FromBody] SkipNoteRequest? request = null)
    {
        var result = await service.SkipAsync(CurrentUserId, id, request?.Note);
        return Ok(ApiResponse<MedicationLogResponse>.Ok(result));
    }

    [HttpPost("{id}/undo")]
    public async Task<ActionResult<ApiResponse<MedicationLogResponse>>> Undo(int id)
    {
        var result = await service.UndoAsync(CurrentUserId, id);
        return Ok(ApiResponse<MedicationLogResponse>.Ok(result));
    }

    [HttpGet("adherence")]
    public async Task<ActionResult<ApiResponse<AdherenceResponse>>> GetAdherence(
        [FromQuery] int days = 30)
    {
        var result = await service.GetAdherenceAsync(CurrentUserId, days);
        return Ok(ApiResponse<AdherenceResponse>.Ok(result));
    }
}
