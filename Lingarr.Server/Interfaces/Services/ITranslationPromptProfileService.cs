using Lingarr.Server.Models.PromptProfiles;
using Lingarr.Server.Services.Translation;

namespace Lingarr.Server.Interfaces.Services;

public interface ITranslationPromptProfileService
{
    Task<IReadOnlyList<PromptProfileResponse>> GetAllAsync(
        string? type = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
    Task<PromptProfileResponse?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<PromptProfileResponse> CreateAsync(
        CreatePromptProfileRequest request,
        CancellationToken cancellationToken = default);
    Task<PromptProfileResponse> SaveDraftAsync(
        int id,
        SavePromptProfileDraftRequest request,
        CancellationToken cancellationToken = default);
    Task<PromptProfileResponse> PublishAsync(
        int id,
        string? changeNote,
        CancellationToken cancellationToken = default);
    Task<PromptProfileResponse> RestoreAsync(
        int id,
        int versionId,
        CancellationToken cancellationToken = default);
    Task<PromptProfileDeleteResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
    Task ActivateAsync(int id, CancellationToken cancellationToken = default);
    Task ResolveChainAsync(
        IReadOnlyList<TranslationChainEntry> entries,
        int? translationRequestId = null,
        CancellationToken cancellationToken = default);
}
