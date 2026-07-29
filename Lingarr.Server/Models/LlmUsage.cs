namespace Lingarr.Server.Models;

public sealed record LlmUsage(
    long InputTokens,
    long OutputTokens,
    decimal? EstimatedCostUsd);
