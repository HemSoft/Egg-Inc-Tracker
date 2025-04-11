namespace HemSoft.News.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// A simple server for handling MCP (Model-Completion-Protocol) requests
/// </summary>
public class MCPServer
{
    private readonly ILogger<MCPServer> _logger;
    private readonly MCPToolHandler _toolHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="MCPServer"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="toolHandler">The tool handler</param>
    public MCPServer(ILogger<MCPServer> logger, MCPToolHandler toolHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolHandler = toolHandler ?? throw new ArgumentNullException(nameof(toolHandler));
    }

    /// <summary>
    /// Handles an MCP request
    /// </summary>
    /// <param name="requestJson">The request JSON</param>
    /// <returns>The response JSON</returns>
    public async Task<string> HandleRequestAsync(string requestJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestJson);

        try
        {
            _logger.LogInformation("Handling MCP request");

            // Parse the request
            var request = JsonSerializer.Deserialize<MCPRequest>(requestJson);
            if (request == null)
            {
                _logger.LogWarning("Invalid MCP request format");
                return JsonSerializer.Serialize(new MCPResponse
                {
                    Success = false,
                    Error = "Invalid request format"
                });
            }

            // Handle the request
            var result = await _toolHandler.HandleToolRequestAsync(request.Tool, request.Parameters).ConfigureAwait(false);

            // Return the response
            return JsonSerializer.Serialize(new MCPResponse
            {
                Success = true,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request: {ErrorMessage}", ex.Message);
            return JsonSerializer.Serialize(new MCPResponse
            {
                Success = false,
                Error = $"Error handling request: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Gets the list of available tools
    /// </summary>
    /// <returns>A JSON string containing the list of available tools</returns>
    public string GetAvailableTools()
    {
        var tools = _toolHandler.GetRegisteredTools();
        return JsonSerializer.Serialize(new { Tools = tools });
    }

    /// <summary>
    /// Class representing an MCP request
    /// </summary>
    private class MCPRequest
    {
        /// <summary>
        /// Gets or sets the tool to execute
        /// </summary>
        public string Tool { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters for the tool
        /// </summary>
        public string Parameters { get; set; } = string.Empty;
    }

    /// <summary>
    /// Class representing an MCP response
    /// </summary>
    private class MCPResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result of the request
        /// </summary>
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if the request failed
        /// </summary>
        public string? Error { get; set; }
    }
}
