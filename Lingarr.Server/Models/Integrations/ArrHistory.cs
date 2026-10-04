using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations;

public class ArrHistoryPage
{
    [JsonPropertyName("records")]
    public List<ArrHistoryRecord> Records { get; set; } = [];
}

public class ArrHistoryRecord
{
    [JsonPropertyName("eventType")]
    public string? EventType { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("movieId")]
    public int MovieId { get; set; }

    [JsonPropertyName("episodeId")]
    public int EpisodeId { get; set; }
}
