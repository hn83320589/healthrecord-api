using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Lab;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("lab-results")]
public class LabController(ILabService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LabResultResponse>>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? itemCode = null)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize, from, to, itemCode);
        return Ok(ApiResponse<PagedResult<LabResultResponse>>.Ok(result));
    }

    [HttpGet("by-date")]
    public async Task<ActionResult<ApiResponse<List<LabResultsByDateGroup>>>> GetByDate(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await service.GetByDateAsync(CurrentUserId, from, to);
        return Ok(ApiResponse<List<LabResultsByDateGroup>>.Ok(result));
    }

    [HttpGet("trend")]
    public async Task<ActionResult<ApiResponse<List<LabTrendPoint>>>> GetTrend([FromQuery] string itemCode = "Cr")
    {
        var result = await service.GetTrendAsync(CurrentUserId, itemCode);
        return Ok(ApiResponse<List<LabTrendPoint>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LabResultResponse>>> GetById(int id)
    {
        var result = await service.GetByIdAsync(CurrentUserId, id);
        return Ok(ApiResponse<LabResultResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<List<LabResultResponse>>>> Create(
        [FromBody] CreateLabResultsRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<List<LabResultResponse>>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<LabResultResponse>>> Update(
        int id, [FromBody] UpdateLabResultRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<LabResultResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
