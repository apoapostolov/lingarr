using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Services.Translation;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class TranslationChainTests
{
    [Fact]
    public void Parse_Null_ReturnsDefaultService()
    {
        var chain = TranslationChain.Parse(null);
        Assert.Single(chain);
        Assert.Equal(SettingKeys.Translation.DefaultServiceType, chain[0].ProviderNormalized);
    }

    [Fact]
    public void Parse_PlainProviderString_ReturnsSingleEntry()
    {
        var chain = TranslationChain.Parse("microsoft");
        Assert.Single(chain);
        Assert.Equal("microsoft", chain[0].ProviderNormalized);
        Assert.Null(chain[0].Model);
    }

    [Fact]
    public void Parse_LegacyStringArray_PreservesOrder()
    {
        var chain = TranslationChain.Parse("""["microsoft","deepseek","openrouter"]""");
        Assert.Equal(3, chain.Count);
        Assert.Equal(new[] { "microsoft", "deepseek", "openrouter" },
            chain.Select(e => e.ProviderNormalized).ToArray());
    }

    [Fact]
    public void Parse_RichObjects_WithModels()
    {
        var raw = """
        [
          { "provider": "openrouter", "model": "openrouter/free" },
          { "provider": "deepseek", "model": "deepseek-v4-flash" },
          { "provider": "microsoft" }
        ]
        """;
        var chain = TranslationChain.Parse(raw);
        Assert.Equal(3, chain.Count);
        Assert.Equal("openrouter", chain[0].ProviderNormalized);
        Assert.Equal("openrouter/free", chain[0].Model);
        Assert.Equal("deepseek", chain[1].ProviderNormalized);
        Assert.Equal("deepseek-v4-flash", chain[1].Model);
        Assert.Equal("microsoft", chain[2].ProviderNormalized);
        Assert.Null(chain[2].Model);
    }

    [Fact]
    public void Parse_AllowsDuplicateProviders_DifferentModels()
    {
        var raw = """
        [
          { "provider": "openrouter", "model": "openrouter/free" },
          { "provider": "openrouter", "model": "moonshotai/kimi-k2.5" }
        ]
        """;
        var chain = TranslationChain.Parse(raw);
        Assert.Equal(2, chain.Count);
        Assert.Equal("openrouter", chain[0].ProviderNormalized);
        Assert.Equal("openrouter", chain[1].ProviderNormalized);
        Assert.True(chain[0].Model != chain[1].Model);
    }

    [Fact]
    public void Parse_MalformedJson_FallsBackToDefault()
    {
        var chain = TranslationChain.Parse("[not-json");
        Assert.Single(chain);
        Assert.Equal(SettingKeys.Translation.DefaultServiceType, chain[0].ProviderNormalized);
    }

    [Fact]
    public void Parse_EmptyArray_FallsBackToDefault()
    {
        var chain = TranslationChain.Parse("[]");
        Assert.Single(chain);
    }

    [Fact]
    public void Serialize_RoundTrips()
    {
        var entries = new List<TranslationChainEntry>
        {
            new() { Provider = "zai", Model = "glm-5.2" },
            new() { Provider = "microsoft" }
        };
        var json = TranslationChain.Serialize(entries);
        var again = TranslationChain.Parse(json);
        Assert.Equal(2, again.Count);
        Assert.Equal("zai", again[0].ProviderNormalized);
        Assert.Equal("glm-5.2", again[0].Model);
        Assert.Equal("microsoft", again[1].ProviderNormalized);
    }

    [Fact]
    public void Serialize_RoundTripsStableRowAndPromptAssignments()
    {
        var entries = new List<TranslationChainEntry>
        {
            new()
            {
                Id = "stable-row",
                Provider = "openrouter",
                Model = "openrouter/free",
                SystemPromptProfileId = 3,
                ContextPromptProfileId = 7
            }
        };

        var again = TranslationChain.Parse(TranslationChain.Serialize(entries));

        Assert.Equal("stable-row", again[0].Id);
        Assert.Equal(3, again[0].SystemPromptProfileId);
        Assert.Equal(7, again[0].ContextPromptProfileId);
    }

    [Fact]
    public void Normalize_AcceptsLegacyAndEmitsJson()
    {
        var normalized = TranslationChain.Normalize("microsoft");
        using var doc = JsonDocument.Parse(normalized);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    [Theory]
    [InlineData("openrouter", true)]
    [InlineData("OPENROUTER", true)]
    [InlineData("deepseek", true)]
    [InlineData("zai", true)]
    [InlineData("opencode-go", true)]
    [InlineData("microsoft", false)]
    [InlineData("google", false)]
    [InlineData("bing", false)]
    public void SupportsModel_MatchesExpectedProviders(string provider, bool expected)
    {
        Assert.Equal(expected, TranslationChain.SupportsModel(provider));
    }

    [Fact]
    public void TranslationServices_Parse_ReturnsProviderNamesOnly()
    {
        var names = TranslationServices.Parse("""[{"provider":"zai","model":"glm-5.2"},"microsoft"]""");
        Assert.Equal(new[] { "zai", "microsoft" }, names.ToArray());
    }
}
