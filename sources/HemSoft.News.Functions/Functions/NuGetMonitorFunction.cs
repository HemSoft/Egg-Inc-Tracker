namespace HemSoft.News.Functions.Functions;

using HemSoft.News.Data.Models;
using HemSoft.News.Data.Repositories;
using HemSoft.News.Functions.Models;
using HemSoft.News.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Azure Function to monitor NuGet packages for updates
/// </summary>
public class NuGetMonitorFunction
{
    private static readonly string[] MarkdownFormat = { "markdown" };

    private readonly ILogger<NuGetMonitorFunction> _logger;
    private readonly IFirecrawlService _firecrawlService;
    private readonly INewsRepository _newsRepository;
    private readonly IAIContentParsingService _aiContentParsingService;

    public NuGetMonitorFunction(
        ILogger<NuGetMonitorFunction> logger,
        IFirecrawlService firecrawlService,
        INewsRepository newsRepository,
        IAIContentParsingService aiContentParsingService)
    {
        _logger = logger;
        _firecrawlService = firecrawlService;
        _newsRepository = newsRepository;
        _aiContentParsingService = aiContentParsingService;
    }

    /// <summary>
    /// Timer-triggered function to monitor news sources that need to be checked
    /// </summary>
    /// <param name="myTimer">Timer information</param>
    [Function("MonitorNewsSources")]
    public async Task Run([TimerTrigger("0 0 */2 * * *")] TimerInfo myTimer)
    {
        try
        {
            // Get all news sources that need to be checked
            var newsSources = await _newsRepository.GetNewsSourcesToCheckAsync();

            foreach (var source in newsSources)
            {
                try
                {
                    // Scrape the URL
                    if (source.Url == null)
                    {
                        _logger.LogWarning("URL is null for news source: {Name}", source.Name);
                        continue;
                    }

                    var result = await _firecrawlService.ScrapeUrlAsync(source.Url, MarkdownFormat);
                    var newsItems = await ProcessScrapedContentAsync(source, result);
                    await _newsRepository.UpdateNewsSourceLastCheckedAsync(source.Id, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking news source {Name}: {Message}", source.Name, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring news sources: {Message}", ex.Message);
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {Next}", myTimer.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Process the scraped content based on the source type
    /// </summary>
    /// <param name="source">The news source</param>
    /// <param name="content">The scraped content</param>
    /// <returns>A list of news items</returns>
    private async Task<List<NewsItem>> ProcessScrapedContentAsync(NewsSource source, string content)
    {
        var newsItems = new List<NewsItem>();

        switch (source.Type.ToLowerInvariant())
        {
            case "nuget":
                // Use the AI-based content parsing service
                newsItems = await _aiContentParsingService.ParseNuGetPackagesAsync(source, content);
                break;

            // Add more source types here as needed

            default:
                _logger.LogWarning("Unknown news source type: {Type}", source.Type);
                break;
        }

        // Save the news items to the database
        foreach (var item in newsItems)
        {
            try
            {
                await _newsRepository.AddNewsItemAsync(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving news item {Title}: {Message}", item.Title, ex.Message);
            }
        }

        return newsItems;
    }
}
