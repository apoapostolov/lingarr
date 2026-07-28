using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Dashboard;
using Lingarr.Server.Models.ProviderHealth;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class DashboardActivityService : IDashboardActivityService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISettingService _settings;
    private readonly IProviderHealthService _providerHealth;

    public DashboardActivityService(
        LingarrDbContext dbContext,
        ISettingService settings,
        IProviderHealthService providerHealth)
    {
        _dbContext = dbContext;
        _settings = settings;
        _providerHealth = providerHealth;
    }

    public async Task<DashboardActivityResponse> GetAsync(
        int? requestedHours = null,
        CancellationToken cancellationToken = default)
    {
        var configured = 48;
        if (!requestedHours.HasValue)
        {
            var setting = await _settings.GetSetting(SettingKeys.Dashboard.ActivityWindowHours);
            if (int.TryParse(setting, out var parsed))
                configured = parsed;
        }

        var hours = Math.Clamp(requestedHours ?? configured, 1, 168);
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
                item.Outcome
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
            recentLines.Count,
            active,
            failed,
            checkedRequests.Count,
            passed,
            needsReview,
            fallbackRecoveries,
            unavailable,
            topProviders.FirstOrDefault(),
            languagePairs.FirstOrDefault());
        var headline = active > 0
            ? "Lingarr is translating now."
            : completed.Count > 0
                ? "Lingarr completed work recently."
                : "Lingarr has been quiet in this period.";

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
        int lines,
        int active,
        int failed,
        int checkedFiles,
        int passed,
        int needsReview,
        int fallbacks,
        int unavailableProviders,
        DashboardNamedCount? topProvider,
        DashboardNamedCount? topLanguagePair)
    {
        var result = new List<string>
        {
            files == 0
                ? $"No subtitle files completed in the last {hours} hours."
                : $"In the last {hours} hours, Lingarr completed {files} subtitle {Plural(files, "file", "files")} and translated {lines:N0} dialogue {Plural(lines, "line", "lines")}."
        };
        if (checkedFiles > 0)
            result.Add($"{passed} {Plural(passed, "file passed", "files passed")} quality checks; {needsReview} {Plural(needsReview, "needs", "need")} review.");
        else if (files > 0)
            result.Add("Recent files do not have quality results yet.");
        if (topProvider != null)
            result.Add($"{topProvider.Name} handled most translated lines ({topProvider.Count:N0}).");
        if (topLanguagePair != null)
            result.Add($"The busiest language pair was {topLanguagePair.Name} ({topLanguagePair.Count} {Plural(topLanguagePair.Count, "file", "files")}).");
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

    private static string DisplayName(string provider) =>
        string.Join(' ', provider
            .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
