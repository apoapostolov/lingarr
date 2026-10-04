using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Integration;

/// <summary>
/// Defines a service for interacting with the Radarr API.
/// </summary>
public interface IRadarrService
{
    /// <summary>
    /// Asynchronously retrieves a list of movies from the Radarr API.
    /// </summary>
    /// <returns>
    /// This method calls the Radarr API to fetch movies
    /// The task result contains a <see cref="List{T}"/> of <see cref="RadarrMovie"/>
    /// objects representing all movies, or <c>null</c> if the API call fails.
    /// </returns>
    Task<List<RadarrMovie>?> GetMovies();

    /// <summary>
    /// Asynchronously retrieves a movie from the Radarr API.
    /// </summary>
    /// <returns>
    /// This method calls the Radarr API to fetch a movie
    /// The task result contains a <see cref="RadarrMovie"/>
    /// objects representing the movie, or <c>null</c> if the API call fails.
    /// </returns>
    Task<RadarrMovie?> GetMovie(int MovieId);

    /// <summary>
    /// Finds one movie already in the Radarr library by a tmdb or imdb id.
    /// </summary>
    /// <returns>The movie that has a file, or the first match, or null.</returns>
    Task<RadarrMovie?> FindLibraryMovie(string kind, string id);

    /// <summary>
    /// Movie ids imported into Radarr since the given time.
    /// </summary>
    Task<List<int>> GetImportedMovieIdsSince(DateTime since);
}