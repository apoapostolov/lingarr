namespace Lingarr.Server.Interfaces.Services;

public interface ILibraryLightDiscovery
{
    /// <summary>
    /// Uses Plex, Radarr, and Sonarr to find new or unfinished items.
    /// Returns null when none of those services are configured.
    /// </summary>
    Task<int?> TryDiscover(int translationLimit, CancellationToken cancellationToken);
}
