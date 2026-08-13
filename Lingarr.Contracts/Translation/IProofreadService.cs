namespace Lingarr.Contracts.Translation;

/// <summary>
/// Optional capability for AI providers that can reread a source line and its
/// translation, then return a corrected line or the original unchanged.
/// </summary>
public interface IProofreadService
{
    Task<string> ProofreadAsync(
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken);
}
