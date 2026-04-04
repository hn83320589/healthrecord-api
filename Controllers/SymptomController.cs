using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Symptom;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("symptoms")]
public class SymptomController(ISymptomService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SymptomResponse>>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? type = null)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize, startDate, endDate, type);
        return Ok(ApiResponse<PagedResult<SymptomResponse>>.Ok(result));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<SymptomSummaryResponse>>> GetSummary(
        [FromQuery] int months = 3)
    {
        var result = await service.GetSummaryAsync(CurrentUserId, months);
        return Ok(ApiResponse<SymptomSummaryResponse>.Ok(result));
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetTypes()
    {
        var result = await service.GetTypesAsync(CurrentUserId);
        return Ok(ApiResponse<List<string>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SymptomResponse>>> GetById(int id)
    {
        var result = await service.GetByIdAsync(CurrentUserId, id);
        return Ok(ApiResponse<SymptomResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SymptomResponse>>> Create(
        [FromBody] CreateSymptomRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<SymptomResponse>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SymptomResponse>>> Update(
        int id, [FromBody] UpdateSymptomRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<SymptomResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
