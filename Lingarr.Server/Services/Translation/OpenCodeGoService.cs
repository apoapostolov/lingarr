using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

/// <summary>OpenCode Go curated OpenAI-compatible model gateway.</summary>
public class OpenCodeGoService : BaseLanguageService
{
    private string _endpoint = "https://opencode.ai/zen/go/v1";
    private readonly HttpClient _httpClient;
    private readonly IRequestTemplateService _requestTemplateService;
    private string? _model;
    private string? _prompt;
    private string? _apiKey;
    private string? _requestTemplate;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public override string? ModelName => _model;

    public OpenCodeGoService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<OpenCodeGoService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(settings, logger, languageCodeService)
    {
        _httpClient = httpClient;
        _requestTemplateService = requestTemplateService;
    }

    private async Task InitializeAsync(string sourceLanguage, string targetLanguage)
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            var settings = await _settings.GetSettings([
                SettingKeys.Translation.OpenCodeGo.Model,
                SettingKeys.Translation.OpenCodeGo.Endpoint,
                SettingKeys.Translation.OpenCodeGo.RequestTemplate,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.LanguageCodeFormat
            ]);
            _model = ResolveModel(settings[SettingKeys.Translation.OpenCodeGo.Model]);
            var ep = settings[SettingKeys.Translation.OpenCodeGo.Endpoint];
            if (!string.IsNullOrWhiteSpace(ep)) _endpoint = ep.Trim().TrimEnd('/');
            _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.OpenCodeGo.ApiKey);
            _requestTemplate = !string.IsNullOrEmpty(settings[SettingKeys.Translation.OpenCodeGo.RequestTemplate])
                ? settings[SettingKeys.Translation.OpenCodeGo.RequestTemplate]
                : _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.OpenAi.RequestTemplate);
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];
            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("OpenCode Go API key or model is not configured.");
            SetLanguageReplacements(sourceLanguage, targetLanguage, settings[SettingKeys.Translation.LanguageCodeFormat]);
            _prompt = ReplacePlaceholders(settings[SettingKeys.Translation.AiPrompt], _replacements);
            _contextPrompt = settings[SettingKeys.Translation.AiContextPrompt];
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
        var body = _requestTemplateService.BuildRequestBody(
            _requestTemplate ?? "",
            new Dictionary<string, string>
            {
                ["model"] = _model!,
                ["systemPrompt"] = _prompt ?? $"Translate to {targetLanguage}. Return only the translation.",
                ["userMessage"] = text
            });
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(req, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new TranslationException($"OpenCode Go API error ({response.StatusCode}): {responseBody}");
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim()
               ?? throw new TranslationException("Empty OpenCode Go response.");
    }

    public override async Task<ModelsResponse> GetModels()
    {
        _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.OpenCodeGo.ApiKey);
        var curated = new[]
        {
            "deepseek-v4-flash", "deepseek-v4-pro", "glm-5.1", "glm-5.2",
            "kimi-k2.6", "minimax-m2.7", "qwen3.6-plus", "mimo-v2.5"
        }.Select(id => new LabelValue { Label = id, Value = id }).ToList();
        if (string.IsNullOrEmpty(_apiKey))
            return new ModelsResponse { Options = curated, Message = "API key optional for curated list; required to translate." };
        try
        {
            var settings = await _settings.GetSettings([SettingKeys.Translation.OpenCodeGo.Endpoint]);
            var ep = settings.GetValueOrDefault(SettingKeys.Translation.OpenCodeGo.Endpoint);
            if (string.IsNullOrWhiteSpace(ep)) ep = _endpoint;
            ep = ep!.Trim().TrimEnd('/');
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
                        .Select(m => m.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "")
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => new LabelValue { Label = id!, Value = id! })
                        .ToList();
                    if (remote.Count > 0) return new ModelsResponse { Options = remote };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "OpenCode Go /models failed; using curated list");
        }
        return new ModelsResponse { Options = curated };
    }
}
