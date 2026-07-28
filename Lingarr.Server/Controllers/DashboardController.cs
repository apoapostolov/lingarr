using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardActivityService _activity;

    public DashboardController(IDashboardActivityService activity)
    {
        _activity = activity;
    }

    [HttpGet("activity")]
    public async Task<ActionResult<DashboardActivityResponse>> GetActivity(
        [FromQuery] int? hours,
        CancellationToken cancellationToken)
    {
        return Ok(await _activity.GetAsync(hours, cancellationToken));
    }
}
