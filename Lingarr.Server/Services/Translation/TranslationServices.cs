namespace Lingarr.Server.Services.Translation;

public static class TranslationServices
{
    public static List<string> Parse(string? raw, ILogger? logger = null) =>
        TranslationChain.Parse(raw, logger).Select(e => e.ProviderNormalized).ToList();

    public static string Normalise(string? raw, ILogger? logger = null) =>
        TranslationChain.Normalize(raw, logger);
}
