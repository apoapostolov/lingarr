using Lingarr.Server.Services.Translation;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class LlmPricingCatalogTests
{
    [Theory]
    [InlineData("openai", "gpt-3.5-turbo", 1_000_000, 1_000_000, 2.00)]
    [InlineData("openai", "gpt-4o-mini", 1_000_000, 1_000_000, 0.75)]
    [InlineData("deepseek", "deepseek-v4-pro", 1_000_000, 1_000_000, 1.305)]
    [InlineData("anthropic", "claude-sonnet-4-6", 1_000_000, 1_000_000, 18.00)]
    [InlineData("openrouter", "openrouter/free", 1_000_000, 1_000_000, 0.00)]
    public void Estimate_KnownModel_UsesPublishedPerMillionRates(
        string provider,
        string model,
        long inputTokens,
        long outputTokens,
        double expected)
    {
        var result = LlmPricingCatalog.Estimate(
            provider,
            model,
            inputTokens,
            outputTokens);

        Assert.Equal((decimal)expected, result);
    }

    [Fact]
    public void Estimate_UnknownModel_DoesNotInventPrice()
    {
        Assert.Null(LlmPricingCatalog.Estimate(
            "openai",
            "future-model-with-unknown-price",
            100,
            100));
    }
}
