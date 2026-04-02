using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.UserLabItem;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("user-lab-items")]
public class UserLabItemController(IUserLabItemService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserLabItemResponse>>>> GetAll()
    {
        var result = await service.GetAllAsync(CurrentUserId);
        return Ok(ApiResponse<List<UserLabItemResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserLabItemResponse>>> Create(
        [FromBody] CreateUserLabItemRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<UserLabItemResponse>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserLabItemResponse>>> Update(
        int id, [FromBody] UpdateUserLabItemRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<UserLabItemResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }
}
