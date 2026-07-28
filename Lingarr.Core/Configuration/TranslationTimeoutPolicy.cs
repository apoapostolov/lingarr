namespace Lingarr.Core.Configuration;

/// <summary>
/// Resolves translation request timeouts without forcing every provider to share one value.
/// Provider-specific settings fall back to the legacy global setting for backward compatibility.
/// </summary>
public static class TranslationTimeoutPolicy
{
    public const int DefaultTimeoutMinutes = 5;
    public const int MicrosoftDefaultTimeoutMinutes = 15;

    public static int ResolveMinutes(
        IReadOnlyDictionary<string, string> settings,
        string provider)
    {
        var providerKey = SettingKeys.Translation.RequestTimeoutForProvider(provider);
        if (TryGetPositiveMinutes(settings, providerKey, out var providerMinutes))
        {
            return providerMinutes;
        }

        if (TryGetPositiveMinutes(settings, SettingKeys.Translation.RequestTimeout, out var globalMinutes))
        {
            return globalMinutes;
        }

        return provider.Equals("microsoft", StringComparison.OrdinalIgnoreCase)
            ? MicrosoftDefaultTimeoutMinutes
            : DefaultTimeoutMinutes;
    }

    private static bool TryGetPositiveMinutes(
        IReadOnlyDictionary<string, string> settings,
        string key,
        out int minutes)
    {
        minutes = 0;
        return settings.TryGetValue(key, out var value)
               && int.TryParse(value, out minutes)
               && minutes > 0;
    }
}
