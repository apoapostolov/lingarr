using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Plex;

namespace Lingarr.Server.Services.Integration.Plex;

public static class PlexCredentials
{
    public static async Task<ResolvedPlexCredentials> ResolveAsync(ISettingService settings)
    {
        var fileToken = ReadTokenFile(Environment.GetEnvironmentVariable("PLEX_TOKEN_FILE"));
        var envToken = Environment.GetEnvironmentVariable("PLEX_TOKEN");
        string? settingToken = null;
        try
        {
            settingToken = await settings.GetEncryptedSetting(SettingKeys.MediaServers.PlexToken);
        }
        catch (Exception)
        {
            settingToken = null;
        }

        var ignoreEnvironment = string.Equals(
            await settings.GetSetting(SettingKeys.MediaServers.PlexIgnoreEnvironment),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var authMethod = await settings.GetSetting(SettingKeys.MediaServers.PlexAuthMethod);
        var savedUrl = await settings.GetSetting(SettingKeys.MediaServers.PlexUrl);
        var chosen = Choose(
            Environment.GetEnvironmentVariable("PLEX_URL"),
            First(fileToken, envToken),
            savedUrl,
            settingToken,
            authMethod,
            ignoreEnvironment,
            clientId: "");
        var token = chosen.Token;
        var url = chosen.Url;
        var source = chosen.Source;
        var clientId = await settings.GetSetting(SettingKeys.MediaServers.PlexClientIdentifier);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = Guid.NewGuid().ToString("D");
            await settings.SetSetting(SettingKeys.MediaServers.PlexClientIdentifier, clientId);
        }

        return new ResolvedPlexCredentials(url, token, source, clientId);
    }

    public static ResolvedPlexCredentials Choose(
        string? environmentUrl,
        string? environmentToken,
        string? savedUrl,
        string? savedToken,
        string? authMethod,
        bool ignoreEnvironment,
        string clientId)
    {
        var explicitSession = authMethod is "oauth" or "token"
            && !string.IsNullOrWhiteSpace(savedToken);
        if (explicitSession)
        {
            return new ResolvedPlexCredentials(savedUrl, savedToken, "setting", clientId);
        }

        if (ignoreEnvironment)
        {
            return new ResolvedPlexCredentials(null, null, "setting", clientId);
        }

        var token = First(environmentToken, savedToken);
        var url = First(environmentUrl, savedUrl);
        var source = !string.IsNullOrWhiteSpace(environmentToken) ? "environment" : "setting";
        return new ResolvedPlexCredentials(url, token, source, clientId);
    }

    private static string? First(params string?[] values) =>
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
        catch (Exception)
        {
            return null;
        }
    }
}
