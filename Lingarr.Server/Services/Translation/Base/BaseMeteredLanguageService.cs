using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services.Translation.Base;

public abstract class BaseMeteredLanguageService : BaseLanguageService, IMeteredTranslationService
{
    private readonly object _usageLock = new();
    private readonly string _provider;
    private LlmUsage? _lastUsage;

    protected BaseMeteredLanguageService(
        ISettingService settings,
        ILogger logger,
        LanguageCodeService languageCodeService,
        string provider)
        : base(settings, logger, languageCodeService)
    {
        _provider = provider;
    }

    protected void RecordUsage(
        string? model,
        long inputTokens,
        long outputTokens,
        decimal? providerReportedCostUsd = null)
    {
        if (inputTokens < 0 || outputTokens < 0)
        {
            return;
        }

        var cost = providerReportedCostUsd ??
                   LlmPricingCatalog.Estimate(_provider, model, inputTokens, outputTokens);
        lock (_usageLock)
        {
            _lastUsage = new LlmUsage(inputTokens, outputTokens, cost);
        }
    }

    public LlmUsage? ConsumeUsage()
    {
        lock (_usageLock)
        {
            var usage = _lastUsage;
            _lastUsage = null;
            return usage;
        }
    }
}
