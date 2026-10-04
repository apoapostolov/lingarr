using System.Globalization;
using Lingarr.Server.Services;

namespace Lingarr.Server.Services.Integration.Plex;

public static class PlexLanguage
{
    private static readonly Dictionary<string, string> BibliographicAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fre"] = "fr",
        ["ger"] = "de",
        ["chi"] = "zh",
        ["dut"] = "nl",
        ["rum"] = "ro",
        ["alb"] = "sq",
        ["arm"] = "hy",
        ["baq"] = "eu",
        ["bur"] = "my",
        ["cze"] = "cs",
        ["geo"] = "ka",
        ["gre"] = "el",
        ["ice"] = "is",
        ["mac"] = "mk",
        ["mao"] = "mi",
        ["may"] = "ms",
        ["per"] = "fa",
        ["scc"] = "sr",
        ["slo"] = "sk",
        ["tib"] = "bo",
        ["wel"] = "cy"
    };

    public static bool Same(string? left, string? right)
    {
        var a = Canonical(left);
        var b = Canonical(right);
        return a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static string TwoLetter(string code)
    {
        return Canonical(code)
            ?? throw new ArgumentException($"Invalid language code: '{code}'", nameof(code));
    }

    private static string? Canonical(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var trimmed = code.Trim();
        if (BibliographicAliases.TryGetValue(trimmed, out var alias))
        {
            trimmed = alias;
        }

        try
        {
            var normalized = LanguageCodeService.GetNormalizedCode(trimmed);
            var culture = CultureInfo.GetCultureInfo(normalized);
            var two = culture.TwoLetterISOLanguageName;
            return string.IsNullOrWhiteSpace(two) ? normalized : two;
        }
        catch (Exception exception) when (exception is ArgumentException or CultureNotFoundException)
        {
            return null;
        }
    }
}
