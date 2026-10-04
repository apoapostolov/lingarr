namespace Lingarr.Server.Services;

public static class LibraryCoverage
{
    public static string Format(IEnumerable<string>? codes)
    {
        var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (codes != null)
        {
            foreach (var code in codes)
            {
                var two = TwoLetter(code);
                if (two != null)
                {
                    set.Add(two);
                }
            }
        }

        return string.Join(',', set);
    }

    public static bool HasFullPair(
        string? coverage,
        IEnumerable<string>? extraLanguages,
        IReadOnlySet<string> sources,
        IReadOnlySet<string> targets)
    {
        if (sources.Count == 0 || targets.Count == 0)
        {
            return false;
        }

        var have = Parse(coverage);
        if (extraLanguages != null)
        {
            foreach (var code in extraLanguages)
            {
                var two = TwoLetter(code);
                if (two != null)
                {
                    have.Add(two);
                }
            }
        }

        return sources.Any(have.Contains) && targets.All(have.Contains);
    }

    public static bool CheckedWithoutSource(string? coverage, IReadOnlySet<string> sources)
    {
        if (coverage == null || sources.Count == 0)
        {
            return false;
        }

        var have = Parse(coverage);
        return !sources.Any(have.Contains);
    }

    private static HashSet<string> Parse(string? coverage)
    {
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(coverage))
        {
            return have;
        }

        foreach (var code in coverage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var two = TwoLetter(code);
            if (two != null)
            {
                have.Add(two);
            }
        }

        return have;
    }

    private static string? TwoLetter(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        try
        {
            return Services.Integration.Plex.PlexLanguage.TwoLetter(code);
        }
        catch (ArgumentException)
        {
            return code.Trim().Length <= 3 ? code.Trim().ToLowerInvariant() : null;
        }
    }
}
