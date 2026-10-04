using Lingarr.Server.Services;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class PayPerTokenProvidersTests
{
    [Theory]
    [InlineData("openai")]
    [InlineData("OpenAI")]
    [InlineData("anthropic")]
    [InlineData("gemini")]
    [InlineData("deepseek")]
    [InlineData("openrouter")]
    [InlineData("mistral")]
    [InlineData("qwen")]
    [InlineData("qwen-mt")]
    [InlineData("xai")]
    public void Contains_PayPerTokenApis(string provider)
    {
        Assert.True(PayPerTokenProviders.Contains(provider));
    }

    [Theory]
    [InlineData("xai-oauth")]
    [InlineData("zai")]
    [InlineData("opencode-go")]
    [InlineData("localai")]
    [InlineData("deepl")]
    [InlineData("microsoft")]
    [InlineData("google")]
    [InlineData("libretranslate")]
    [InlineData(null)]
    public void Contains_SkipsSubscriptionsAndCharacterPricedProviders(string? provider)
    {
        Assert.False(PayPerTokenProviders.Contains(provider));
    }
}
