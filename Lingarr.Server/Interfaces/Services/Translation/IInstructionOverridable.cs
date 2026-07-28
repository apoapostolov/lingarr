namespace Lingarr.Server.Interfaces.Services.Translation;

public interface IInstructionOverridable
{
    void OverrideInstructions(string? systemPrompt, string? contextPrompt);
}
