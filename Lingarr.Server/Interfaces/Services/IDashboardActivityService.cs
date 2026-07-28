using Lingarr.Server.Models.Dashboard;

namespace Lingarr.Server.Interfaces.Services;

public interface IDashboardActivityService
{
    Task<DashboardActivityResponse> GetAsync(
        int? requestedHours = null,
        CancellationToken cancellationToken = default);
}
