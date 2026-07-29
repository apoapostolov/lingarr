using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class XaiService : OpenAiCompatibleProviderService
{
    public const string DefaultEndpoint = "https://api.x.ai/v1";

    public XaiService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<XaiService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(
            settings,
            httpClient,
            logger,
            languageCodeService,
            requestTemplateService,
            "xai",
            "xAI API",
            SettingKeys.Translation.Xai.Model,
            SettingKeys.Translation.Xai.Endpoint,
            SettingKeys.Translation.Xai.RequestTemplate,
            DefaultEndpoint,
            "grok-4.5",
            ["grok-4.5", "grok-4", "grok-3-mini"])
    {
    }

    protected override Task<string?> GetCredentialAsync() =>
        _settings.GetEncryptedSetting(SettingKeys.Translation.Xai.ApiKey);

    protected override bool IncludeRemoteModel(string id) =>
        id.StartsWith("grok", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("image", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("video", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("voice", StringComparison.OrdinalIgnoreCase);
}
