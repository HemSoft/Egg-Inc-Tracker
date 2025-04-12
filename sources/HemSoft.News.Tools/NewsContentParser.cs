namespace HemSoft.News.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Models;
using HemSoft.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tool for parsing news content using AI
/// </summary>
public class NewsContentParser
{
    private readonly ILogger<NewsContentParser> _logger;
    private readonly IConfiguration _configuration;
    private readonly ChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsContentParser"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="configuration">The configuration</param>
    public NewsContentParser(ILogger<NewsContentParser> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Create a chat client for content parsing
        _chatClient = ChatClientFactory.CreateForContentParsing(_configuration);
    }

    /// <summary>
    /// Parses NuGet package information from content
    /// </summary>
    /// <param name="content">The content to parse</param>
    /// <returns>A JSON string containing the parsed package information</returns>
    public async Task<string> ParseNuGetPackagesAsync(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);

        try
        {
            _logger.LogInformation("Parsing NuGet packages from content");

            // Create a specialized chat client for NuGet parsing
            var nugetChatClient = ChatClientFactory.CreateForNuGetParsing(_configuration);

            // Add the content as a user message
            nugetChatClient.AddUserMessage($"Parse the following content and extract NuGet package information:\n\n{content}");
            nugetChatClient.AddUserMessage($"Give me back the exact json that I can deserialize into { typeof(NewsItem) }");

            // Create chat options for the request
            var chatOptions = new ChatClientOptions();

            // Get the response
            var response = await nugetChatClient.GetResponseAsync(chatOptions).ConfigureAwait(false);

            _logger.LogInformation("Successfully parsed NuGet packages");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing NuGet packages: {ErrorMessage}", ex.Message);

            // Return a fallback response with basic extraction
            _logger.LogWarning("Using fallback package extraction method");
            return FallbackParseNuGetPackages(content);
        }
    }

    /// <summary>
    /// Tool function to extract package information
    /// </summary>
    /// <param name="content">The content to extract from</param>
    /// <returns>A list of package information</returns>
    [System.ComponentModel.Description("Extract package information from the content and return a structured list")]
    private List<PackageInfo> ExtractPackages(string content)
    {
        try
        {
            // This is a placeholder implementation
            // In a real implementation, this would parse the content and extract package information
            // For now, we'll just return an empty list
            return new List<PackageInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting packages: {ErrorMessage}", ex.Message);
            return new List<PackageInfo>();
        }
    }

    /// <summary>
    /// Fallback method to parse NuGet packages when AI parsing fails
    /// </summary>
    /// <param name="content">The content to parse</param>
    /// <returns>A string containing the parsed package information</returns>
    private string FallbackParseNuGetPackages(string content)
    {
        try
        {
            // Use regex to extract package information
            var packagePattern = @"\[([^\]]+)\]\(([^\)]+)\)\s*([^\n]+)";
            var matches = System.Text.RegularExpressions.Regex.Matches(content, packagePattern);

            var packages = new List<PackageInfo>();

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    var title = match.Groups[1].Value.Trim();
                    var url = match.Groups[2].Value.Trim();
                    var description = match.Groups[3].Value.Trim();

                    packages.Add(new PackageInfo
                    {
                        Title = title,
                        Url = url,
                        Description = description
                    });
                }
            }

            // Convert to JSON string
            return JsonSerializer.Serialize(packages, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback parsing: {ErrorMessage}", ex.Message);
            return "[]";
        }
    }

    /// <summary>
    /// Class representing package information
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// Gets or sets the package title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the package URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the package description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the package version
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the package release date
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the package author
        /// </summary>
        public string? Author { get; set; }
    }
}
