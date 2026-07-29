using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

/// <summary>
/// Shared implementation for first-party providers that expose OpenAI-compatible
/// chat-completions and model-list endpoints.
/// </summary>
public abstract class OpenAiCompatibleProviderService : BaseMeteredLanguageService
{
    private readonly HttpClient _httpClient;
    private readonly IRequestTemplateService _requestTemplateService;
    private readonly string _displayName;
    private readonly string _modelSettingKey;
    private readonly string _endpointSettingKey;
    private readonly string _requestTemplateSettingKey;
    private readonly string _defaultEndpoint;
    private readonly string _defaultModel;
    private readonly IReadOnlyList<string> _curatedModels;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private string _endpoint = "";
    private string _model = "";
    private string _prompt = "";
    private string _requestTemplate = "";
    private string _credential = "";
    private bool _initialized;

    protected OpenAiCompatibleProviderService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService,
        string provider,
        string displayName,
        string modelSettingKey,
        string endpointSettingKey,
        string requestTemplateSettingKey,
        string defaultEndpoint,
        string defaultModel,
        IReadOnlyList<string> curatedModels)
        : base(settings, logger, languageCodeService, provider)
    {
        Provider = provider;
        _httpClient = httpClient;
        _requestTemplateService = requestTemplateService;
        _displayName = displayName;
        _modelSettingKey = modelSettingKey;
        _endpointSettingKey = endpointSettingKey;
        _requestTemplateSettingKey = requestTemplateSettingKey;
        _defaultEndpoint = defaultEndpoint.TrimEnd('/');
        _defaultModel = defaultModel;
        _curatedModels = curatedModels;
    }

    protected string Provider { get; }
    public override string? ModelName => _model;

    protected abstract Task<string?> GetCredentialAsync();

    protected virtual bool IncludeRemoteModel(string id) => true;

    private async Task InitializeAsync(string sourceLanguage, string targetLanguage)
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                _modelSettingKey,
                _endpointSettingKey,
                _requestTemplateSettingKey,
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.LanguageCodeFormat,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.RequestTimeoutForProvider(Provider)
            ]);

            _model = ResolveModel(settings.GetValueOrDefault(_modelSettingKey));
            if (string.IsNullOrWhiteSpace(_model)) _model = _defaultModel;
            _endpoint = NormalizeEndpoint(settings.GetValueOrDefault(_endpointSettingKey), _defaultEndpoint);
            _credential = (await GetCredentialAsync())?.Trim() ?? "";
            _requestTemplate = settings.GetValueOrDefault(_requestTemplateSettingKey) ?? "";
            if (string.IsNullOrWhiteSpace(_requestTemplate))
            {
                _requestTemplate =
                    _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.OpenAi.RequestTemplate)
                    ?? throw new InvalidOperationException("OpenAI-compatible request template is unavailable.");
            }

            if (string.IsNullOrWhiteSpace(_credential))
            {
                throw new InvalidOperationException($"{_displayName} credentials are not configured.");
            }

            SetLanguageReplacements(
                sourceLanguage,
                targetLanguage,
                settings.GetValueOrDefault(SettingKeys.Translation.LanguageCodeFormat) ?? "false");
            _prompt = ReplacePlaceholders(
                ResolveSystemPrompt(settings.GetValueOrDefault(SettingKeys.Translation.AiPrompt)),
                _replacements);
            _contextPrompt = ResolveContextPrompt(
                settings.GetValueOrDefault(SettingKeys.Translation.AiContextPrompt));
            _contextPromptEnabled = settings.GetValueOrDefault(
                SettingKeys.Translation.AiContextPromptEnabled);
            _httpClient.Timeout = TimeSpan.FromMinutes(
                TranslationTimeoutPolicy.ResolveMinutes(settings, Provider));
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);
        var message = ApplyContextIfEnabled(text, contextLinesBefore, contextLinesAfter);
        var body = _requestTemplateService.BuildRequestBody(
            _requestTemplate,
            new Dictionary<string, string>
            {
                ["model"] = _model,
                ["systemPrompt"] = _prompt,
                ["userMessage"] = message,
                ["sourceLanguage"] = _replacements["sourceLanguage"],
                ["targetLanguage"] = _replacements["targetLanguage"]
            });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_endpoint}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credential);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"{_displayName} request failed ({response.StatusCode}): {responseBody}",
                null,
                response.StatusCode);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
            cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new TranslationException($"{_displayName} returned an empty translation.");
        }

        if (completion?.Usage is not null)
        {
            RecordUsage(
                completion.Model ?? _model,
                completion.Usage.PromptTokens,
                completion.Usage.CompletionTokens);
        }

        return content.Trim();
    }

    public override async Task<ModelsResponse> GetModels()
    {
        var credential = (await GetCredentialAsync())?.Trim();
        var curated = _curatedModels
            .Select(id => new LabelValue { Label = id, Value = id })
            .ToList();
        if (string.IsNullOrWhiteSpace(credential))
        {
            return new ModelsResponse
            {
                Options = curated,
                Message = $"{_displayName} credentials are not configured; showing recommended model IDs."
            };
        }

        var settings = await _settings.GetSettings([_endpointSettingKey]);
        var endpoint = NormalizeEndpoint(
            settings.GetValueOrDefault(_endpointSettingKey),
            _defaultEndpoint);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new ModelsResponse
                {
                    Options = curated,
                    Message = $"Model refresh failed ({response.StatusCode}); showing recommended models."
                };
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return new ModelsResponse { Options = curated };
            }

            var remote = data.EnumerateArray()
                .Select(item => item.TryGetProperty("id", out var id) ? id.GetString() : null)
                .Where(id => !string.IsNullOrWhiteSpace(id) && IncludeRemoteModel(id!))
                .Select(id => id!)
                .ToList();
            var merged = _curatedModels
                .Concat(remote)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => new LabelValue { Label = id, Value = id })
                .ToList();
            return new ModelsResponse { Options = merged };
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "{Provider} model refresh failed.", _displayName);
            return new ModelsResponse
            {
                Options = curated,
                Message = "Model refresh failed; showing recommended models."
            };
        }
    }

    internal static string NormalizeEndpoint(string? configured, string fallback) =>
        (string.IsNullOrWhiteSpace(configured) ? fallback : configured).Trim().TrimEnd('/');
}
