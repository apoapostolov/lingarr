using System.Text.RegularExpressions;

namespace Lingarr.Server.Services.Integration.Plex;

public static partial class PlexMovieTags
{
    [GeneratedRegex(@"^(tmdb|imdb|tvdb)://([^/\s]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GuidPattern();

    public static IReadOnlyList<string> FromGuids(IEnumerable<string>? guids)
    {
        if (guids == null)
        {
            return [];
        }

        var tags = new List<string>();
        foreach (var guid in guids)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                continue;
            }

            var match = GuidPattern().Match(guid.Trim());
            if (!match.Success)
            {
                continue;
            }

            var tag = "{" + match.Groups[1].Value.ToLowerInvariant() + "-" + match.Groups[2].Value.Trim() + "}";
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    public static bool TryParse(string tag, out string kind, out string id)
    {
        kind = string.Empty;
        id = string.Empty;
        if (tag.Length < 5 || tag[0] != '{' || tag[^1] != '}')
        {
            return false;
        }

        var body = tag[1..^1];
        var dash = body.IndexOf('-');
        if (dash <= 0 || dash >= body.Length - 1)
        {
            return false;
        }

        kind = body[..dash].ToLowerInvariant();
        id = body[(dash + 1)..];
        return kind is "tmdb" or "imdb" or "tvdb" && id.Length > 0;
    }
}
