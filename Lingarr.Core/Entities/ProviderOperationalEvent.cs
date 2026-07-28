namespace Lingarr.Core.Entities;

public class ProviderOperationalEvent : BaseEntity
{
    public required string Provider { get; set; }
    public string? Model { get; set; }
    public required string Operation { get; set; }
    public required string Outcome { get; set; }
    public string? ErrorFamily { get; set; }
    public bool IsTransient { get; set; }
    public long DurationMs { get; set; }
    public int RetryCount { get; set; }
    public int? TranslationRequestId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int PolicyVersion { get; set; } = 1;
}
