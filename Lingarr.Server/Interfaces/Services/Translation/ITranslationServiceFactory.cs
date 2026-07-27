using Lingarr.Contracts.Translation;
using Lingarr.Server.Services.Translation;

namespace Lingarr.Server.Interfaces.Services.Translation;

public readonly record struct TranslationServiceEntry(
    string Name,
    ITranslationService Service,
    IBatchTranslationService? BatchService,
    string? Model = null);

public interface ITranslationServiceFactory
{
    ITranslationService CreateTranslationService(string serviceType);

    IReadOnlyList<TranslationServiceEntry> CreateTranslationServices(IReadOnlyList<TranslationChainEntry> entries);

    IReadOnlyList<TranslationServiceEntry> CreateTranslationServices(IReadOnlyList<string> serviceTypes);
}
