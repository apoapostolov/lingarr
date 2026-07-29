using Lingarr.Server.Models.Api;

namespace Lingarr.Server.Interfaces.Services;

public interface IXaiOAuthSessionService
{
    Task<XaiOAuthStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<XaiOAuthDeviceResponse> StartDeviceFlowAsync(CancellationToken cancellationToken = default);
    Task<XaiOAuthPollResponse> PollAsync(string flowId, CancellationToken cancellationToken = default);
    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
