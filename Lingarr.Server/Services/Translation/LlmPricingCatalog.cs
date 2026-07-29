namespace Lingarr.Server.Services.Translation;

public static class LlmPricingCatalog
{
    public static decimal? Estimate(
        string provider,
        string? model,
        long inputTokens,
        long outputTokens)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var rates = ResolveRates(
            provider.Trim().ToLowerInvariant(),
            model.Trim().ToLowerInvariant());
        if (rates is null)
        {
            return null;
        }

        return inputTokens * rates.Value.InputPerMillion / 1_000_000m
               + outputTokens * rates.Value.OutputPerMillion / 1_000_000m;
    }

    private static (decimal InputPerMillion, decimal OutputPerMillion)? ResolveRates(
        string provider,
        string model) => provider switch
    {
        "openrouter" when model == "openrouter/free" => (0m, 0m),
        "openai" => OpenAiRates(model),
        "deepseek" => DeepSeekRates(model),
        "anthropic" => AnthropicRates(model),
        _ => null
    };

    private static (decimal, decimal)? OpenAiRates(string model)
    {
        if (model.Contains("gpt-5.6-sol") || model == "gpt-5.6") return (5m, 30m);
        if (model.Contains("gpt-5.6-terra")) return (2.5m, 15m);
        if (model.Contains("gpt-5.6-luna")) return (1m, 6m);
        if (model.Contains("gpt-5-nano")) return (0.05m, 0.4m);
        if (model.Contains("gpt-5-mini")) return (0.25m, 2m);
        if (model.StartsWith("gpt-5")) return (1.25m, 10m);
        if (model.Contains("gpt-4.1-nano")) return (0.1m, 0.4m);
        if (model.Contains("gpt-4.1-mini")) return (0.4m, 1.6m);
        if (model.StartsWith("gpt-4.1")) return (2m, 8m);
        if (model.Contains("gpt-4o-mini")) return (0.15m, 0.6m);
        if (model.StartsWith("gpt-4o") || model.StartsWith("chatgpt-4o")) return (2.5m, 10m);
        if (model.StartsWith("gpt-3.5-turbo")) return (0.5m, 1.5m);
        if (model == "gpt-4" || model.StartsWith("gpt-4-")) return (30m, 60m);
        return null;
    }

    private static (decimal, decimal)? DeepSeekRates(string model)
    {
        if (model.Contains("deepseek-v4-flash")) return (0.14m, 0.28m);
        if (model.Contains("deepseek-v4-pro")) return (0.435m, 0.87m);
        return null;
    }

    private static (decimal, decimal)? AnthropicRates(string model)
    {
        if (model.Contains("fable-5") || model.Contains("mythos-5")) return (10m, 50m);
        if (model.Contains("opus-5") ||
            model.Contains("opus-4-8") ||
            model.Contains("opus-4-7") ||
            model.Contains("opus-4-6") ||
            model.Contains("opus-4-5"))
            return (5m, 25m);
        if (model.Contains("opus")) return (15m, 75m);
        if (model.Contains("sonnet")) return (3m, 15m);
        if (model.Contains("haiku-4-5")) return (1m, 5m);
        if (model.Contains("haiku-3-5")) return (0.8m, 4m);
        if (model.Contains("haiku-3")) return (0.25m, 1.25m);
        return null;
    }
}
