namespace HemSoft.AI;

using System;
using System.Collections.Generic;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Factory for creating customized ChatClient instances
/// </summary>
public static class ChatClientFactory
{
    /// <summary>
    /// Creates a new ChatClient with the specified system prompt
    /// </summary>
    /// <param name="configuration">The configuration containing Azure OpenAI settings</param>
    /// <param name="systemPrompt">The system prompt to initialize the chat</param>
    /// <returns>A new ChatClient instance</returns>
    public static ChatClient CreateWithSystemPrompt(IConfiguration configuration, string systemPrompt)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(systemPrompt);

        return new ChatClient(configuration, systemPrompt);
    }

    /// <summary>
    /// Creates a new ChatClient with the specified system prompt and tools
    /// </summary>
    /// <param name="configuration">The configuration containing Azure OpenAI settings</param>
    /// <param name="systemPrompt">The system prompt to initialize the chat</param>
    /// <param name="tools">The tools to make available to the chat</param>
    /// <returns>A new ChatClient instance with the specified tools</returns>
    public static ChatClient CreateWithTools(IConfiguration configuration, string systemPrompt, IEnumerable<ChatCompletionsFunctionToolDefinition> tools)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(systemPrompt);
        ArgumentNullException.ThrowIfNull(tools);

        var chatClient = new ChatClient(configuration, systemPrompt);
        return chatClient;
    }

    /// <summary>
    /// Creates a new ChatClient for content parsing
    /// </summary>
    /// <param name="configuration">The configuration containing Azure OpenAI settings</param>
    /// <returns>A new ChatClient instance configured for content parsing</returns>
    public static ChatClient CreateForContentParsing(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        const string contentParsingPrompt = @"
You are an expert content parser. Your task is to extract structured information from unstructured text.
Follow these guidelines:
1. Extract only the information explicitly present in the content
2. Maintain the original formatting of extracted data when relevant
3. Return the data in the requested format
4. If you cannot find specific information, indicate it as missing rather than making assumptions
";

        return new ChatClient(configuration, contentParsingPrompt);
    }

    /// <summary>
    /// Creates a new ChatClient for NuGet package parsing
    /// </summary>
    /// <param name="configuration">The configuration containing Azure OpenAI settings</param>
    /// <returns>A new ChatClient instance configured for NuGet package parsing</returns>
    public static ChatClient CreateForNuGetParsing(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        const string nugetParsingPrompt = @"
You are an expert at parsing NuGet package information from web content. Your task is to extract structured information about NuGet packages.
For each package, extract:
1. Package name/title
2. Package URL
3. Description
4. Version information (if available)
5. Release date (if available)
6. Author information (if available)

Return the information in a structured format that can be easily converted to a collection of NewsItem objects.

Do NOT make assumptions about the content. If information is not explicitly present, indicate it as missing.
Do NOT add any leading or trailing text. Return only the structured data in JSON format.
Do NOT use markdown or any special newline characters or anything like that. The return string has to be a valid json array.

The return string has to be a JSON array starting with '[' and ending with ']'
";

        return new ChatClient(configuration, nugetParsingPrompt);
    }
}
