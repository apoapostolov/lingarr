using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.ProviderHealth;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/provider-health")]
public sealed class ProviderHealthController : ControllerBase
{
    private readonly IProviderHealthService _providerHealth;

    public ProviderHealthController(IProviderHealthService providerHealth)
    {
        _providerHealth = providerHealth;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderHealthResponse>>> Get(
        CancellationToken cancellationToken)
    {
        return Ok(await _providerHealth.GetAllAsync(cancellationToken));
    }

    [HttpGet("{provider}/events")]
    public async Task<ActionResult<IReadOnlyList<ProviderHealthEventResponse>>> GetEvents(
        string provider,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        return Ok(await _providerHealth.GetEventsAsync(provider, limit == 0 ? 50 : limit, cancellationToken));
    }

    [HttpPost("{provider}/test")]
    public async Task<ActionResult<ProviderProbeResponse>> Test(
        string provider,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _providerHealth.ProbeAsync(provider, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
