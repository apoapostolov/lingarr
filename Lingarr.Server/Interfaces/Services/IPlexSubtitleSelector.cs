using Lingarr.Core.Entities;

namespace Lingarr.Server.Interfaces.Services;

public interface IPlexSubtitleSelector
{
    Task ApplyTranslatedSubtitleAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);
}
