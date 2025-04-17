namespace HemSoft.News.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for MCP (Model-Completion-Protocol) tools
/// </summary>
public class MCPToolHandler
{
    private readonly ILogger<MCPToolHandler> _logger;
    private readonly Dictionary<string, Func<string, Task<string>>> _tools;

    /// <summary>
    /// Initializes a new instance of the <see cref="MCPToolHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public MCPToolHandler(ILogger<MCPToolHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tools = new Dictionary<string, Func<string, Task<string>>>();
    }

    /// <summary>
    /// Registers a tool with the handler
    /// </summary>
    /// <param name="toolName">The name of the tool</param>
    /// <param name="toolFunction">The function to execute when the tool is called</param>
    public void RegisterTool(string toolName, Func<string, Task<string>> toolFunction)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);
        ArgumentNullException.ThrowIfNull(toolFunction);

        _tools[toolName] = toolFunction;
        _logger.LogInformation("Registered tool: {ToolName}", toolName);
    }

    /// <summary>
    /// Handles a tool request
    /// </summary>
    /// <param name="toolName">The name of the tool to execute</param>
    /// <param name="parameters">The parameters for the tool</param>
    /// <returns>The result of the tool execution</returns>
    public async Task<string> HandleToolRequestAsync(string toolName, string parameters)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);

        if (!_tools.TryGetValue(toolName, out var toolFunction))
        {
            _logger.LogWarning("Tool not found: {ToolName}", toolName);
            return JsonSerializer.Serialize(new { error = $"Tool '{toolName}' not found" });
        }

        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", toolName);
            return await toolFunction(parameters).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}: {ErrorMessage}", toolName, ex.Message);
            return JsonSerializer.Serialize(new { error = $"Error executing tool '{toolName}': {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets the list of registered tools
    /// </summary>
    /// <returns>A list of registered tool names</returns>
    public IReadOnlyCollection<string> GetRegisteredTools()
    {
        return _tools.Keys;
    }
}
