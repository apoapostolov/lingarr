using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public sealed class QwenService : OpenAiCompatibleProviderService
{
    public const string DefaultEndpoint = "https://dashscope-us.aliyuncs.com/compatible-mode/v1";

    public QwenService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<QwenService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(
            settings,
            httpClient,
            logger,
            languageCodeService,
            requestTemplateService,
            "qwen",
            "Qwen General AI",
            SettingKeys.Translation.Qwen.Model,
            SettingKeys.Translation.Qwen.Endpoint,
            SettingKeys.Translation.Qwen.RequestTemplate,
            DefaultEndpoint,
            "qwen3.7-plus",
            ["qwen3.7-plus", "qwen3.7-max", "qwen3.6-flash"])
    {
    }

    protected override Task<string?> GetCredentialAsync() =>
        _settings.GetEncryptedSetting(SettingKeys.Translation.Qwen.ApiKey);

    protected override bool IncludeRemoteModel(string id) =>
        id.StartsWith("qwen", StringComparison.OrdinalIgnoreCase)
        && !id.StartsWith("qwen-mt", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("image", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("audio", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("tts", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("asr", StringComparison.OrdinalIgnoreCase);
}
