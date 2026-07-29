using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Services.Translation;

public interface IMeteredTranslationService
{
    LlmUsage? ConsumeUsage();
}
