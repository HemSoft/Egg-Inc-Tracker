namespace HemSoft.AI;

using System.Collections.Generic;
using Azure.AI.OpenAI;

/// <summary>
/// Options for chat completions
/// </summary>
public class ChatClientOptions
{
    /// <summary>
    /// Gets or sets the temperature for the chat completion
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the tools available for the chat completion
    /// </summary>
    public List<ChatCompletionsFunctionToolDefinition> Tools { get; set; } = [];
}
