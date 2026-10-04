namespace Lingarr.Server.Models.Webhooks;

public class PlexAddedMovie
{
    public string Kind { get; set; } = "movie";

    public string Title { get; set; } = string.Empty;

    public string? ShowTitle { get; set; }

    public int? Year { get; set; }

    public int? SeasonNumber { get; set; }

    public int? EpisodeNumber { get; set; }

    public string? RatingKey { get; set; }

    public string? ShowRatingKey { get; set; }

    public List<string> Guids { get; set; } = new();
}
