using Lingarr.Server.Services.Translation;
using Xunit;

namespace Lingarr.Server.Tests.Services.Translation;

public class ProofreadPromptsTests
{
    [Fact]
    public void FormatUser_ShouldUseDefaultTemplate_WhenTemplateIsEmpty()
    {
        var formatted = ProofreadPrompts.FormatUser(
            null,
            "Hello",
            "Zdrasti",
            "en",
            "bg");

        Assert.Contains("Source (en): Hello", formatted);
        Assert.Contains("Translation (bg): Zdrasti", formatted);
    }

    [Fact]
    public void FormatUser_ShouldHonorCustomTemplate()
    {
        var formatted = ProofreadPrompts.FormatUser(
            "{sourceText} => {translatedText}",
            "Hello",
            "Zdrasti",
            "en",
            "bg");

        Assert.Equal("Hello => Zdrasti", formatted);
    }

    [Fact]
    public void Mistral_ShouldUseOfficialApiHost()
    {
        Assert.Equal("https://api.mistral.ai/v1", MistralService.DefaultEndpoint);
    }
}
