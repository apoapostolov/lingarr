using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/xai-oauth")]
public sealed class XaiOAuthController : ControllerBase
{
    private readonly IXaiOAuthSessionService _sessions;

    public XaiOAuthController(IXaiOAuthSessionService sessions)
    {
        _sessions = sessions;
    }

    [HttpGet("status")]
    public Task<XaiOAuthStatusResponse> GetStatus(CancellationToken cancellationToken) =>
        _sessions.GetStatusAsync(cancellationToken);

    [HttpPost("device")]
    public Task<XaiOAuthDeviceResponse> StartDeviceFlow(CancellationToken cancellationToken) =>
        _sessions.StartDeviceFlowAsync(cancellationToken);

    [HttpPost("device/{flowId}/poll")]
    public Task<XaiOAuthPollResponse> Poll(
        string flowId,
        CancellationToken cancellationToken) =>
        _sessions.PollAsync(flowId, cancellationToken);

    [HttpDelete("session")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _sessions.DisconnectAsync(cancellationToken);
        return NoContent();
    }
}
