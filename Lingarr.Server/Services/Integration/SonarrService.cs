using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Integration;

public class SonarrService : ISonarrService
{
    private readonly IIntegrationService _integrationService;

    public SonarrService(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrShow>?> GetShows()
    {
        return await _integrationService.GetApiResponse<List<SonarrShow>>(
            "/api/v3/series/",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrEpisode>?> GetEpisodes(int seriesNumber, int seasonNumber)
    {
        return await _integrationService.GetApiResponse<List<SonarrEpisode>>(
            $"/api/v3/episode?seriesId={seriesNumber}&seasonNumber={seasonNumber}&includeImages=true",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }

    /// <inheritdoc />
    public async Task<SonarrEpisode?> GetEpisode(int episodeNumber)
    {
        return await _integrationService.GetApiResponse<SonarrEpisode>(
            $"/api/v3/episode/{episodeNumber}?includeImages=true",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
    
    /// <inheritdoc />
    public async Task<SonarrEpisodePath?> GetEpisodePath(int episodeId)
    {
        return await _integrationService.GetApiResponse<SonarrEpisodePath>(
            $"/api/v3/episode/{episodeId}",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }

    /// <inheritdoc />
    public async Task<SonarrEpisode?> FindLibraryEpisode(
        string kind,
        string showId,
        int seasonNumber,
        int episodeNumber)
    {
        if (string.IsNullOrWhiteSpace(showId))
        {
            return null;
        }

        var filter = kind.ToLowerInvariant() switch
        {
            "tvdb" => "tvdbId=" + Uri.EscapeDataString(showId),
            "tmdb" => "tmdbId=" + Uri.EscapeDataString(showId),
            "imdb" => "imdbId=" + Uri.EscapeDataString(showId),
            _ => null
        };
        if (filter == null)
        {
            return null;
        }

        var shows = await _integrationService.GetApiResponse<List<SonarrShow>>(
            "/api/v3/series?" + filter,
            Keys);
        var show = shows?.FirstOrDefault();
        if (show == null)
        {
            return null;
        }

        var episodes = await GetEpisodes(show.Id, seasonNumber);
        if (episodes == null || episodes.Count == 0)
        {
            return null;
        }

        return episodes.FirstOrDefault(episode => episode.EpisodeNumber == episodeNumber && episode.HasFile)
            ?? episodes.FirstOrDefault(episode => episode.EpisodeNumber == episodeNumber);
    }

    private static IntegrationSettingKeys Keys => new()
    {
        Url = "sonarr_url",
        ApiKey = "sonarr_api_key"
    };
}