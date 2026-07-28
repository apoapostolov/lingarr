namespace Lingarr.Server.Models.Dashboard;

public record DashboardActivityBucket(
    DateTime Hour,
    int CompletedFiles,
    int TranslatedLines);

public record DashboardNamedCount(string Name, int Count);

public record DashboardActivityResponse(
    int WindowHours,
    DateTime WindowStartedAt,
    DateTime GeneratedAt,
    int CompletedFiles,
    int TranslatedLines,
    int ActiveTranslations,
    int FailedTranslations,
    int QualityChecked,
    int QualityPassed,
    int QualityNeedsReview,
    double? AverageQualityScore,
    int FallbackRecoveries,
    int UnavailableProviders,
    IReadOnlyList<DashboardNamedCount> TopProviders,
    IReadOnlyList<DashboardNamedCount> TopLanguagePairs,
    IReadOnlyList<DashboardActivityBucket> Buckets,
    string Headline,
    IReadOnlyList<string> Narrative);
