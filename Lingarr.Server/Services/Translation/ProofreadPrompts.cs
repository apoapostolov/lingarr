namespace Lingarr.Server.Services.Translation;

public static class ProofreadPrompts
{
    public const string DefaultSystem =
        "You are revising a subtitle translation. Compare the source line to the current " +
        "translation. Fix only clear errors: wrong meaning, missing content, broken names, " +
        "or garbled output. Keep the line short enough for a subtitle. If the translation " +
        "is already good, return it unchanged. Return only the revised subtitle line.";

    public const string DefaultUser =
        "Source ({sourceLanguage}): {sourceText}\nTranslation ({targetLanguage}): {translatedText}";

    public static string FormatUser(
        string? template,
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage)
    {
        var user = string.IsNullOrWhiteSpace(template) ? DefaultUser : template;
        return user
            .Replace("{sourceLanguage}", sourceLanguage)
            .Replace("{targetLanguage}", targetLanguage)
            .Replace("{sourceText}", sourceText)
            .Replace("{translatedText}", translatedText);
    }
}
