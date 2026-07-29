namespace Lingarr.Server.Models.ProviderHealth;

public static class ProviderHealthStates
{
    public const string NotConfigured = "not_configured";
    public const string NotChecked = "not_checked";
    public const string Healthy = "healthy";
    public const string NeedsAttention = "needs_attention";
    public const string RecentlyUnavailable = "recently_unavailable";
    public const string Unavailable = "unavailable";
}

public sealed class ProviderHealthResponse
{
    public required string Provider { get; init; }
    public required string DisplayName { get; init; }
    public string? Model { get; init; }
    public required bool Configured { get; init; }
    public required IReadOnlyList<string> MissingFields { get; init; }
    public required string State { get; init; }
    public required string StatusLabel { get; init; }
    public required string Reason { get; init; }
    public DateTime? LastSuccessAt { get; init; }
    public DateTime? LastFailureAt { get; init; }
    public DateTime? LastWarningAt { get; init; }
    public int ConsecutiveFailures { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public double? SuccessRate { get; init; }
    public long? MedianDurationMs { get; init; }
    public required DateTime EvaluatedAt { get; init; }
}

public sealed class ProviderHealthEventResponse
{
    public int Id { get; init; }
    public required string Provider { get; init; }
    public string? Model { get; init; }
    public required string Operation { get; init; }
    public required string Outcome { get; init; }
    public string? ErrorFamily { get; init; }
    public bool IsTransient { get; init; }
    public long DurationMs { get; init; }
    public int RetryCount { get; init; }
    public int? TranslationRequestId { get; init; }
    public DateTime OccurredAt { get; init; }
}

public sealed class ProviderProbeResponse
{
    public required string Provider { get; init; }
    public string? Model { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public bool Supported { get; init; }
    public bool Success { get; init; }
    public long DurationMs { get; init; }
    public string? ErrorFamily { get; init; }
    public required string Message { get; init; }
}

public sealed class ProviderOperationalResult
{
    public required string Provider { get; init; }
    public string? Model { get; init; }
    public required string Operation { get; init; }
    public required string Outcome { get; init; }
    public string? ErrorFamily { get; init; }
    public bool IsTransient { get; init; }
    public long DurationMs { get; init; }
    public int RetryCount { get; init; }
    public int? TranslationRequestId { get; init; }
    public long? InputTokens { get; init; }
    public long? OutputTokens { get; init; }
    public decimal? EstimatedCostUsd { get; init; }
}
