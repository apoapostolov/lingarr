using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class OpenRouterPluginManifest : IPluginManifest
{
    public string Provider => "openrouter";
    public string DisplayName => "OpenRouter";
    public string Description =>
        "100+ models via a single OpenAI-compatible API. AI translation can be costly — keep automation cautious.";
    public bool HasRequestTemplate => true;
    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.OpenRouter.ApiKey,
            Label = "API Key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "OpenRouter API key. Stored encrypted."
        },
        new()
        {
            Key = SettingKeys.Translation.OpenRouter.Model,
            Label = "AI Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/openrouter/models",
            Description = "Select a model from the OpenRouter catalogue."
        },
        new()
        {
            Key = SettingKeys.Translation.OpenRouter.Endpoint,
            Label = "API Endpoint",
            Type = PluginSettingType.Url,
            Required = false,
            Default = "https://openrouter.ai/api/v1/",
            Description = "Override only if using a proxy."
        }
    ];
}
