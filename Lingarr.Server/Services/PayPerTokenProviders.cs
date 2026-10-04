namespace Lingarr.Server.Services;

/// <summary>
/// API-key providers that bill per token. Flat subscriptions and OAuth plans
/// are not included, even when the API reports a token count.
/// </summary>
public static class PayPerTokenProviders
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai",
        "anthropic",
        "gemini",
        "deepseek",
        "openrouter",
        "mistral",
        "qwen",
        "qwen-mt",
        "xai"
    };

    public static bool Contains(string? provider) =>
        !string.IsNullOrWhiteSpace(provider) && Names.Contains(provider);
}
