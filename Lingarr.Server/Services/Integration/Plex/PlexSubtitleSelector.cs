using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Plex;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services.Integration.Plex;

public sealed class PlexPollOptions
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(2);
    public int RefreshAttempts { get; init; } = 15;
    public int UploadAttempts { get; init; } = 5;
}

public sealed class PlexSubtitleSelector : IPlexSubtitleSelector
{
    private readonly ISettingService _settings;
    private readonly IPlexClient _plex;
    private readonly LingarrDbContext _dbContext;
    private readonly PlexPollOptions _poll;
    private readonly ILogger<PlexSubtitleSelector> _logger;

    public PlexSubtitleSelector(
        ISettingService settings,
        IPlexClient plex,
        LingarrDbContext dbContext,
        PlexPollOptions poll,
        ILogger<PlexSubtitleSelector> logger)
    {
        _settings = settings;
        _plex = plex;
        _dbContext = dbContext;
        _poll = poll;
        _logger = logger;
    }

    public async Task ApplyTranslatedSubtitleAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ApplyCoreAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Plex subtitle update failed for translation request {RequestId}. The translated file remains available.",
                request.Id);
        }
    }

    private async Task ApplyCoreAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        var enabled = await _settings.GetSetting(SettingKeys.MediaServers.PlexSetSelectedSubtitle);
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var defaultLanguage = await _settings.GetSetting(SettingKeys.MediaServers.PlexDefaultSubtitleLanguage);
        if (string.IsNullOrWhiteSpace(defaultLanguage))
        {
            _logger.LogWarning(
                "Plex subtitle selection is on without a default language. Translation request {RequestId} was left unchanged.",
                request.Id);
            return;
        }

        var credentials = await PlexCredentials.ResolveAsync(_settings);
        if (string.IsNullOrWhiteSpace(credentials.Url) || string.IsNullOrWhiteSpace(credentials.Token))
        {
            return;
        }

        var subtitlePath = request.TranslatedSubtitle;
        if (request.MediaId == null
            || request.MediaType is not (MediaType.Movie or MediaType.Episode)
            || string.IsNullOrWhiteSpace(subtitlePath))
        {
            return;
        }

        var media = await LoadMediaAsync(request, cancellationToken);
        if (media == null)
        {
            return;
        }

        var match = await FindPartAsync(credentials, media, cancellationToken);
        if (match == null)
        {
            _logger.LogWarning(
                "Plex has no item for {Title} in {Directory}. Translation request {RequestId} was not updated.",
                media.Title,
                media.Directory,
                request.Id);
            return;
        }

        _logger.LogInformation(
            "Matched Plex item {RatingKey} for {Title} by {Reason}.",
            match.RatingKey,
            media.Title,
            match.MatchReason);

        if (!string.IsNullOrWhiteSpace(match.LibrarySectionId) && !string.IsNullOrWhiteSpace(match.PlexDirectory))
        {
            await _plex.RefreshSectionPathAsync(
                credentials.Url,
                credentials.Token,
                credentials.ClientId,
                match.LibrarySectionId,
                match.PlexDirectory,
                cancellationToken);
        }

        await _plex.RefreshMetadataAsync(
            credentials.Url,
            credentials.Token,
            credentials.ClientId,
            match.RatingKey,
            cancellationToken);

        var stream = await PollForStreamAsync(
            credentials,
            match,
            media.Directory,
            media.Stem,
            request.TargetLanguage,
            preferredTitle: null,
            _poll.RefreshAttempts,
            cancellationToken);

        if (stream == null)
        {
            stream = await UploadAndPollAsync(
                credentials,
                match,
                media.Directory,
                media.Stem,
                subtitlePath,
                request.TargetLanguage,
                request.Id,
                cancellationToken);
        }

        if (stream == null)
        {
            _logger.LogWarning(
                "Plex did not list the translated subtitle for {RatingKey}. Translation request {RequestId} was not selected.",
                match.RatingKey,
                request.Id);
            return;
        }

        if (!PlexLanguage.Same(request.TargetLanguage, defaultLanguage))
        {
            return;
        }

        await _plex.SetSelectedSubtitleAsync(
            credentials.Url,
            credentials.Token,
            credentials.ClientId,
            match.PartId,
            stream.Id,
            allParts: match.PartCount <= 1,
            cancellationToken);
    }

    private async Task<PlexSubtitleStream?> UploadAndPollAsync(
        ResolvedPlexCredentials credentials,
        PlexPartMatch match,
        string directory,
        string stem,
        string subtitlePath,
        string targetLanguage,
        int requestId,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(subtitlePath))
        {
            _logger.LogWarning(
                "Translated subtitle {Path} is missing, so Plex could not be given the file for request {RequestId}.",
                subtitlePath,
                requestId);
            return null;
        }

        var fileName = Path.GetFileName(subtitlePath);
        var format = Path.GetExtension(subtitlePath).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(format))
        {
            format = "srt";
        }

        var language = PlexLanguage.TwoLetter(targetLanguage);
        var bytes = await File.ReadAllBytesAsync(subtitlePath, cancellationToken);
        _logger.LogInformation(
            "Plex did not find {FileName} after refresh. Uploading it onto item {RatingKey} as {Language}.",
            fileName,
            match.RatingKey,
            language);
        await _plex.UploadSubtitleAsync(
            credentials.Url!,
            credentials.Token!,
            credentials.ClientId,
            match.RatingKey,
            fileName,
            format,
            language,
            bytes,
            cancellationToken);

        return await PollForStreamAsync(
            credentials,
            match,
            directory,
            stem,
            targetLanguage,
            fileName,
            _poll.UploadAttempts,
            cancellationToken);
    }

    private async Task<PlexSubtitleStream?> PollForStreamAsync(
        ResolvedPlexCredentials credentials,
        PlexPartMatch match,
        string directory,
        string stem,
        string targetLanguage,
        string? preferredTitle,
        int attempts,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (attempt > 0 && _poll.Interval > TimeSpan.Zero)
            {
                await Task.Delay(_poll.Interval, cancellationToken);
            }

            var metadata = await _plex.GetMetadataAsync(
                credentials.Url!,
                credentials.Token!,
                credentials.ClientId,
                match.RatingKey,
                cancellationToken);
            var current = PlexMatcher.MatchMetadata(
                metadata,
                PlexMatcher.DirectoryCandidates(directory),
                stem);
            var picked = PlexMatcher.PickNewSubtitle(
                match.Subtitles,
                current?.Subtitles ?? [],
                targetLanguage,
                preferredTitle);
            if (picked != null)
            {
                return picked;
            }
        }

        return null;
    }

    private async Task<PlexPartMatch?> FindPartAsync(
        ResolvedPlexCredentials credentials,
        LocalMedia media,
        CancellationToken cancellationToken)
    {
        var lookup = PlexLookup.Create(media.Title, media.Directory, media.Stem);
        var directories = PlexMatcher.DirectoryCandidates(media.Directory);
        PlexPartMatch? nameMatch = null;
        foreach (var query in lookup.SearchQueries())
        {
            IReadOnlyList<string> ratingKeys;
            try
            {
                ratingKeys = await _plex.SearchRatingKeysAsync(
                    credentials.Url!,
                    credentials.Token!,
                    credentials.ClientId,
                    query,
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                continue;
            }

            foreach (var ratingKey in ratingKeys)
            {
                var match = await MatchRatingKeyAsync(
                    credentials,
                    ratingKey,
                    lookup,
                    directories,
                    cancellationToken);
                if (match == null)
                {
                    continue;
                }

                if (match.MatchReason is "path" or "id")
                {
                    return match;
                }

                nameMatch ??= match;
            }
        }

        var scanned = await FindInLibraryAsync(credentials, media.Type, lookup, directories, cancellationToken);
        return scanned ?? nameMatch;
    }

    private async Task<PlexPartMatch?> FindInLibraryAsync(
        ResolvedPlexCredentials credentials,
        MediaType mediaType,
        PlexLookup lookup,
        IReadOnlyCollection<string> directories,
        CancellationToken cancellationToken)
    {
        var wanted = mediaType == MediaType.Movie ? "movie" : "show";
        var metadataType = mediaType == MediaType.Movie ? 1 : 4;
        var sections = await _plex.GetLibrariesAsync(
            credentials.Url!,
            credentials.Token!,
            credentials.ClientId,
            cancellationToken);
        PlexPartMatch? nameMatch = null;
        foreach (var section in sections.Where(section =>
                     string.Equals(section.Type, wanted, StringComparison.OrdinalIgnoreCase)))
        {
            var start = 0;
            const int pageSize = 100;
            while (true)
            {
                var page = await _plex.GetLibraryPageAsync(
                    credentials.Url!,
                    credentials.Token!,
                    credentials.ClientId,
                    section.Key,
                    metadataType,
                    start,
                    pageSize,
                    cancellationToken);
                var scan = PlexMatcher.MatchPage(page, lookup, directories);
                if (scan.PathMatch != null || scan.GuidMatch != null)
                {
                    return scan.PathMatch ?? scan.GuidMatch;
                }

                nameMatch ??= scan.NameMatch;
                var total = scan.TotalSize ?? 0;
                start += pageSize;
                if (scan.ItemCount == 0 || start >= total)
                {
                    break;
                }
            }
        }

        return nameMatch;
    }

    private async Task<PlexPartMatch?> MatchRatingKeyAsync(
        ResolvedPlexCredentials credentials,
        string ratingKey,
        PlexLookup lookup,
        IReadOnlyCollection<string> directories,
        CancellationToken cancellationToken)
    {
        string metadata;
        try
        {
            metadata = await _plex.GetMetadataAsync(
                credentials.Url!,
                credentials.Token!,
                credentials.ClientId,
                ratingKey,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        var scan = PlexMatcher.MatchPage(metadata, lookup, directories);
        return scan.PathMatch ?? scan.GuidMatch ?? scan.NameMatch;
    }

    private async Task<LocalMedia?> LoadMediaAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MediaType == MediaType.Movie)
        {
            var movie = await _dbContext.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.MediaId, cancellationToken);
            if (movie == null || string.IsNullOrWhiteSpace(movie.Path) || string.IsNullOrWhiteSpace(movie.FileName))
            {
                return null;
            }

            return new LocalMedia(movie.Path, movie.FileName, movie.Title, MediaType.Movie);
        }

        var episode = await _dbContext.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.MediaId, cancellationToken);
        if (episode == null || string.IsNullOrWhiteSpace(episode.Path) || string.IsNullOrWhiteSpace(episode.FileName))
        {
            return null;
        }

        return new LocalMedia(episode.Path, episode.FileName, episode.Title, MediaType.Episode);
    }

    private sealed record LocalMedia(string Directory, string Stem, string Title, MediaType Type);
}
