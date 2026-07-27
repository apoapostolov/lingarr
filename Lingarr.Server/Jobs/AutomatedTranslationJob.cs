using Hangfire;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Core.Interfaces;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class AutomatedTranslationJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<AutomatedTranslationJob> _logger;
    private readonly IMediaSubtitleProcessor _mediaSubtitleProcessor;
    private readonly ISettingService _settingService;
    private readonly IScheduleService _scheduleService;
    private int _maxTranslationsPerRun = 10;
    private TimeSpan _defaultMovieAgeThreshold;
    private TimeSpan _defaultShowAgeThreshold;

    /// <summary>
    /// Max entities loaded per DB window while walking the automation cycle.
    /// Keeps memory bounded on large libraries (25k+ episodes).
    /// </summary>
    private const int ScanWindowSize = 64;

    public AutomatedTranslationJob(
        LingarrDbContext dbContext,
        ILogger<AutomatedTranslationJob> logger,
        IMediaSubtitleProcessor mediaSubtitleProcessor,
        IScheduleService scheduleService,
        ISettingService settingService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _settingService = settingService;
        _scheduleService = scheduleService;
        _mediaSubtitleProcessor = mediaSubtitleProcessor;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 0)]
    [Queue("translation")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

        var settings = await _settingService.GetSettings([
            SettingKeys.Automation.AutomationEnabled,
            SettingKeys.Automation.TranslationCycle,
            SettingKeys.Automation.MaxTranslationsPerRun,
            SettingKeys.Automation.MovieAgeThreshold,
            SettingKeys.Automation.ShowAgeThreshold
        ]);

        if (settings.GetValueOrDefault(SettingKeys.Automation.AutomationEnabled) == "false")
        {
            _logger.LogInformation("Automation not enabled, skipping translation automation.");
            return;
        }

        int.TryParse(settings.GetValueOrDefault(SettingKeys.Automation.MaxTranslationsPerRun), out int maxTranslations);
        int.TryParse(settings.GetValueOrDefault(SettingKeys.Automation.MovieAgeThreshold), out int movieAgeThreshold);
        int.TryParse(settings.GetValueOrDefault(SettingKeys.Automation.ShowAgeThreshold), out int showAgeThreshold);

        _maxTranslationsPerRun = maxTranslations > 0 ? maxTranslations : 10;
        _defaultMovieAgeThreshold = TimeSpan.FromHours(movieAgeThreshold);
        _defaultShowAgeThreshold = TimeSpan.FromHours(showAgeThreshold);

        var translationCycle = settings.GetValueOrDefault(SettingKeys.Automation.TranslationCycle) == "true" ? "movies" : "shows";
        _logger.LogInformation("Starting translation cycle for |Green|{Cycle}|/Green|", translationCycle);

        var translationsPerformed = 0;
        switch (translationCycle)
        {
            case "movies":
                await _settingService.SetSetting(SettingKeys.Automation.TranslationCycle, "false");
                translationsPerformed += await ProcessMovies(_maxTranslationsPerRun);
                if (translationsPerformed < _maxTranslationsPerRun)
                {
                    await ProcessShows(_maxTranslationsPerRun - translationsPerformed);
                }

                break;
            case "shows":
                await _settingService.SetSetting(SettingKeys.Automation.TranslationCycle, "true");
                translationsPerformed += await ProcessShows(_maxTranslationsPerRun);
                if (translationsPerformed < _maxTranslationsPerRun)
                {
                    await ProcessMovies(_maxTranslationsPerRun - translationsPerformed);
                }

                break;
        }

        await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
    }

    private bool ShouldProcessMedia(IMedia media, MediaType mediaType, TimeSpan? customAgeThreshold = null)
    {
        if (media.Path == null)
        {
            return false;
        }

        var fileInfo = new FileInfo(media.Path);
        TimeSpan fileAge;
        if (!fileInfo.Exists)
        {
            if (!media.DateAdded.HasValue)
            {
                return false;
            }

            fileAge = DateTime.UtcNow - media.DateAdded.Value.ToUniversalTime();
        }
        else
        {
            fileAge = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
        }

        var threshold = customAgeThreshold ??
                        (mediaType == MediaType.Movie ? _defaultMovieAgeThreshold : _defaultShowAgeThreshold);

        var fileAgeHours = fileAge.TotalHours;
        var thresholdHours = threshold.TotalHours;
        if (fileAgeHours >= thresholdHours)
        {
            return true;
        }

        _logger.LogDebug(
            "Media {FileName} does not meet age threshold. Age: {Age} hours, Required: {Threshold} hours",
            media.FileName,
            fileAgeHours.ToString("F2"),
            thresholdHours.ToString("F2"));
        return false;
    }

    private async Task<int> ProcessMovies(int limit)
    {
        _logger.LogInformation("Movie Translation job initiated");

        var totalCount = await _dbContext.Movies
            .Where(movie => movie.IncludeInTranslation)
            .CountAsync();

        if (totalCount == 0)
        {
            _logger.LogInformation("No translatable movies found.");
            return 0;
        }

        var currentIndex = await GetProcessingIndexAsync(SettingKeys.Automation.MovieProcessingIndex);
        if (currentIndex >= totalCount)
        {
            currentIndex = 0;
            _logger.LogInformation("Movie processing cycle completed. Starting new cycle from the beginning.");
        }

        _logger.LogInformation(
            "Processing up to {MaxTranslations} movies starting at {StartIndex} out of {TotalCount}",
            limit,
            currentIndex,
            totalCount);

        var translationsInitiated = 0;
        var scannedMovies = 0;
        var index = currentIndex;

        while (translationsInitiated < limit && scannedMovies < totalCount)
        {
            var remainingInPass = totalCount - scannedMovies;
            var window = Math.Min(ScanWindowSize, remainingInPass);
            // Wrap: load from index to end, then from 0 if needed within this window.
            var batch = await LoadMovieWindow(index, window, totalCount);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var movie in batch)
            {
                if (translationsInitiated >= limit || scannedMovies >= totalCount)
                {
                    break;
                }

                try
                {
                    TimeSpan? threshold = movie.TranslationAgeThreshold.HasValue
                        ? TimeSpan.FromHours(movie.TranslationAgeThreshold.Value)
                        : null;

                    if (!ShouldProcessMedia(movie, MediaType.Movie, threshold))
                    {
                        continue;
                    }

                    var isProcessed = await _mediaSubtitleProcessor.ProcessMedia(movie, MediaType.Movie);
                    if (isProcessed)
                    {
                        translationsInitiated++;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    _logger.LogWarning("Directory not found at path: |Red|{Path}|/Red|, skipping subtitle", movie.Path);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex,
                        "I/O or memory error processing movie at path: |Red|{Path}|/Red|, skipping",
                        movie.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing subtitles for movie at path: |Red|{Path}|/Red|, skipping subtitle",
                        movie.Path);
                }
                finally
                {
                    index = (index + 1) % totalCount;
                    scannedMovies++;
                }
            }
        }

        await SetProcessingIndexAsync(SettingKeys.Automation.MovieProcessingIndex, index);
        return translationsInitiated;
    }

    private async Task<List<Movie>> LoadMovieWindow(int startIndex, int window, int totalCount)
    {
        // Two-slice load when the window wraps past the end of the ordered set.
        var firstTake = Math.Min(window, totalCount - startIndex);
        var movies = await _dbContext.Movies
            .Where(movie => movie.IncludeInTranslation)
            .OrderBy(movie => movie.Id)
            .Skip(startIndex)
            .Take(firstTake)
            .ToListAsync();

        if (movies.Count < window && startIndex + firstTake >= totalCount)
        {
            var wrapTake = window - movies.Count;
            var wrapped = await _dbContext.Movies
                .Where(movie => movie.IncludeInTranslation)
                .OrderBy(movie => movie.Id)
                .Take(wrapTake)
                .ToListAsync();
            movies.AddRange(wrapped);
        }

        return movies;
    }

    private async Task<int> ProcessShows(int limit)
    {
        _logger.LogInformation("Show Translation job initiated");

        var baseQuery =
            from episode in _dbContext.Episodes
            join season in _dbContext.Seasons on episode.SeasonId equals season.Id
            join show in _dbContext.Shows on season.ShowId equals show.Id
            where show.IncludeInTranslation
                  && season.IncludeInTranslation
                  && episode.IncludeInTranslation
            orderby episode.Id
            select new EpisodeAutomationRow
            {
                Episode = episode,
                ShowThreshold = show.TranslationAgeThreshold
            };

        var totalCount = await baseQuery.CountAsync();
        if (totalCount == 0)
        {
            _logger.LogInformation("No translatable shows found.");
            return 0;
        }

        var currentIndex = await GetProcessingIndexAsync(SettingKeys.Automation.ShowProcessingIndex);
        if (currentIndex >= totalCount)
        {
            currentIndex = 0;
            _logger.LogInformation("Show processing cycle completed. Starting new cycle from the beginning.");
        }

        _logger.LogInformation(
            "Processing up to {MaxTranslations} episodes starting at {StartIndex} out of {TotalCount}",
            limit,
            currentIndex,
            totalCount);

        var translationsInitiated = 0;
        var scannedEpisodes = 0;
        var episodeIndex = currentIndex;

        while (translationsInitiated < limit && scannedEpisodes < totalCount)
        {
            var remainingInPass = totalCount - scannedEpisodes;
            var window = Math.Min(ScanWindowSize, remainingInPass);
            var batch = await LoadEpisodeWindow(baseQuery, episodeIndex, window, totalCount);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var row in batch)
            {
                if (translationsInitiated >= limit || scannedEpisodes >= totalCount)
                {
                    break;
                }

                var episode = row.Episode;
                try
                {
                    TimeSpan? threshold = row.ShowThreshold.HasValue
                        ? TimeSpan.FromHours(row.ShowThreshold.Value)
                        : null;

                    if (!ShouldProcessMedia(episode, MediaType.Episode, threshold))
                    {
                        continue;
                    }

                    var isProcessed = await _mediaSubtitleProcessor.ProcessMedia(episode, MediaType.Episode);
                    if (isProcessed)
                    {
                        translationsInitiated++;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    _logger.LogWarning("Directory not found for show at path: |Red|{Path}|/Red|, skipping episode",
                        episode.Path);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex,
                        "I/O or memory error processing episode at path: |Red|{Path}|/Red|, skipping",
                        episode.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing subtitles for episode at path: |Red|{Path}|/Red|, skipping episode",
                        episode.Path);
                }
                finally
                {
                    episodeIndex = (episodeIndex + 1) % totalCount;
                    scannedEpisodes++;
                }
            }
        }

        await SetProcessingIndexAsync(SettingKeys.Automation.ShowProcessingIndex, episodeIndex);
        return translationsInitiated;
    }

    private async Task<List<EpisodeAutomationRow>> LoadEpisodeWindow(
        IQueryable<EpisodeAutomationRow> baseQuery,
        int startIndex,
        int window,
        int totalCount)
    {
        var firstTake = Math.Min(window, totalCount - startIndex);
        var rows = await baseQuery
            .Skip(startIndex)
            .Take(firstTake)
            .ToListAsync();

        if (rows.Count < window && startIndex + firstTake >= totalCount)
        {
            var wrapTake = window - rows.Count;
            var wrapped = await baseQuery
                .Take(wrapTake)
                .ToListAsync();
            rows.AddRange(wrapped);
        }

        return rows;
    }

    private async Task<int> GetProcessingIndexAsync(string key)
    {
        var raw = await _settingService.GetSetting(key);
        return int.TryParse(raw, out var index) && index >= 0 ? index : 0;
    }

    private async Task SetProcessingIndexAsync(string key, int value)
    {
        await _settingService.UpsertSetting(key, value.ToString());
    }

    private sealed class EpisodeAutomationRow
    {
        public Episode Episode { get; set; } = null!;
        public int? ShowThreshold { get; set; }
    }
}
