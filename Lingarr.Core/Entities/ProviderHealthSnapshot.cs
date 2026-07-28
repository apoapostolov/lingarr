namespace Lingarr.Core.Entities;

public class ProviderHealthSnapshot : BaseEntity
{
    public required string Provider { get; set; }
    public required string State { get; set; }
    public required string Reason { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public DateTime? LastWarningAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long? MedianDurationMs { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    public int PolicyVersion { get; set; } = 1;
}
