namespace HemSoft.News.Functions.Services;

/// <summary>
/// Interface for the Firecrawl web scraping service
/// </summary>
public interface IFirecrawlService
{
    /// <summary>
    /// Scrapes a URL and returns the content in the specified format
    /// </summary>
    /// <param name="url">The URL to scrape</param>
    /// <param name="formats">The formats to return the content in (e.g., "markdown", "html", "text")</param>
    /// <returns>The scraped content</returns>
    Task<string> ScrapeUrlAsync(string url, string[] formats);
}
