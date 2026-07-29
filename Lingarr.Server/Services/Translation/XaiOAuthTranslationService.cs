using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public sealed class XaiOAuthTranslationService : OpenAiCompatibleProviderService
{
    private readonly IXaiOAuthSessionService _sessions;

    public XaiOAuthTranslationService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<XaiOAuthTranslationService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService,
        IXaiOAuthSessionService sessions)
        : base(
            settings,
            httpClient,
            logger,
            languageCodeService,
            requestTemplateService,
            "xai-oauth",
            "xAI SuperGrok / Premium+",
            SettingKeys.Translation.XaiOAuth.Model,
            SettingKeys.Translation.XaiOAuth.Endpoint,
            SettingKeys.Translation.Xai.RequestTemplate,
            XaiService.DefaultEndpoint,
            "grok-4.5",
            ["grok-4.5", "grok-4", "grok-3-mini"])
    {
        _sessions = sessions;
    }

    protected override Task<string?> GetCredentialAsync() =>
        _sessions.GetValidAccessTokenAsync();

    protected override bool IncludeRemoteModel(string id) =>
        id.StartsWith("grok", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("image", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("video", StringComparison.OrdinalIgnoreCase)
        && !id.Contains("voice", StringComparison.OrdinalIgnoreCase);
}
