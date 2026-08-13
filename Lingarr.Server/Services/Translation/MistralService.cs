using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public sealed class MistralService : OpenAiCompatibleProviderService
{
    public const string DefaultEndpoint = "https://api.mistral.ai/v1";

    public MistralService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<MistralService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(
            settings,
            httpClient,
            logger,
            languageCodeService,
            requestTemplateService,
            "mistral",
            "Mistral AI",
            SettingKeys.Translation.Mistral.Model,
            SettingKeys.Translation.Mistral.Endpoint,
            SettingKeys.Translation.Mistral.RequestTemplate,
            DefaultEndpoint,
            "mistral-small-latest",
            ["mistral-small-latest", "mistral-medium-latest", "mistral-large-latest"])
    {
    }

    protected override Task<string?> GetCredentialAsync() =>
        _settings.GetEncryptedSetting(SettingKeys.Translation.Mistral.ApiKey);

    protected override bool IncludeRemoteModel(string id) =>
        !id.Contains("embed", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("moderation", StringComparison.OrdinalIgnoreCase);
}
