using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class ZaiPluginManifest : IPluginManifest
{
    public string Provider => "zai";
    public string DisplayName => "Z.ai Coding Plan (GLM)";
    public string Description =>
        "GLM Coding Plan subscription (quota), not the general pay-as-you-go Z.ai API. " +
        "Must use the Coding OpenAI base URL or calls hit balance/1113 errors. " +
        "Official models: glm-5.2, glm-5-turbo, glm-4.7.";
    public bool HasRequestTemplate => true;
    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.Zai.ApiKey,
            Label = "Coding Plan API Key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "Key from Z.ai Coding Plan (Individual/Team Plan Overview). Stored encrypted."
        },
        new()
        {
            Key = SettingKeys.Translation.Zai.Model,
            Label = "AI Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/zai/models",
            Description = "Coding Plan models (glm-5.2 default, glm-5-turbo, glm-4.7)."
        },
        new()
        {
            Key = SettingKeys.Translation.Zai.Endpoint,
            Label = "Coding Plan API Base URL",
            Type = PluginSettingType.Url,
            Required = false,
            Default = "https://api.z.ai/api/coding/paas/v4",
            Description =
                "OpenAI Chat Completions Coding Plan: https://api.z.ai/api/coding/paas/v4 " +
                "(CN: https://open.bigmodel.cn/api/coding/paas/v4). " +
                "Do NOT use general https://api.z.ai/api/paas/v4."
        }
    ];
}
