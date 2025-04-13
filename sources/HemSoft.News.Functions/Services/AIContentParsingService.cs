namespace HemSoft.News.Functions.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HemSoft.AI;
using HemSoft.News.Data.Models;
using HemSoft.News.Data.Repositories;
using HemSoft.News.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// Service for parsing content using AI
/// </summary>
public class AIContentParsingService : IAIContentParsingService
{
    private readonly ILogger<AIContentParsingService> _logger;
    private readonly NewsContentParser _newsContentParser;
    private readonly INewsRepository _newsRepository;
    private readonly ChatClient _chatClient; // Keep the main one if needed elsewhere, or remove if only used here temporarily
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContentParsingService"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="newsContentParser">The news content parser</param>
    /// <param name="newsRepository">The news repository</param>
    /// <param name="chatClient">The AI chat client (potentially shared instance)</param>
    /// <param name="configuration">The application configuration</param>
    public AIContentParsingService(
        ILogger<AIContentParsingService> logger,
        NewsContentParser newsContentParser,
        INewsRepository newsRepository,
        ChatClient chatClient, // Keep or remove based on usage pattern
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _newsContentParser = newsContentParser ?? throw new ArgumentNullException(nameof(newsContentParser));
        _newsRepository = newsRepository ?? throw new ArgumentNullException(nameof(newsRepository));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient)); // Keep or remove
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc/>
    public async Task<List<NewsItem>> ParseNuGetPackagesAsync(NewsSource source, string content)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentException.ThrowIfNullOrEmpty(source.Query, nameof(source.Query)); // Query must exist for NuGet sources

        var newsItemsToCreate = new List<NewsItem>();

        try
        {
            _logger.LogInformation("Parsing NuGet packages for source '{SourceName}' with query '{Query}'", source.Name, source.Query);

            // Use the NewsContentParser to get potential package info from the scraped content
            var parserResultJson = await _newsContentParser.ParseNuGetPackagesAsync(content).ConfigureAwait(false);

            List<NewsContentParser.PackageInfo>? packageInfos = null;
            try
            {
                packageInfos = System.Text.Json.JsonSerializer.Deserialize<List<NewsContentParser.PackageInfo>>(parserResultJson);
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize package info JSON from NewsContentParser for source '{SourceName}'. JSON: {Json}", source.Name, parserResultJson);
                return newsItemsToCreate; // Cannot proceed without parsed info
            }

            if (packageInfos == null || packageInfos.Count == 0)
            {
                _logger.LogWarning("NewsContentParser returned no package info for source '{SourceName}'", source.Name);
                return newsItemsToCreate;
            }

            // Find the specific package matching the query (exact match)
            var targetPackageInfo = packageInfos.FirstOrDefault(p =>
                p.Title.Equals(source.Query, StringComparison.OrdinalIgnoreCase));

            if (targetPackageInfo == null)
            {
                _logger.LogWarning("No package found matching exact query '{Query}' for source '{SourceName}'", source.Query, source.Name);
                return newsItemsToCreate;
            }

            // Get the latest existing news item for this package ID
            var latestExistingItem = await _newsRepository.GetLatestNewsItemByTitleAsync(source.Query);

            string? latestVersion = null;
            if (latestExistingItem?.AdditionalData != null)
            {
                try
                {
                    var additionalData = JsonConvert.DeserializeObject<dynamic>(latestExistingItem.AdditionalData);
                    latestVersion = additionalData?.Version;
                }
                catch (System.Text.Json.JsonException ex) // Qualify the exception type
                {
                    _logger.LogWarning(ex, "Could not parse AdditionalData JSON for existing NewsItem Id {Id}", latestExistingItem.Id);
                }
            }

            // Check if the version is new
            bool isNewVersion = latestExistingItem == null ||
                                (targetPackageInfo.Version != null && !targetPackageInfo.Version.Equals(latestVersion, StringComparison.OrdinalIgnoreCase));

            if (isNewVersion)
            {
                var newsItem = new NewsItem
                {
                    Title = targetPackageInfo.Title,
                    Description = $"Version {targetPackageInfo.Version ?? "N/A"}. {targetPackageInfo.Description ?? ""}".Trim(),
                    Url = targetPackageInfo.Url,
                    Source = source.Type,
                    Category = "Package Update",
                    PublishedDate = DateTime.UtcNow,
                    DiscoveredDate = DateTime.UtcNow,
                    IsRead = false,
                    AdditionalData = JsonConvert.SerializeObject(new { PackageId = targetPackageInfo.Title, Version = targetPackageInfo.Version })
                };
                newsItemsToCreate.Add(newsItem);
            }
            else
            {
                _logger.LogInformation("No new version found for package '{PackageId}'. Current version: '{Version}'", source.Query, latestVersion);
            }

            return newsItemsToCreate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NuGet package source '{SourceName}': {ErrorMessage}", source.Name, ex.Message);
            return newsItemsToCreate; // Return empty list on error
        }
    }
}
