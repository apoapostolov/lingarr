using System.Collections.Concurrent;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Dashboard;
using Lingarr.Server.Models.ProviderHealth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Lingarr.Server.Services;

public class DashboardActivityService : IDashboardActivityService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISettingService _settings;
    private readonly IProviderHealthService _providerHealth;
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardActivityService> _logger;

    /// <summary>How long a cached entry is considered fresh and served without refresh.</summary>
    private static readonly TimeSpan FreshTtl = TimeSpan.FromSeconds(30);
    /// <summary>How long a stale entry is still served (while background refresh runs).</summary>
    private static readonly TimeSpan StaleTtl = TimeSpan.FromMinutes(10);
    /// <summary>Per-window semaphore to avoid a cache stampede on cold or expired keys.</summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public DashboardActivityService(
        LingarrDbContext dbContext,
        ISettingService settings,
        IProviderHealthService providerHealth,
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardActivityService> logger)
    {
        _dbContext = dbContext;
        _settings = settings;
        _providerHealth = providerHealth;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DashboardActivityResponse> GetAsync(
        int? requestedHours = null,
        CancellationToken cancellationToken = default)
    {
        var hours = await ResolveHoursAsync(requestedHours, cancellationToken);
        var key = CacheKey(hours);

        // Hot path: fresh cache hit, no DB work at all.
        if (_cache.TryGetValue<CacheEntry>(key, out var entry) && entry is not null)
        {
            if (!entry.IsStale)
            {
                return entry.Response;
            }

            // Stale-while-revalidate: return the stale payload immediately and
            // refresh in the background. The refresh runs in its own DI scope
            // so it does not capture this (scoped) DbContext beyond the request.
            TriggerBackgroundRefresh(key, hours);
            return entry.Response;
        }

        // Cold cache: compute synchronously within this request scope, then cache.
        await using var guard = await AcquireAsync(key, cancellationToken);
        if (_cache.TryGetValue<CacheEntry>(key, out var refreshed) && refreshed is not null && !refreshed.IsStale)
        {
            return refreshed.Response;
        }

        var response = await ComputeAsync(hours, cancellationToken);
        Store(key, response);
        return response;
    }

    private async Task<int> ResolveHoursAsync(int? requestedHours, CancellationToken cancellationToken)
    {
        var configured = 48;
        if (!requestedHours.HasValue)
        {
            var setting = await _settings.GetSetting(SettingKeys.Dashboard.ActivityWindowHours);
            if (int.TryParse(setting, out var parsed))
                configured = parsed;
        }

        return Math.Clamp(requestedHours ?? configured, 1, 168);
    }

    private void TriggerBackgroundRefresh(string key, int hours)
    {
        // Fire-and-forget; failures are logged and never propagate. The stale
        // entry remains served until a successful refresh replaces it or the
        // stale TTL expires.
        _ = Task.Run(async () =>
        {
            try
            {
                await using var guard = await AcquireAsync(key, CancellationToken.None);
                using var scope = _scopeFactory.CreateScope();
                var service = ActivatorUtilities.CreateInstance<DashboardActivityService>(
                    scope.ServiceProvider);
                var fresh = await service.ComputeAsync(hours, CancellationToken.None);
                Store(key, fresh);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Background dashboard activity refresh failed for window {Hours}h.", hours);
            }
        });
    }

    private static string CacheKey(int hours) => $"dashboard:activity:{hours}";

    private void Store(string key, DashboardActivityResponse response)
    {
        var entry = new CacheEntry(response, DateTimeOffset.UtcNow);
        _cache.Set(key, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = StaleTtl
        });
    }

    private static async Task<SemaphoreGuard> AcquireAsync(string key, CancellationToken ct)
    {
        var semaphore = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        return new SemaphoreGuard(semaphore);
    }

    private sealed record CacheEntry(DashboardActivityResponse Response, DateTimeOffset StoredAt)
    {
        public bool IsStale => DateTimeOffset.UtcNow - StoredAt >= FreshTtl;
    }

    private sealed class SemaphoreGuard : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        public SemaphoreGuard(SemaphoreSlim semaphore) => _semaphore = semaphore;
        public ValueTask DisposeAsync()
        {
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// The expensive computation: 5 DB round-trips over TranslationRequests,
    /// TranslationRequestLines, ProviderOperationalEvents plus provider health.
    /// Kept synchronous here so it can be reused by the request and background paths.
    /// </summary>
    private async Task<DashboardActivityResponse> ComputeAsync(
        int hours,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = now.AddHours(-hours);

        var recentRequests = await _dbContext.TranslationRequests
            .Where(request =>
                (request.CompletedAt != null && request.CompletedAt >= start) ||
                (request.CompletedAt == null && request.UpdatedAt >= start))
            .Select(request => new
            {
                request.Id,
                request.Status,
                request.SourceLanguage,
                request.TargetLanguage,
                request.CompletedAt,
                request.QualityScore,
                request.QualityStatus
            })
            .ToListAsync(cancellationToken);

        var completed = recentRequests
            .Where(request => request.Status == TranslationStatus.Completed &&
                              request.CompletedAt >= start)
            .ToList();
        var completedIds = completed.Select(request => request.Id).ToArray();
        var recentLines = await _dbContext.TranslationRequestLines
            .Where(line => completedIds.Contains(line.TranslationRequestId))
            .Select(line => new
            {
                line.TranslationRequestId,
                line.Service
            })
            .ToListAsync(cancellationToken);

        var checkedRequests = completed
            .Where(request =>
                request.QualityStatus == "completed" &&
                request.QualityScore.HasValue)
            .ToList();
        var passed = checkedRequests.Count(request => request.QualityScore >= 85);
        var needsReview = checkedRequests.Count - passed;

        var topProviders = recentLines
            .Where(line => !string.IsNullOrWhiteSpace(line.Service))
            .GroupBy(line => line.Service!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardNamedCount(
                DisplayName(group.Key), group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name)
            .Take(5)
            .ToList();
        var dominantProvidersByFile = recentLines
            .Where(line => !string.IsNullOrWhiteSpace(line.Service))
            .GroupBy(line => line.TranslationRequestId)
            .Select(file => file
                .GroupBy(line => line.Service!, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(provider => provider.Count())
                .ThenBy(provider => provider.Key, StringComparer.OrdinalIgnoreCase)
                .Select(provider => provider.Key)
                .First())
            .ToList();
        var topProviderByFiles = dominantProvidersByFile
            .GroupBy(provider => provider, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardNamedCount(
                DisplayName(group.Key), group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name)
            .FirstOrDefault();

        var languagePairs = completed
            .GroupBy(request =>
                $"{request.SourceLanguage.ToUpperInvariant()} → {request.TargetLanguage.ToUpperInvariant()}")
            .Select(group => new DashboardNamedCount(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name)
            .Take(5)
            .ToList();

        var lineCountsByRequest = recentLines
            .GroupBy(line => line.TranslationRequestId)
            .ToDictionary(group => group.Key, group => group.Count());
        var bucketStart = new DateTime(
            start.Year, start.Month, start.Day, start.Hour, 0, 0, DateTimeKind.Utc);
        var bucketValues = completed
            .GroupBy(request => new DateTime(
                request.CompletedAt!.Value.Year,
                request.CompletedAt.Value.Month,
                request.CompletedAt.Value.Day,
                request.CompletedAt.Value.Hour,
                0,
                0,
                DateTimeKind.Utc))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Files = group.Count(),
                    Lines = group.Sum(request =>
                        lineCountsByRequest.GetValueOrDefault(request.Id))
                });
        var buckets = Enumerable.Range(0, hours + 1)
            .Select(offset =>
            {
                var hour = bucketStart.AddHours(offset);
                var value = bucketValues.GetValueOrDefault(hour);
                return new DashboardActivityBucket(
                    hour,
                    value?.Files ?? 0,
                    value?.Lines ?? 0);
            })
            .Where(bucket => bucket.Hour <= now)
            .ToList();

        var providerEvents = await _dbContext.ProviderOperationalEvents
            .Where(item =>
                item.OccurredAt >= start &&
                item.TranslationRequestId != null)
            .OrderBy(item => item.OccurredAt)
            .Select(item => new
            {
                item.TranslationRequestId,
                item.Provider,
                item.Outcome,
                item.InputTokens,
                item.OutputTokens,
                item.EstimatedCostUsd
            })
            .ToListAsync(cancellationToken);
        var fallbackRecoveries = providerEvents
            .GroupBy(item => item.TranslationRequestId)
            .Count(group =>
            {
                var events = group.ToList();
                var firstFailure = events.FindIndex(item => item.Outcome == "failure");
                return firstFailure >= 0 &&
                       events.Skip(firstFailure + 1).Any(item => item.Outcome == "success");
            });
        var meteredProviders = new HashSet<string>(
            ["openrouter", "openai", "deepseek", "anthropic"],
            StringComparer.OrdinalIgnoreCase);
        var meteredUsage = providerEvents
            .Where(item =>
                item.Outcome == "success" &&
                meteredProviders.Contains(item.Provider) &&
                (item.InputTokens.HasValue || item.OutputTokens.HasValue))
            .ToList();
        var inputTokens = meteredUsage.Sum(item => item.InputTokens ?? 0);
        var outputTokens = meteredUsage.Sum(item => item.OutputTokens ?? 0);
        decimal? estimatedCost = meteredUsage.Count > 0 &&
                                 meteredUsage.All(item => item.EstimatedCostUsd.HasValue)
            ? meteredUsage.Sum(item => item.EstimatedCostUsd!.Value)
            : null;

        var health = await _providerHealth.GetAllAsync(cancellationToken);
        var unavailable = health.Count(provider =>
            provider.State is ProviderHealthStates.RecentlyUnavailable
                or ProviderHealthStates.Unavailable);

        var active = await _dbContext.TranslationRequests.CountAsync(
            request => request.Status == TranslationStatus.Pending ||
                       request.Status == TranslationStatus.InProgress,
            cancellationToken);
        var failed = recentRequests.Count(request =>
            request.Status == TranslationStatus.Failed);

        var narrative = BuildNarrative(
            hours,
            completed.Count,
            active,
            failed,
            checkedRequests.Count,
            passed,
            needsReview,
            fallbackRecoveries,
            unavailable,
            topProviderByFiles,
            inputTokens,
            outputTokens,
            estimatedCost);
        var headline = active > 0
            ? "Lingarr Next is translating now."
            : completed.Count > 0
                ? "Lingarr Next completed work recently."
                : "Lingarr Next has been quiet in this period.";

        return new DashboardActivityResponse(
            hours,
            start,
            now,
            completed.Count,
            recentLines.Count,
            active,
            failed,
            checkedRequests.Count,
            passed,
            needsReview,
            checkedRequests.Count > 0
                ? Math.Round(checkedRequests.Average(request => request.QualityScore!.Value), 1)
                : null,
            fallbackRecoveries,
            unavailable,
            topProviders,
            languagePairs,
            buckets,
            headline,
            narrative);
    }

    private static List<string> BuildNarrative(
        int hours,
        int files,
        int active,
        int failed,
        int checkedFiles,
        int passed,
        int needsReview,
        int fallbacks,
        int unavailableProviders,
        DashboardNamedCount? topProvider,
        long inputTokens,
        long outputTokens,
        decimal? estimatedCostUsd)
    {
        var result = new List<string>
        {
            files == 0
                ? $"No subtitle files completed in the last {hours} hours."
                : $"In the last {hours} hours, Lingarr Next completed {files} subtitle {Plural(files, "file", "files")}."
        };
        if (checkedFiles > 0)
            result.Add($"{passed} {Plural(passed, "file passed", "files passed")} quality checks; {needsReview} {Plural(needsReview, "needs", "need")} review.");
        else if (files > 0)
            result.Add("Recent files do not have quality results yet.");
        if (topProvider != null)
            result.Add($"{topProvider.Name} completed {topProvider.Count:N0} subtitle {Plural(topProvider.Count, "file", "files")}.");
        if (inputTokens > 0 || outputTokens > 0)
        {
            result.Add(estimatedCostUsd.HasValue
                ? $"Metered LLM work used {inputTokens:N0} input tokens and returned {outputTokens:N0} output tokens, with an estimated cost of {FormatCost(estimatedCostUsd.Value)}."
                : $"Metered LLM work used {inputTokens:N0} input tokens and returned {outputTokens:N0} output tokens. Pricing was unavailable for one or more models, so the total cost could not be estimated.");
        }
        if (fallbacks > 0)
            result.Add($"{fallbacks} {Plural(fallbacks, "translation recovered", "translations recovered")} after a provider failure.");
        if (active > 0)
            result.Add($"{active} {Plural(active, "translation is", "translations are")} still running.");
        if (failed > 0)
            result.Add($"{failed} recent {Plural(failed, "translation failed", "translations failed")} and may need attention.");
        result.Add(unavailableProviders == 0
            ? "No configured providers are currently unavailable."
            : $"{unavailableProviders} configured {Plural(unavailableProviders, "provider is", "providers are")} currently unavailable.");
        return result;
    }

    private static string Plural(int count, string singular, string plural) =>
        count == 1 ? singular : plural;

    private static string FormatCost(decimal cost) =>
        cost > 0m && cost < 0.01m
            ? $"${cost:0.0000}"
            : $"${cost:0.00}";

    private static string DisplayName(string provider) =>
        string.Join(' ', provider
            .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
