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
        [FromQuery] int pageSize = 20)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize);
        return Ok(ApiResponse<PagedResult<MedicationResponse>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MedicationResponse>>> GetById(int id)
    {
        var result = await service.GetByIdAsync(CurrentUserId, id);
        return Ok(ApiResponse<MedicationResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MedicationResponse>>> Create([FromBody] CreateMedicationRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return Ok(ApiResponse<MedicationResponse>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<MedicationResponse>>> Update(int id, [FromBody] UpdateMedicationRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<MedicationResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<MedicationResponse>>>> GetActive()
    {
        var result = await service.GetActiveAsync(CurrentUserId);
        return Ok(ApiResponse<List<MedicationResponse>>.Ok(result));
    }
}
