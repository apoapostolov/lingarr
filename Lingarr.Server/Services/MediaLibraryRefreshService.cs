using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Integration.Plex;

namespace Lingarr.Server.Services;

public class MediaLibraryRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingService _settings;
    private readonly IPlexClient _plex;
    private readonly ILogger<MediaLibraryRefreshService> _logger;

    public MediaLibraryRefreshService(
        IHttpClientFactory httpClientFactory,
        ISettingService settings,
        IPlexClient plex,
        ILogger<MediaLibraryRefreshService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _plex = plex;
        _logger = logger;
    }

    public async Task RefreshFoldersAsync(IReadOnlyCollection<string> folders, CancellationToken cancellationToken)
    {
        var distinct = folders
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
        {
            return;
        }

        var plex = await PlexCredentials.ResolveAsync(_settings);
        var jellyfinUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("JELLYFIN_URL"),
            await _settings.GetSetting(SettingKeys.MediaServers.JellyfinUrl));
        var jellyfinToken = FirstNonEmpty(
            ReadTokenFile(Environment.GetEnvironmentVariable("JELLYFIN_TOKEN_FILE")),
            Environment.GetEnvironmentVariable("JELLYFIN_API_KEY"),
            Environment.GetEnvironmentVariable("JELLYFIN_TOKEN"),
            await _settings.GetEncryptedSetting(SettingKeys.MediaServers.JellyfinToken));

        foreach (var folder in distinct)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var windowsPath = ToWindowsMediaPath(folder);
            if (!string.IsNullOrWhiteSpace(jellyfinUrl) && !string.IsNullOrWhiteSpace(jellyfinToken))
            {
                await RefreshJellyfinAsync(jellyfinUrl, jellyfinToken, windowsPath, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(plex.Url) && !string.IsNullOrWhiteSpace(plex.Token))
            {
                await RefreshPlexAsync(plex.Url, plex.Token, plex.ClientId, windowsPath, cancellationToken);
            }
        }
    }

    internal static string ToWindowsMediaPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var match = Regex.Match(normalized, @"^/media/media/(.*)$");
        if (match.Success)
        {
            return @"E:\media\" + match.Groups[1].Value.Replace('/', '\\');
        }

        match = Regex.Match(normalized, @"^/mnt/([a-zA-Z])/(.*)$");
        if (match.Success)
        {
            return $"{match.Groups[1].Value.ToUpperInvariant()}:\\{match.Groups[2].Value.Replace('/', '\\')}";
        }

        return path;
    }

    private async Task RefreshJellyfinAsync(
        string baseUrl,
        string token,
        string windowsPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/Library/Media/Updated");
            request.Headers.TryAddWithoutValidation("X-Emby-Token", token);
            var payload = JsonSerializer.Serialize(new
            {
                Updates = new[]
                {
                    new { Path = windowsPath, UpdateType = "Modified" }
                }
            });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Jellyfin refresh returned {Status} for {Path}",
                    (int)response.StatusCode,
                    windowsPath);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Jellyfin refresh failed for {Path}", windowsPath);
        }
    }

    private async Task RefreshPlexAsync(
        string baseUrl,
        string token,
        string clientId,
        string windowsPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var folderName = Path.GetFileName(windowsPath.TrimEnd('\\'));
            var titleHint = Regex.Replace(folderName, @"\{[^}]+\}", "");
            titleHint = Regex.Replace(titleHint, @"\[[^\]]+\]", "");
            titleHint = Regex.Replace(titleHint, @"\(\d{4}\)", "").Trim(' ', '.', '-', '_');
            if (string.IsNullOrWhiteSpace(titleHint))
            {
                titleHint = folderName;
            }

            var ratingKeys = await _plex.SearchRatingKeysAsync(
                baseUrl,
                token,
                clientId,
                titleHint,
                cancellationToken);
            var ratingKey = ratingKeys.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ratingKey))
            {
                _logger.LogDebug("Plex item not found for {Path}", windowsPath);
                return;
            }

            await _plex.RefreshMetadataAsync(baseUrl, token, clientId, ratingKey, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Plex refresh failed for {Path}", windowsPath);
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? ReadTokenFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(path).Trim();
        }
        catch
        {
            return null;
        }
    }
}
