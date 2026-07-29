using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

/// <summary>
/// Qwen-MT's purpose-built translation API. Unlike general Qwen chat models,
/// Qwen-MT accepts one user message and structured translation options.
/// </summary>
public sealed class QwenMtService : BaseMeteredLanguageService
{
    private static readonly string[] Models = ["qwen-mt-flash", "qwen-mt-plus"];
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private string _endpoint = QwenService.DefaultEndpoint;
    private string _model = Models[0];
    private string _apiKey = "";
    private bool _initialized;

    public QwenMtService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<QwenMtService> logger,
        LanguageCodeService languageCodeService)
        : base(settings, logger, languageCodeService, "qwen-mt")
    {
        _httpClient = httpClient;
    }

    public override string? ModelName => _model;

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            var settings = await _settings.GetSettings([
                SettingKeys.Translation.QwenMt.Model,
                SettingKeys.Translation.QwenMt.Endpoint,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.RequestTimeoutForProvider("qwen-mt")
            ]);
            _model = ResolveModel(settings.GetValueOrDefault(SettingKeys.Translation.QwenMt.Model));
            if (string.IsNullOrWhiteSpace(_model)) _model = Models[0];
            _endpoint = OpenAiCompatibleProviderService.NormalizeEndpoint(
                settings.GetValueOrDefault(SettingKeys.Translation.QwenMt.Endpoint),
                QwenService.DefaultEndpoint);
            _apiKey = (await _settings.GetEncryptedSetting(
                SettingKeys.Translation.QwenMt.ApiKey))?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Qwen API key is not configured.");
            }

            _httpClient.Timeout = TimeSpan.FromMinutes(
                TranslationTimeoutPolicy.ResolveMinutes(settings, "qwen-mt"));
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
        await InitializeAsync();
        var requestBody = JsonSerializer.Serialize(new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = text }
            },
            translation_options = new
            {
                source_lang = NormalizeLanguageCode(sourceLanguage),
                target_lang = NormalizeLanguageCode(targetLanguage)
            }
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_endpoint}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Qwen Translation request failed ({response.StatusCode}): {responseBody}",
                null,
                response.StatusCode);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
            cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new TranslationException("Qwen Translation returned an empty translation.");
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

    public override Task<ModelsResponse> GetModels() =>
        Task.FromResult(new ModelsResponse
        {
            Options = Models.Select(model => new LabelValue
            {
                Value = model,
                Label = model switch
                {
                    "qwen-mt-flash" => "qwen-mt-flash • Recommended",
                    "qwen-mt-plus" => "qwen-mt-plus • Highest quality",
                    _ => model
                }
            }).ToList()
        });

    internal static string NormalizeLanguageCode(string code)
    {
        var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();
        return normalized switch
        {
            "zh-tw" or "zh-hant" => "zh_tw",
            "zh-hk" => "yue",
            "nb-no" => "nb",
            "nn-no" => "nn",
            _ => normalized.Split('-', StringSplitOptions.RemoveEmptyEntries)[0]
        };
    }
}
