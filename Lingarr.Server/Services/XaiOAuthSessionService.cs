using System.Net.Http.Headers;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;
using Microsoft.Extensions.Caching.Memory;

namespace Lingarr.Server.Services;

public sealed class XaiOAuthSessionService : IXaiOAuthSessionService
{
    public const string ClientId = "b1a00492-073a-47ea-816f-4c329264a828";
    public const string Scope = "openid profile email offline_access grok-cli:access api:access";
    public const string DeviceCodeEndpoint = "https://auth.x.ai/oauth2/device/code";
    public const string TokenEndpoint = "https://auth.x.ai/oauth2/token";
    public const string VerificationFallback = "https://accounts.x.ai/oauth2/device";
    private const string DeviceGrant = "urn:ietf:params:oauth:grant-type:device_code";
    private const string CachePrefix = "xai-oauth-device:";

    private readonly ISettingService _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<XaiOAuthSessionService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private sealed record PendingDevice(
        string DeviceCode,
        int IntervalSeconds,
        DateTimeOffset ExpiresAt);

    public XaiOAuthSessionService(
        ISettingService settings,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<XaiOAuthSessionService> logger)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<XaiOAuthStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var accessToken = await _settings.GetEncryptedSetting(
            SettingKeys.Translation.XaiOAuth.AccessToken);
        var refreshToken = await _settings.GetEncryptedSetting(
            SettingKeys.Translation.XaiOAuth.RefreshToken);
        var expiresAt = ParseExpiresAt(await _settings.GetSetting(
            SettingKeys.Translation.XaiOAuth.ExpiresAt));
        return new XaiOAuthStatusResponse
        {
            Connected = !string.IsNullOrWhiteSpace(accessToken)
                        || !string.IsNullOrWhiteSpace(refreshToken),
            ExpiresAt = expiresAt
        };
    }

    public async Task<XaiOAuthDeviceResponse> StartDeviceFlowAsync(
        CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, DeviceCodeEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"] = Scope
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"xAI device login could not start ({response.StatusCode}): {responseBody}",
                null,
                response.StatusCode);
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var deviceCode = GetString(root, "device_code", "deviceCode");
        var userCode = GetString(root, "user_code", "userCode");
        if (string.IsNullOrWhiteSpace(deviceCode) || string.IsNullOrWhiteSpace(userCode))
        {
            throw new InvalidOperationException("xAI did not return a device code.");
        }

        var interval = Math.Max(3, GetInt(root, 5, "interval"));
        var expiresIn = Math.Max(60, GetInt(root, 1800, "expires_in", "expiresIn"));
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        var flowId = Guid.NewGuid().ToString("N");
        _cache.Set(
            CachePrefix + flowId,
            new PendingDevice(deviceCode, interval, expiresAt),
            expiresAt);

        return new XaiOAuthDeviceResponse
        {
            FlowId = flowId,
            UserCode = userCode,
            VerificationUri =
                GetString(root, "verification_uri", "verificationUri", "verification_url")
                ?? VerificationFallback,
            VerificationUriComplete = GetString(
                root,
                "verification_uri_complete",
                "verificationUriComplete"),
            IntervalSeconds = interval,
            ExpiresAt = expiresAt
        };
    }

    public async Task<XaiOAuthPollResponse> PollAsync(
        string flowId,
        CancellationToken cancellationToken = default)
    {
        if (!_cache.TryGetValue<PendingDevice>(CachePrefix + flowId, out var pending)
            || pending is null
            || pending.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new XaiOAuthPollResponse
            {
                Status = "expired",
                Message = "The device code expired. Start again."
            };
        }

        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = DeviceGrant,
                ["device_code"] = pending.DeviceCode,
                ["client_id"] = ClientId
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(responseBody);
        }
        catch (JsonException)
        {
            // The caller receives a useful provider error below.
        }

        using (document)
        {
            var root = document?.RootElement;
            if (response.IsSuccessStatusCode && root is not null)
            {
                var accessToken = GetString(root.Value, "access_token", "accessToken");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    await StoreSessionAsync(root.Value, accessToken);
                    _cache.Remove(CachePrefix + flowId);
                    return new XaiOAuthPollResponse { Status = "connected" };
                }
            }

            var error = root is null ? "" : GetString(root.Value, "error") ?? "";
            var description = root is null
                ? null
                : GetString(root.Value, "error_description", "message");
            return error switch
            {
                "authorization_pending" => new XaiOAuthPollResponse
                {
                    Status = "pending",
                    IntervalSeconds = pending.IntervalSeconds
                },
                "slow_down" => new XaiOAuthPollResponse
                {
                    Status = "pending",
                    IntervalSeconds = pending.IntervalSeconds + 5
                },
                "expired_token" or "expired" => new XaiOAuthPollResponse
                {
                    Status = "expired",
                    Message = "The device code expired. Start again."
                },
                "access_denied" or "authorization_denied" => new XaiOAuthPollResponse
                {
                    Status = "denied",
                    Message = description ?? "xAI login was denied."
                },
                _ => new XaiOAuthPollResponse
                {
                    Status = "error",
                    Message = description
                              ?? (!string.IsNullOrWhiteSpace(error) ? error : $"xAI login failed ({response.StatusCode}).")
                }
            };
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var accessToken = await _settings.GetEncryptedSetting(
            SettingKeys.Translation.XaiOAuth.AccessToken);
        var expiresAt = ParseExpiresAt(await _settings.GetSetting(
            SettingKeys.Translation.XaiOAuth.ExpiresAt));
        if (!string.IsNullOrWhiteSpace(accessToken)
            && (expiresAt is null || expiresAt > DateTimeOffset.UtcNow.AddMinutes(1)))
        {
            return accessToken;
        }

        var refreshToken = await _settings.GetEncryptedSetting(
            SettingKeys.Translation.XaiOAuth.RefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            accessToken = await _settings.GetEncryptedSetting(
                SettingKeys.Translation.XaiOAuth.AccessToken);
            expiresAt = ParseExpiresAt(await _settings.GetSetting(
                SettingKeys.Translation.XaiOAuth.ExpiresAt));
            if (!string.IsNullOrWhiteSpace(accessToken)
                && (expiresAt is null || expiresAt > DateTimeOffset.UtcNow.AddMinutes(1)))
            {
                return accessToken;
            }

            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = ClientId
                })
            };
            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "xAI OAuth refresh failed with status {StatusCode}.",
                    response.StatusCode);
                await DisconnectAsync(cancellationToken);
                return null;
            }

            using var document = JsonDocument.Parse(responseBody);
            accessToken = GetString(document.RootElement, "access_token", "accessToken");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                await DisconnectAsync(cancellationToken);
                return null;
            }

            await StoreSessionAsync(document.RootElement, accessToken, refreshToken);
            return accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _settings.SetEncryptedSetting(SettingKeys.Translation.XaiOAuth.AccessToken, "");
        await _settings.SetEncryptedSetting(SettingKeys.Translation.XaiOAuth.RefreshToken, "");
        await _settings.SetSetting(SettingKeys.Translation.XaiOAuth.ExpiresAt, "");
        await _settings.SetSetting(SettingKeys.Translation.XaiOAuth.Connection, "");
    }

    private async Task StoreSessionAsync(
        JsonElement root,
        string accessToken,
        string? existingRefreshToken = null)
    {
        var refreshToken =
            GetString(root, "refresh_token", "refreshToken")
            ?? existingRefreshToken
            ?? "";
        var expiresIn = Math.Max(60, GetInt(root, 3600, "expires_in", "expiresIn"));
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        await _settings.SetEncryptedSetting(
            SettingKeys.Translation.XaiOAuth.AccessToken,
            accessToken);
        await _settings.SetEncryptedSetting(
            SettingKeys.Translation.XaiOAuth.RefreshToken,
            refreshToken);
        await _settings.SetSetting(
            SettingKeys.Translation.XaiOAuth.ExpiresAt,
            expiresAt.ToString("O"));
        await _settings.SetSetting(SettingKeys.Translation.XaiOAuth.Connection, "connected");
    }

    private static DateTimeOffset? ParseExpiresAt(string? value) =>
        DateTimeOffset.TryParse(value, out var result) ? result : null;

    private static string? GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        return null;
    }

    private static int GetInt(JsonElement root, int fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value)
                && value.TryGetInt32(out var result))
            {
                return result;
            }
        }
        return fallback;
    }
}
