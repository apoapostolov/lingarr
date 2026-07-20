using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

/// <summary>Z.ai (GLM) OpenAI-compatible chat completions.</summary>
public class ZaiService : BaseLanguageService
{
    private string _endpoint = "https://api.z.ai/api/paas/v4";
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
                SettingKeys.Translation.LanguageCodeFormat
            ]);
            _model = ResolveModel(settings[SettingKeys.Translation.Zai.Model]);
            var ep = settings[SettingKeys.Translation.Zai.Endpoint];
            if (!string.IsNullOrWhiteSpace(ep)) _endpoint = ep.Trim().TrimEnd('/');
            _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.Zai.ApiKey);
            _requestTemplate = !string.IsNullOrEmpty(settings[SettingKeys.Translation.Zai.RequestTemplate])
                ? settings[SettingKeys.Translation.Zai.RequestTemplate]
                : _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.OpenAi.RequestTemplate);
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];
            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Z.ai API key or model is not configured.");
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
            throw new TranslationException($"Z.ai API error ({response.StatusCode}): {responseBody}");
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim()
               ?? throw new TranslationException("Empty Z.ai response.");
    }

    public override async Task<ModelsResponse> GetModels()
    {
        _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.Zai.ApiKey);
        if (string.IsNullOrEmpty(_apiKey))
            return new ModelsResponse { Message = "Z.ai API key is not configured." };
        // Curated GLM defaults + try /models
        var curated = new[] { "glm-5.2", "glm-5.1", "glm-5", "glm-4.7", "glm-4.6", "glm-4.5" }
            .Select(id => new LabelValue { Label = id, Value = id }).ToList();
        try
        {
            var settings = await _settings.GetSettings([SettingKeys.Translation.Zai.Endpoint]);
            var ep = settings.GetValueOrDefault(SettingKeys.Translation.Zai.Endpoint);
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
                        .Select(m => m.GetProperty("id").GetString() ?? "")
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => new LabelValue { Label = id, Value = id })
                        .ToList();
                    if (remote.Count > 0) return new ModelsResponse { Options = remote };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Z.ai /models failed; using curated list");
        }
        return new ModelsResponse { Options = curated };
    }
}
