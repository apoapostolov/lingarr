namespace Lingarr.Server.Models.TranslationQuality;

public static class QualityEvaluationStatus
{
    public const string Completed = "completed";
    public const string Unavailable = "unavailable";
    public const string Superseded = "superseded";
}

public static class QualitySeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Critical = "critical";
}

public record TranslationQualitySummary(
    int AssessmentId,
    int TranslationRequestId,
    int? Score,
    string Grade,
    double? AverageLineScore,
    double? LowTailScore,
    int CriticalCount,
    int ErrorCount,
    int WarningCount,
    int LineCount,
    DateTime EvaluatedAt,
    string EvaluationStatus);

public record TranslationQualityFindingResponse(
    int Id,
    int? TranslationRequestLineId,
    int? LinePosition,
    string RuleId,
    string Category,
    string Severity,
    int Penalty,
    string Summary,
    string MetadataJson);

public record TranslationQualityDetail(
    TranslationQualitySummary? Summary,
    IReadOnlyList<TranslationQualityFindingResponse> Findings);
