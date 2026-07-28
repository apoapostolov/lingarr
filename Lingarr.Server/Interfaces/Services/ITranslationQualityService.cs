using Lingarr.Server.Models.TranslationQuality;

namespace Lingarr.Server.Interfaces.Services;

public interface ITranslationQualityService
{
    Task<TranslationQualitySummary> EvaluateAsync(
        int translationRequestId,
        CancellationToken cancellationToken = default);

    Task<TranslationQualityDetail?> GetAsync(
        int translationRequestId,
        string? severity = null,
        string? category = null,
        CancellationToken cancellationToken = default);
}
