using System.Text.Json.Serialization;

namespace Lingarr.Server.Models.Integrations.Translation;

public class DeepSeekChatResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")] 
    public List<DeepSeekChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public DeepSeekUsage? Usage { get; set; }
}

public class DeepSeekChoice
{
    [JsonPropertyName("message")] 
    public DeepSeekMessage Message { get; set; } = new();
}

public class DeepSeekMessage
{
    [JsonPropertyName("content")] 
    public string Content { get; set; } = string.Empty;
}

public class DeepSeekUsage
{
    [JsonPropertyName("prompt_tokens")]
    public long PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public long CompletionTokens { get; set; }
}
