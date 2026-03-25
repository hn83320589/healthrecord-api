using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Nhi;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("nhi")]
public class NhiController(INhiImportService service) : BaseController
{
    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<NhiImportResponse>>> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<NhiImportResponse>.Fail("No file uploaded."));

        using var reader = new StreamReader(file.OpenReadStream());
        var json = await reader.ReadToEndAsync();

        var result = await service.ImportAsync(CurrentUserId, json);
        return Ok(ApiResponse<NhiImportResponse>.Ok(result));
    }

    [HttpGet("import/logs")]
    public async Task<ActionResult<ApiResponse<List<NhiImportLogResponse>>>> GetLogs()
    {
        var result = await service.GetLogsAsync(CurrentUserId);
        return Ok(ApiResponse<List<NhiImportLogResponse>>.Ok(result));
    }

    [HttpDelete("import/{logId}")]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(int logId)
    {
        await service.RevokeAsync(CurrentUserId, logId);
        return Ok(ApiResponse<object>.Ok(null!, "Import revoked."));
    }
}
