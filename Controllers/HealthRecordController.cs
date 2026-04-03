using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.HealthRecord;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("health-records")]
public class HealthRecordController(IHealthRecordService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<HealthRecordResponse>>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize, from, to);
        return Ok(ApiResponse<PagedResult<HealthRecordResponse>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HealthRecordResponse>>> GetById(int id)
    {
        var result = await service.GetByIdAsync(CurrentUserId, id);
        return Ok(ApiResponse<HealthRecordResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
