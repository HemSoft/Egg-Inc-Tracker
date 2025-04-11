namespace HemSoft.News.Functions.Services;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HemSoft.News.Data.Models;
using HemSoft.News.Tools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// Service for parsing content using AI
/// </summary>
public class AIContentParsingService : IAIContentParsingService
{
    private readonly ILogger<AIContentParsingService> _logger;
    private readonly NewsContentParser _newsContentParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContentParsingService"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="newsContentParser">The news content parser</param>
    public AIContentParsingService(ILogger<AIContentParsingService> logger, NewsContentParser newsContentParser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _newsContentParser = newsContentParser ?? throw new ArgumentNullException(nameof(newsContentParser));
    }

    /// <inheritdoc/>
    public async Task<List<NewsItem>> ParseNuGetPackagesAsync(NewsSource source, string content)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(content);

        try
        {
            _logger.LogInformation("Parsing NuGet packages using AI");

            // Use the NewsContentParser to parse the content
            var result = await _newsContentParser.ParseNuGetPackagesAsync(content).ConfigureAwait(false);

            // Parse the result into a list of NewsItem objects
            var newsItems = new List<NewsItem>();

            try
            {
                // Try to parse the result as JSON
                var packageInfos = System.Text.Json.JsonSerializer.Deserialize<List<HemSoft.News.Tools.NewsContentParser.PackageInfo>>(result);

                if (packageInfos != null && packageInfos.Count > 0)
                {
                    foreach (var packageInfo in packageInfos)
                    {
                        // Check if the package matches the query
                        if (!string.IsNullOrEmpty(source.Query) &&
                            !packageInfo.Title.Contains(source.Query, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var newsItem = new NewsItem
                        {
                            Title = packageInfo.Title,
                            Description = packageInfo.Description,
                            Url = packageInfo.Url,
                            Source = source.Type,
                            Category = "Package",
                            PublishedDate = packageInfo.ReleaseDate ?? DateTime.UtcNow,
                            IsRead = false,
                            AdditionalData = JsonConvert.SerializeObject(new { PackageId = packageInfo.Title, Version = packageInfo.Version })
                        };

                        newsItems.Add(newsItem);
                    }

                    _logger.LogInformation("Successfully parsed {Count} NuGet packages from JSON", newsItems.Count);
                    return newsItems;
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed to parse result as JSON, falling back to regex parsing");
            }

            // Fallback to regex parsing if JSON parsing fails or returns no results
            _logger.LogInformation("Using regex to parse packages from content");
            var packagePattern = @"\[([^\]]+)\]\(([^\)]+)\)\s*([^\n]+)";
            var matches = System.Text.RegularExpressions.Regex.Matches(content, packagePattern);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    var title = match.Groups[1].Value.Trim();
                    var url = match.Groups[2].Value.Trim();
                    var description = match.Groups[3].Value.Trim();

                    // Check if the package matches the query
                    if (!string.IsNullOrEmpty(source.Query) &&
                        !title.Contains(source.Query, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var newsItem = new NewsItem
                    {
                        Title = title,
                        Description = description,
                        Url = url,
                        Source = source.Type,
                        Category = "Package",
                        PublishedDate = DateTime.UtcNow, // Ideally, extract the actual published date
                        IsRead = false,
                        AdditionalData = JsonConvert.SerializeObject(new { PackageId = title })
                    };

                    newsItems.Add(newsItem);
                }
            }

            _logger.LogInformation("Successfully parsed {Count} NuGet packages using regex", newsItems.Count);
            return newsItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing NuGet packages: {ErrorMessage}", ex.Message);

            // Return an empty list instead of throwing an exception
            return new List<NewsItem>();
        }
    }
}
