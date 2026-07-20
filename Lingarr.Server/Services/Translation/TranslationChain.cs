using System.Text.Json;
using System.Text.Json.Serialization;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Translation;

/// <summary>
/// One step in the ordered translation fallback chain (primary = index 0).
/// </summary>
public sealed class TranslationChainEntry
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = SettingKeys.Translation.DefaultServiceType;

    /// <summary>Optional model id for multi-model AI providers. Null/empty for NMT scrapers.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonIgnore]
    public string ProviderNormalized => Provider.Trim().ToLowerInvariant();
}

public static class TranslationChain
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static List<TranslationChainEntry> Parse(string? raw, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [new TranslationChainEntry { Provider = SettingKeys.Translation.DefaultServiceType }];
        }

        var trimmed = raw.Trim();

        if (!trimmed.StartsWith('['))
        {
            return [new TranslationChainEntry { Provider = trimmed }];
        }

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return [new TranslationChainEntry { Provider = SettingKeys.Translation.DefaultServiceType }];
            }

            var list = new List<TranslationChainEntry>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(new TranslationChainEntry { Provider = s! });
                    }
                    continue;
                }

                if (el.ValueKind == JsonValueKind.Object)
                {
                    var provider = el.TryGetProperty("provider", out var p) ? p.GetString()
                        : el.TryGetProperty("service", out var s2) ? s2.GetString()
                        : el.TryGetProperty("name", out var n) ? n.GetString()
                        : null;
                    if (string.IsNullOrWhiteSpace(provider))
                    {
                        continue;
                    }
                    string? model = null;
                    if (el.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String)
                    {
                        model = m.GetString();
                    }
                    list.Add(new TranslationChainEntry
                    {
                        Provider = provider!,
                        Model = string.IsNullOrWhiteSpace(model) ? null : model!.Trim()
                    });
                }
            }

            if (list.Count > 0)
            {
                return list;
            }
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex,
                "service_type setting contained malformed JSON, falling back to '{Default}'.",
                SettingKeys.Translation.DefaultServiceType);
        }

        return [new TranslationChainEntry { Provider = SettingKeys.Translation.DefaultServiceType }];
    }

    public static string Normalize(string? raw, ILogger? logger = null) =>
        JsonSerializer.Serialize(Parse(raw, logger), JsonOptions);

    public static string Serialize(IReadOnlyList<TranslationChainEntry> entries) =>
        JsonSerializer.Serialize(entries, JsonOptions);

    public static bool SupportsModel(string provider) =>
        provider.Trim().ToLowerInvariant() is
            "openai" or "anthropic" or "gemini" or "deepseek" or "localai"
            or "openrouter" or "zai" or "opencode-go";
}
