using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.RequestTemplates;

/// <summary>
/// OpenRouter uses the OpenAI-compatible chat completions API format.
/// Placeholders:
///   {model}          - The selected AI model
///   {systemPrompt}   - System instruction prompt
///   {contextBefore}  - Prior dialogue lines for context (empty when disabled, newline-separated)
///   {userMessage}    - The subtitle line to translate
///   {temperature}    - Creativity/randomness control
///   {maxTokens}      - Maximum response length
/// </summary>
public class OpenRouterChatTemplate
{
    [JsonPropertyName("model")] 
    public string Model { get; set; } = "{model}";

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } =
    [
        new()
        {
            Role = "system", 
            Content = "{systemPrompt}"
        },
        new()
        {
            Role = "user", 
            Content = "{contextBefore}\n\n{userMessage}"
        }
    ];

    [JsonPropertyName("temperature")]
    public string Temperature { get; set; } = "{temperature}";

    [JsonPropertyName("max_tokens")]
    public string MaxTokens { get; set; } = "{maxTokens}";
}
