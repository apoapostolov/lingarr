namespace Lingarr.Core.Entities;

public class TranslationPromptProfileVersion : BaseEntity
{
    public int ProfileId { get; set; }
    public TranslationPromptProfile Profile { get; set; } = null!;
    public int VersionNumber { get; set; }
    public required string Content { get; set; }
    public string ChangeNote { get; set; } = string.Empty;
    public required string ContentHash { get; set; }
}
