using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Integration;

public class RadarrService : IRadarrService
{
    private readonly IIntegrationService _integrationService;

    public RadarrService(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    /// <inheritdoc />
    public async Task<List<RadarrMovie>?> GetMovies()
    {
        return await _integrationService.GetApiResponse<List<RadarrMovie>>(
            "/api/v3/movie/",
            new IntegrationSettingKeys
            {
                Url = "radarr_url",
                ApiKey = "radarr_api_key"
            });
    }

    /// <inheritdoc />
    public async Task<RadarrMovie?> GetMovie(int moveId)
    {
        return await _integrationService.GetApiResponse<RadarrMovie>(
            $"/api/v3/movie/{moveId}",
            new IntegrationSettingKeys
            {
                Url = "radarr_url",
                ApiKey = "radarr_api_key"
            });
    }

    /// <inheritdoc />
    public async Task<RadarrMovie?> FindLibraryMovie(string kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var filter = kind.ToLowerInvariant() switch
        {
            "tmdb" => "tmdbId=" + Uri.EscapeDataString(id),
            "imdb" => "imdbId=" + Uri.EscapeDataString(id),
            _ => null
        };
        if (filter == null)
        {
            return null;
        }

        var movies = await _integrationService.GetApiResponse<List<RadarrMovie>>(
            "/api/v3/movie?" + filter,
            new IntegrationSettingKeys
            {
                Url = "radarr_url",
                ApiKey = "radarr_api_key"
            });
        if (movies == null || movies.Count == 0)
        {
            return null;
        }

        return movies.FirstOrDefault(movie => movie.HasFile) ?? movies[0];
    }

    /// <inheritdoc />
    public Task<List<int>> GetImportedMovieIdsSince(DateTime since) =>
        ArrImportHistory.GetIdsSince(
            _integrationService,
            "/api/v3/history",
            new IntegrationSettingKeys { Url = "radarr_url", ApiKey = "radarr_api_key" },
            since,
            record => record.MovieId);
}