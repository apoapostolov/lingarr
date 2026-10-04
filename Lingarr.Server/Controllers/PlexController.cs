using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/plex")]
public sealed class PlexController : ControllerBase
{
    private readonly IPlexAuthService _auth;

    public PlexController(IPlexAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet("status")]
    public Task<PlexStatusResponse> GetStatus(CancellationToken cancellationToken) =>
        _auth.GetStatusAsync(cancellationToken);

    [HttpPost("oauth/pin")]
    public Task<PlexPinResponse> StartPin(CancellationToken cancellationToken) =>
        _auth.StartPinAsync(cancellationToken);

    [HttpPost("oauth/pin/{pinId:long}/poll")]
    public Task<PlexPollResponse> PollPin(long pinId, CancellationToken cancellationToken) =>
        _auth.PollPinAsync(pinId, cancellationToken);

    [HttpGet("servers")]
    public Task<IReadOnlyList<PlexServerResponse>> GetServers(CancellationToken cancellationToken) =>
        _auth.GetServersAsync(cancellationToken);

    [HttpPost("server")]
    public async Task<ActionResult<PlexStatusResponse>> SelectServer(
        [FromBody] PlexServerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _auth.SelectServerAsync(request, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { detail = exception.Message });
        }
    }

    [HttpPost("token")]
    public async Task<ActionResult<PlexStatusResponse>> SaveToken(
        [FromBody] PlexTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _auth.SaveTokenAsync(request, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { detail = exception.Message });
        }
    }

    [HttpPost("test")]
    public Task<PlexTestResponse> Test(CancellationToken cancellationToken) =>
        _auth.TestAsync(cancellationToken);

    [HttpDelete("session")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(cancellationToken);
        return NoContent();
    }
}
