using Lingarr.Server.Models.Api;

namespace Lingarr.Server.Interfaces.Services;

public interface IPlexAuthService
{
    Task<PlexStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<PlexPinResponse> StartPinAsync(CancellationToken cancellationToken = default);

    Task<PlexPollResponse> PollPinAsync(long pinId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlexServerResponse>> GetServersAsync(CancellationToken cancellationToken = default);

    Task<PlexStatusResponse> SelectServerAsync(
        PlexServerRequest request,
        CancellationToken cancellationToken = default);

    Task<PlexStatusResponse> SaveTokenAsync(
        PlexTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<PlexTestResponse> TestAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
