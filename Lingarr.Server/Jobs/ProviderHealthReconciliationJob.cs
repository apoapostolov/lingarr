using Hangfire;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public sealed class ProviderHealthReconciliationJob
{
    private readonly IProviderHealthService _providerHealth;
    private readonly IScheduleService _schedule;

    public ProviderHealthReconciliationJob(
        IProviderHealthService providerHealth,
        IScheduleService schedule)
    {
        _providerHealth = providerHealth;
        _schedule = schedule;
    }

    [AutomaticRetry(Attempts = 0)]
    [Queue("system")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        await _schedule.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());
        await _providerHealth.ReconcileAsync();
        await _schedule.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
    }
}
