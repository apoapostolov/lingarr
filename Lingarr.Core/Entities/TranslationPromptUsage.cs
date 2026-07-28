namespace Lingarr.Core.Entities;

public class TranslationPromptUsage : BaseEntity
{
    public int TranslationRequestId { get; set; }
    public required string ChainRowId { get; set; }
    public required string Provider { get; set; }
    public string? Model { get; set; }
    public int? SystemProfileId { get; set; }
    public int? SystemVersionId { get; set; }
    public string? SystemContentHash { get; set; }
    public int? ContextProfileId { get; set; }
    public int? ContextVersionId { get; set; }
    public string? ContextContentHash { get; set; }
}
