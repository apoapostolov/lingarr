using System.Collections.Generic;
using Lingarr.Core.Configuration;
using Xunit;

namespace Lingarr.Server.Tests.Configuration;

public class TranslationTimeoutPolicyTests
{
    [Fact]
    public void RequestTimeoutForProvider_NormalizesProviderId()
    {
        var key = SettingKeys.Translation.RequestTimeoutForProvider("OpenCode-Go");

        Assert.Equal("opencode_go_request_timeout", key);
    }

    [Fact]
    public void ResolveMinutes_UsesProviderSpecificValue()
    {
        var settings = new Dictionary<string, string>
        {
            [SettingKeys.Translation.RequestTimeout] = "5",
            [SettingKeys.Translation.RequestTimeoutForProvider("microsoft")] = "15"
        };

        var timeout = TranslationTimeoutPolicy.ResolveMinutes(settings, "microsoft");

        Assert.Equal(15, timeout);
    }

    [Fact]
    public void ResolveMinutes_FallsBackToLegacyGlobalValue()
    {
        var settings = new Dictionary<string, string>
        {
            [SettingKeys.Translation.RequestTimeout] = "7"
        };

        var timeout = TranslationTimeoutPolicy.ResolveMinutes(settings, "openai");

        Assert.Equal(7, timeout);
    }

    [Fact]
    public void ResolveMinutes_UsesLongerMicrosoftDefaultWhenSettingsAreInvalid()
    {
        var settings = new Dictionary<string, string>
        {
            [SettingKeys.Translation.RequestTimeout] = "0",
            [SettingKeys.Translation.RequestTimeoutForProvider("microsoft")] = "invalid"
        };

        var timeout = TranslationTimeoutPolicy.ResolveMinutes(settings, "microsoft");

        Assert.Equal(TranslationTimeoutPolicy.MicrosoftDefaultTimeoutMinutes, timeout);
    }
}
