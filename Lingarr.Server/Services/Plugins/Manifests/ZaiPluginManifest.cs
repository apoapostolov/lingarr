using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class ZaiPluginManifest : IPluginManifest
{
    public string Provider => "zai";
    public string DisplayName => "Z.ai (GLM)";
    public string Description =>
        "Z.ai GLM models via OpenAI-compatible API. Use the Coding Plan endpoint when on a coding subscription.";
    public bool HasRequestTemplate => true;
    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.Zai.ApiKey,
            Label = "API Key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "Z.ai API key. Stored encrypted."
        },
        new()
        {
            Key = SettingKeys.Translation.Zai.Model,
            Label = "AI Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/zai/models",
            Description = "GLM model id (e.g. glm-5.2)."
        },
        new()
        {
            Key = SettingKeys.Translation.Zai.Endpoint,
            Label = "API Base URL",
            Type = PluginSettingType.Url,
            Required = false,
            Default = "https://api.z.ai/api/paas/v4",
            Description = "General: https://api.z.ai/api/paas/v4 — Coding Plan may use a different base URL from Z.ai docs."
        }
    ];
}
