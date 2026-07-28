namespace Lingarr.Core.Entities;

public class TranslationLineQualityFinding : BaseEntity
{
    public int TranslationQualityAssessmentId { get; set; }
    public TranslationQualityAssessment TranslationQualityAssessment { get; set; } = null!;
    public int? TranslationRequestLineId { get; set; }
    public int? LinePosition { get; set; }
    public required string RuleId { get; set; }
    public required string Category { get; set; }
    public required string Severity { get; set; }
    public int Penalty { get; set; }
    public required string Summary { get; set; }
    public required string MetadataJson { get; set; }
}
