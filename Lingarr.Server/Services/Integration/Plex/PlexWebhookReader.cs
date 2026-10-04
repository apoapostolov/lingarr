using System.Text.Json;
using Lingarr.Server.Models.Webhooks;

namespace Lingarr.Server.Services.Integration.Plex;

public static class PlexWebhookReader
{
    public static PlexWebhookDecision Read(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return PlexWebhookDecision.Unreadable.Instance;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return PlexWebhookDecision.Unreadable.Instance;
            }

            if (!string.Equals(ReadString(root, "event"), "library.new", StringComparison.OrdinalIgnoreCase))
            {
                return PlexWebhookDecision.Ignored.Instance;
            }

            if (!root.TryGetProperty("Metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            {
                return PlexWebhookDecision.Ignored.Instance;
            }

            var type = ReadString(metadata, "type");
            if (!string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, "episode", StringComparison.OrdinalIgnoreCase))
            {
                return PlexWebhookDecision.Ignored.Instance;
            }

            return new PlexWebhookDecision.Added(ItemFrom(metadata));
        }
    }

    public static void FillMissing(PlexAddedMovie item, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var metadata = FirstMetadata(document.RootElement);
            if (metadata.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            item.ShowTitle ??= ReadString(metadata, "grandparentTitle");
            item.SeasonNumber ??= ReadInt(metadata, "parentIndex");
            item.EpisodeNumber ??= ReadInt(metadata, "index");
            item.ShowRatingKey ??= ReadId(metadata, "grandparentRatingKey");
            item.Year ??= ReadInt(metadata, "year");
            if (item.Guids.Count == 0)
            {
                item.Guids = CollectGuids(metadata).ToList();
            }
        }
        catch (JsonException)
        {
        }
    }

    public static IReadOnlyList<string> GuidsFromMetadata(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var metadata = FirstMetadata(root);
            return metadata.ValueKind == JsonValueKind.Object
                ? CollectGuids(metadata).ToList()
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static PlexAddedMovie ItemFrom(JsonElement metadata)
    {
        var type = ReadString(metadata, "type");
        var episode = string.Equals(type, "episode", StringComparison.OrdinalIgnoreCase);
        return new PlexAddedMovie
        {
            Kind = episode ? "episode" : "movie",
            Title = ReadString(metadata, "title") ?? string.Empty,
            ShowTitle = ReadString(metadata, "grandparentTitle"),
            Year = ReadInt(metadata, "year"),
            SeasonNumber = ReadInt(metadata, "parentIndex"),
            EpisodeNumber = ReadInt(metadata, "index"),
            RatingKey = ReadId(metadata, "ratingKey"),
            ShowRatingKey = ReadId(metadata, "grandparentRatingKey"),
            Guids = CollectGuids(metadata).ToList()
        };
    }

    private static JsonElement FirstMetadata(JsonElement root)
    {
        if (root.TryGetProperty("MediaContainer", out var container)
            && container.TryGetProperty("Metadata", out var items)
            && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                return item;
            }
        }

        if (root.TryGetProperty("Metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
        {
            return metadata;
        }

        return root;
    }

    private static IEnumerable<string> CollectGuids(JsonElement metadata)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var single = ReadString(metadata, "guid");
        if (!string.IsNullOrWhiteSpace(single) && seen.Add(single))
        {
            yield return single;
        }

        if (!metadata.TryGetProperty("Guid", out var guids) || guids.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var guid in guids.EnumerateArray())
        {
            var value = guid.ValueKind switch
            {
                JsonValueKind.String => guid.GetString(),
                JsonValueKind.Object => ReadString(guid, "id"),
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
            {
                yield return value;
            }
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static string? ReadId(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

public abstract class PlexWebhookDecision
{
    private PlexWebhookDecision()
    {
    }

    public sealed class Ignored : PlexWebhookDecision
    {
        public static readonly Ignored Instance = new();

        private Ignored()
        {
        }
    }

    public sealed class Unreadable : PlexWebhookDecision
    {
        public static readonly Unreadable Instance = new();

        private Unreadable()
        {
        }
    }

    public sealed class Added : PlexWebhookDecision
    {
        public Added(PlexAddedMovie item)
        {
            Item = item;
        }

        public PlexAddedMovie Item { get; }
    }
}
