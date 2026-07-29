using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;
using Lingarr.Server.Services.Translation;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class QwenPluginManifest : IPluginManifest
{
    public string Provider => "qwen";
    public string DisplayName => "Qwen General AI";
    public string Description =>
        "General Qwen chat models with Lingarr instruction profiles. " +
        "Use Qwen Translation for the purpose-built subtitle translation models.";
    public bool HasRequestTemplate => true;
    public bool SupportsInstructionProfiles => true;
    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.Qwen.ApiKey,
            Label = "Model Studio API Key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "Alibaba Cloud Model Studio key. Stored encrypted."
        },
        new()
        {
            Key = SettingKeys.Translation.Qwen.Model,
            Label = "AI Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/qwen/models",
            Description = "Recommended Qwen text models appear first, followed by your live catalogue."
        },
        new()
        {
            Key = SettingKeys.Translation.Qwen.Endpoint,
            Label = "OpenAI-Compatible API Base URL",
            Type = PluginSettingType.Url,
            Required = true,
            Default = QwenService.DefaultEndpoint,
            Description =
                "Keys and endpoints are region-specific. The default is US (Virginia); paste the API Host supplied with your Model Studio key for another region."
        }
    ];
}
