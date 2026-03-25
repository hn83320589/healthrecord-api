using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[ApiController]
[Route("[controller]")]
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated."));
}
