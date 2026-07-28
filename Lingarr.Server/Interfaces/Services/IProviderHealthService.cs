using Lingarr.Server.Models.ProviderHealth;

namespace Lingarr.Server.Interfaces.Services;

public interface IProviderHealthService
{
    Task<IReadOnlyList<ProviderHealthResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProviderHealthEventResponse>> GetEventsAsync(
        string provider,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        ProviderOperationalResult result,
        CancellationToken cancellationToken = default);

    Task<ProviderProbeResponse> ProbeAsync(
        string provider,
        CancellationToken cancellationToken = default);

    Task ReconcileAsync(CancellationToken cancellationToken = default);
}
