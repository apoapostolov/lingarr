using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class XaiOAuthPluginManifest : IPluginManifest
{
    public string Provider => "xai-oauth";
    public string DisplayName => "xAI SuperGrok / Premium+";
    public string Description =>
        "Experimental consumer-subscription connection using xAI device login. " +
        "Availability and quota are controlled by xAI and may change.";
    public bool HasRequestTemplate => true;
    public bool SupportsInstructionProfiles => true;
    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.XaiOAuth.Connection,
            Label = "xAI Account",
            Type = PluginSettingType.OAuth,
            Required = true,
            Description = "Access and refresh tokens stay encrypted on the Lingarr server."
        },
        new()
        {
            Key = SettingKeys.Translation.XaiOAuth.Model,
            Label = "Grok Model",
            Type = PluginSettingType.RemoteDropdown,
            Required = true,
            OptionsEndpoint = "/api/plugin/xai-oauth/models",
            Description = "Models available to the connected xAI account."
        }
    ];
}
