namespace Lingarr.Core.Entities;

public class TranslationPromptProfile : BaseEntity
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DraftContent { get; set; } = string.Empty;
    public int? CurrentPublishedVersionId { get; set; }
    public bool IsArchived { get; set; }
    public List<TranslationPromptProfileVersion> Versions { get; set; } = [];
}
