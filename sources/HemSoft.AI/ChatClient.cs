namespace HemSoft.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

/// <summary>
/// A client for interacting with AI models
/// </summary>
public class ChatClient
{
    private readonly OpenAIClient? _openAiClient;
    private readonly string _deploymentName;
    private readonly List<ChatRequestMessage> _messages = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatClient"/> class.
    /// </summary>
    /// <param name="configuration">The configuration containing Azure OpenAI settings</param>
    /// <param name="systemPrompt">Optional system prompt to initialize the chat</param>
    public ChatClient(IConfiguration configuration, string? systemPrompt = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var apiUrl = configuration["AzureOpenAI:Endpoint"];

        // Get model name from configuration or use default
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";

        // Check if we have valid Azure OpenAI credentials
        if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiUrl))
        {
            // Create the Azure OpenAI client
            _openAiClient = new OpenAIClient(new Uri(apiUrl), new AzureKeyCredential(apiKey));
        }
        else
        {
            // Log that we're using a mock client
            Console.WriteLine("Azure OpenAI credentials not found. Using mock client for development/testing.");
            _openAiClient = null;
        }

        // Add system prompt if provided
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _messages.Add(new ChatRequestSystemMessage(systemPrompt));
        }
    }

    /// <summary>
    /// Adds a system message to the chat history
    /// </summary>
    /// <param name="message">The system message to add</param>
    public void AddSystemMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("System message cannot be null or empty", nameof(message));
        }

        _messages.Add(new ChatRequestSystemMessage(message));
    }

    /// <summary>
    /// Adds a user message to the chat history
    /// </summary>
    /// <param name="message">The user message to add</param>
    public void AddUserMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("User message cannot be null or empty", nameof(message));
        }

        _messages.Add(new ChatRequestUserMessage(message));
    }

    /// <summary>
    /// Gets a response from the AI model
    /// </summary>
    /// <param name="options">Optional chat options including temperature and tools</param>
    /// <returns>The AI response text</returns>
    public async Task<string> GetResponseAsync(ChatClientOptions? options = null)
    {
        try
        {
            // If we don't have a valid OpenAI client, return a mock response
            if (_openAiClient == null)
            {
                // Create a mock response based on the last user message
                var lastUserMessage = _messages.LastOrDefault(m => m is ChatRequestUserMessage) as ChatRequestUserMessage;
                string mockResponse = "This is a mock response for development/testing. Azure OpenAI credentials are not configured.";

                if (lastUserMessage != null)
                {
                    // Simple mock parsing logic for NuGet packages
                    if (lastUserMessage.Content.Contains("NuGet") || lastUserMessage.Content.Contains("package"))
                    {
                        mockResponse = "I've analyzed the content and found the following packages:\n\n" +
                                      "- Package1: A useful package for development\n" +
                                      "- Package2: Another great utility\n" +
                                      "- Package3: Framework extension";
                    }
                }

                // Add the mock response to the message history
                var assistantMessage = new ChatRequestAssistantMessage(mockResponse);
                _messages.Add(assistantMessage);

                return mockResponse;
            }

            // Create the chat completions options
            var completionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Temperature = options?.Temperature,
                MaxTokens = options?.MaxTokens
            };

            // Add the messages to the options
            foreach (var message in _messages)
            {
                completionsOptions.Messages.Add(message);
            }

            // Add the tools if provided
            if (options?.Tools != null && options.Tools.Count > 0)
            {
                foreach (var tool in options.Tools)
                {
                    completionsOptions.Tools.Add(tool);
                }
            }

            // Get the response from the AI model
            var response = await _openAiClient.GetChatCompletionsAsync(completionsOptions).ConfigureAwait(false);

            // Get the response message
            var responseMessage = response.Value.Choices[0].Message;

            // Add the response to the message history
            _messages.Add(new ChatRequestAssistantMessage(responseMessage.Content));

            // Return the response text
            return responseMessage.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            // Log the error and rethrow
            Console.Error.WriteLine($"Error getting AI response: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the chat message history
    /// </summary>
    public IReadOnlyCollection<ChatRequestMessage> Messages => _messages.AsReadOnly();
}
