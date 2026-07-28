namespace Lingarr.Core.Entities;

public class TranslationQualityAssessment : BaseEntity
{
    public int TranslationRequestId { get; set; }
    public TranslationRequest TranslationRequest { get; set; } = null!;
    public int RulesetVersion { get; set; } = 1;
    public int? Score { get; set; }
    public required string Grade { get; set; }
    public double? AverageLineScore { get; set; }
    public double? LowTailScore { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int LineCount { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    public required string EvaluationStatus { get; set; }
}
