using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

/// <summary>
/// Z.ai <b>GLM Coding Plan</b> (subscription quota), OpenAI-compatible chat completions.
/// Uses the Coding endpoint, not the general pay-as-you-go API.
/// Docs: https://docs.z.ai/devpack/tool/others
/// </summary>
public class ZaiService : BaseLanguageService
{
    /// <summary>Global Coding Plan OpenAI Chat Completions base (no trailing slash).</summary>
    public const string CodingPlanGlobalEndpoint = "https://api.z.ai/api/coding/paas/v4";

    /// <summary>China-region Coding Plan OpenAI base.</summary>
    public const string CodingPlanCnEndpoint = "https://open.bigmodel.cn/api/coding/paas/v4";

    /// <summary>General API (pay-as-you-go) — not used for Bedroom Coding Plan.</summary>
    public const string GeneralApiGlobalEndpoint = "https://api.z.ai/api/paas/v4";

    /// <summary>Models officially listed for GLM Coding Plan quota.</summary>
    private static readonly string[] CodingPlanModels =
    [
        "glm-5.2",
        "glm-5-turbo",
        "glm-4.7"
    ];

    private string _endpoint = CodingPlanGlobalEndpoint;
    private readonly HttpClient _httpClient;
    private readonly IRequestTemplateService _requestTemplateService;
    private string? _model;
    private string? _prompt;
    private string? _apiKey;
    private string? _requestTemplate;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public override string? ModelName => _model;

    public ZaiService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<ZaiService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(settings, logger, languageCodeService)
    {
        _httpClient = httpClient;
        _requestTemplateService = requestTemplateService;
    }

    /// <summary>
    /// Maps legacy general-API base URLs to Coding Plan so Bedroom never burns pay-as-you-go by accident.
    /// </summary>
    public static string NormalizeCodingEndpoint(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return CodingPlanGlobalEndpoint;
        }

        var ep = configured.Trim().TrimEnd('/');

        // Common misconfig: general global/CN paas endpoints.
        if (ep.Equals(GeneralApiGlobalEndpoint, StringComparison.OrdinalIgnoreCase)
            || ep.Equals("https://api.z.ai/api/paas/v4/", StringComparison.OrdinalIgnoreCase))
        {
            return CodingPlanGlobalEndpoint;
        }

        if (ep.Contains("open.bigmodel.cn", StringComparison.OrdinalIgnoreCase)
            && ep.Contains("/api/paas/v4", StringComparison.OrdinalIgnoreCase)
            && !ep.Contains("/api/coding/", StringComparison.OrdinalIgnoreCase))
        {
            return CodingPlanCnEndpoint;
        }

        // Already coding, or custom proxy — keep.
        return ep;
    }

    private async Task InitializeAsync(string sourceLanguage, string targetLanguage)
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            var settings = await _settings.GetSettings([
                SettingKeys.Translation.Zai.Model,
                SettingKeys.Translation.Zai.Endpoint,
                SettingKeys.Translation.Zai.RequestTemplate,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.LanguageCodeFormat,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.RequestTimeoutForProvider("zai")
            ]);
            _model = ResolveModel(settings[SettingKeys.Translation.Zai.Model]);
            if (string.IsNullOrWhiteSpace(_model))
            {
                _model = CodingPlanModels[0];
            }

            _endpoint = NormalizeCodingEndpoint(settings.GetValueOrDefault(SettingKeys.Translation.Zai.Endpoint));
            _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.Zai.ApiKey);
            _requestTemplate = !string.IsNullOrEmpty(settings[SettingKeys.Translation.Zai.RequestTemplate])
                ? settings[SettingKeys.Translation.Zai.RequestTemplate]
                : _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.OpenAi.RequestTemplate)
                  ?? _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.Zai.RequestTemplate);
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];
            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Z.ai Coding Plan API key or model is not configured.");
            SetLanguageReplacements(sourceLanguage, targetLanguage, settings[SettingKeys.Translation.LanguageCodeFormat]);
            var rawPrompt = ResolveSystemPrompt(settings[SettingKeys.Translation.AiPrompt]);
            _prompt = !string.IsNullOrWhiteSpace(rawPrompt)
                ? ReplacePlaceholders(rawPrompt, _replacements)
                : $"Translate from {_replacements["sourceLanguage"]} to {_replacements["targetLanguage"]}. Only return the translated text without any additional explanation.";
            _contextPrompt = ResolveContextPrompt(settings[SettingKeys.Translation.AiContextPrompt]);
            _httpClient.Timeout = TimeSpan.FromMinutes(
                TranslationTimeoutPolicy.ResolveMinutes(settings, "zai"));
            _initialized = true;
        }
        finally { _initLock.Release(); }
    }

    public override async Task<string> TranslateAsync(
        string text, string sourceLanguage, string targetLanguage,
        List<string>? contextLinesBefore, List<string>? contextLinesAfter,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);
        text = ApplyContextIfEnabled(text, contextLinesBefore, contextLinesAfter);

        if (string.IsNullOrWhiteSpace(_requestTemplate))
        {
            throw new InvalidOperationException("Z.ai request template is not configured.");
        }

        var body = _requestTemplateService.BuildRequestBody(
            _requestTemplate,
            new Dictionary<string, string>
            {
                ["model"] = _model!,
                ["systemPrompt"] = _prompt ?? $"Translate to {targetLanguage}. Return only the translation.",
                ["userMessage"] = text
            });
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(req, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new TranslationException($"Z.ai Coding Plan error ({response.StatusCode}): {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
        var content = message.TryGetProperty("content", out var c) ? c.GetString() : null;
        // Some GLM responses put draft text only in reasoning_content when thinking is on.
        if (string.IsNullOrWhiteSpace(content)
            && message.TryGetProperty("reasoning_content", out var reasoning))
        {
            content = reasoning.GetString();
        }

        return content?.Trim()
               ?? throw new TranslationException("Empty Z.ai Coding Plan response.");
    }

    public override async Task<ModelsResponse> GetModels()
    {
        _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.Zai.ApiKey);

        // Coding Plan catalogue is small and documented; curated list is authoritative for the plan.
        var curated = CodingPlanModels
            .Select(id => new LabelValue
            {
                Label = id switch
                {
                    "glm-5.2" => "glm-5.2 • Coding Plan flagship",
                    "glm-5-turbo" => "glm-5-turbo • Coding Plan",
                    "glm-4.7" => "glm-4.7 • Coding Plan (lighter quota)",
                    _ => id
                },
                Value = id
            })
            .ToList();

        if (string.IsNullOrEmpty(_apiKey))
        {
            return new ModelsResponse
            {
                Options = curated,
                Message = "Coding Plan API key not set — showing Coding Plan model ids only."
            };
        }

        try
        {
            var settings = await _settings.GetSettings([SettingKeys.Translation.Zai.Endpoint]);
            var ep = NormalizeCodingEndpoint(settings.GetValueOrDefault(SettingKeys.Translation.Zai.Endpoint));
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{ep}/models");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    var remote = data.EnumerateArray()
                        .Select(m => m.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "")
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => new LabelValue { Label = id!, Value = id! })
                        .ToList();

                    // Prefer Coding Plan order: curated first, then any extras from /models.
                    if (remote.Count > 0)
                    {
                        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var merged = new List<LabelValue>();
                        foreach (var item in curated)
                        {
                            if (seen.Add(item.Value)) merged.Add(item);
                        }
                        foreach (var item in remote)
                        {
                            if (seen.Add(item.Value)) merged.Add(item);
                        }
                        return new ModelsResponse { Options = merged };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Z.ai Coding Plan /models failed; using curated Coding Plan list");
        }

        return new ModelsResponse { Options = curated };
    }
}
