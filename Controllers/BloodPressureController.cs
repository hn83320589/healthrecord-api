using HealthRecord.API.Models.DTOs.BloodPressure;
using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Visit;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("blood-pressure")]
public class BloodPressureController(IBloodPressureService service, IVisitRelationService relation) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BloodPressureResponse>>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize, from, to);
        return Ok(ApiResponse<PagedResult<BloodPressureResponse>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<BloodPressureStatsResponse>>> GetStats(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await service.GetStatsAsync(CurrentUserId, from, to);
        return Ok(ApiResponse<BloodPressureStatsResponse>.Ok(result));
    }

    [HttpGet("chart-data")]
    public async Task<ActionResult<ApiResponse<List<BloodPressureChartPoint>>>> GetChartData(
        [FromQuery] string period = "30d")
    {
        var result = await service.GetChartDataAsync(CurrentUserId, period);
        return Ok(ApiResponse<List<BloodPressureChartPoint>>.Ok(result));
    }

    [HttpGet("around-date")]
    public async Task<ActionResult<ApiResponse<List<BloodPressureWithDateDto>>>> GetAroundDate(
        [FromQuery] DateTime date,
        [FromQuery] int days = 3)
    {
        var result = await relation.GetBpAroundDateAsync(CurrentUserId, date, days);
        return Ok(ApiResponse<List<BloodPressureWithDateDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BloodPressureResponse>>> GetById(int id)
    {
        var result = await service.GetByIdAsync(CurrentUserId, id);
        return Ok(ApiResponse<BloodPressureResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BloodPressureResponse>>> Create(
        [FromBody] CreateBloodPressureRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<BloodPressureResponse>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<BloodPressureResponse>>> Update(
        int id, [FromBody] UpdateBloodPressureRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<BloodPressureResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
