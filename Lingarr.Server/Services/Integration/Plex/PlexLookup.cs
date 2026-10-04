using System.Text.RegularExpressions;

namespace Lingarr.Server.Services.Integration.Plex;

public sealed class PlexLookup
{
    private static readonly Regex IdPattern = new(
        @"\{(tmdb|imdb|tvdb)-([^}]+)\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex YearPattern = new(
        @"\((19|20)\d{2}\)",
        RegexOptions.Compiled);
    private static readonly Regex TagPattern = new(
        @"\{[^}]*\}|\[[^\]]*\]",
        RegexOptions.Compiled);

    public required string Title { get; init; }
    public required string FileStem { get; init; }
    public string? Year { get; init; }
    public IReadOnlyList<string> ExternalIds { get; init; } = [];
    public IReadOnlySet<string> Slugs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static PlexLookup Create(string title, string directory, string fileStem)
    {
        var ids = new List<string>();
        foreach (Match match in IdPattern.Matches(directory + "\n" + fileStem))
        {
            var kind = match.Groups[1].Value.ToLowerInvariant();
            var value = match.Groups[2].Value.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            ids.Add($"{kind}://{value}");
        }

        var year = YearIn(fileStem) ?? YearIn(directory) ?? YearIn(title);
        var names = new List<string> { title, Clean(fileStem) };
        var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            AddSlug(slugs, name);
            if (year != null)
            {
                AddSlug(slugs, name + " " + year);
            }
        }

        return new PlexLookup
        {
            Title = title,
            FileStem = fileStem,
            Year = year,
            ExternalIds = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Slugs = slugs
        };
    }

    public IReadOnlyList<string> SearchQueries()
    {
        var queries = new List<string>();
        Add(Title);
        Add(FileStem);
        Add(Clean(FileStem));
        if (Year != null)
        {
            Add(Title + " " + Year);
        }

        foreach (var id in ExternalIds)
        {
            Add(id);
            var slash = id.LastIndexOf('/');
            if (slash >= 0 && slash < id.Length - 1)
            {
                Add(id[(slash + 1)..]);
            }
        }

        return queries;

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && !queries.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                queries.Add(value);
            }
        }
    }

    public bool MatchesItem(string? title, string? originalTitle, string? slug, IEnumerable<string> guids)
    {
        if (SameName(title) || SameName(originalTitle))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(slug) && Slugs.Contains(slug))
        {
            return true;
        }

        foreach (var guid in guids)
        {
            if (ExternalIds.Contains(guid, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool SameName(string? plexName)
    {
        if (string.IsNullOrWhiteSpace(plexName))
        {
            return false;
        }

        var slug = ToSlug(plexName);
        return slug.Length >= 6 && Slugs.Contains(slug);
    }

    private static void AddSlug(ISet<string> slugs, string value)
    {
        var slug = ToSlug(value);
        if (slug.Length >= 6)
        {
            slugs.Add(slug);
        }
    }

    public static string ToSlug(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        var pendingHyphen = false;
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                pendingHyphen = false;
                builder.Append(character);
            }
            else
            {
                pendingHyphen = true;
            }
        }

        return builder.ToString();
    }

    private static string Clean(string fileStem)
    {
        var cleaned = TagPattern.Replace(fileStem, " ");
        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? YearIn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = YearPattern.Match(value);
        return match.Success ? match.Value.Trim('(', ')') : null;
    }
}
