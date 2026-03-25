using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Medication;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("medications")]
public class MedicationController(IMedicationService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MedicationResponse>>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? drugType = null)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize, drugType);
        return Ok(ApiResponse<PagedResult<MedicationResponse>>.Ok(result));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<List<MedicationResponse>>>> GetCurrent()
    {
        var result = await service.GetCurrentAsync(CurrentUserId);
        return Ok(ApiResponse<List<MedicationResponse>>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
