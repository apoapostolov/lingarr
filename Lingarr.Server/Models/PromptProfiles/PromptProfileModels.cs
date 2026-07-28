namespace Lingarr.Server.Models.PromptProfiles;

public static class PromptProfileTypes
{
    public const string System = "system";
    public const string Context = "context";
    public static readonly string[] All = [System, Context];
}

public record PromptProfileVersionResponse(
    int Id,
    int VersionNumber,
    string Content,
    string ChangeNote,
    string ContentHash,
    DateTime CreatedAt);

public record PromptProfileResponse(
    int Id,
    string Type,
    string Name,
    string Description,
    string DraftContent,
    int? CurrentPublishedVersionId,
    int? CurrentVersionNumber,
    bool HasUnpublishedChanges,
    bool IsArchived,
    int AssignmentCount,
    DateTime UpdatedAt,
    IReadOnlyList<PromptProfileVersionResponse> Versions);

public sealed record CreatePromptProfileRequest(
    string Type,
    string Name,
    string? Description,
    string? Content);

public sealed record SavePromptProfileDraftRequest(
    string Name,
    string? Description,
    string Content);

public sealed record PublishPromptProfileRequest(
    string? ChangeNote);

public sealed record PromptProfileDeleteResponse(bool Archived);
