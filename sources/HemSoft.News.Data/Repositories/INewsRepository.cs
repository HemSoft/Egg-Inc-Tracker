using HemSoft.News.Data.Models;

namespace HemSoft.News.Data.Repositories;

/// <summary>
/// Interface for the news repository
/// </summary>
public interface INewsRepository
{
    /// <summary>
    /// Gets all news items
    /// </summary>
    /// <returns>A collection of news items</returns>
    Task<IEnumerable<NewsItem>> GetAllNewsItemsAsync();

    /// <summary>
    /// Gets a news item by its ID
    /// </summary>
    /// <param name="id">The ID of the news item</param>
    /// <returns>The news item, or null if not found</returns>
    Task<NewsItem?> GetNewsItemByIdAsync(int id);

    /// <summary>
    /// Gets news items by source
    /// </summary>
    /// <param name="source">The source of the news items</param>
    /// <returns>A collection of news items from the specified source</returns>
    Task<IEnumerable<NewsItem>> GetNewsItemsBySourceAsync(string source);

    /// <summary>
    /// Gets news items by category
    /// </summary>
    /// <param name="category">The category of the news items</param>
    /// <returns>A collection of news items in the specified category</returns>
    Task<IEnumerable<NewsItem>> GetNewsItemsByCategoryAsync(string category);

    /// <summary>
    /// Gets unread news items
    /// </summary>
    /// <returns>A collection of unread news items</returns>
    Task<IEnumerable<NewsItem>> GetUnreadNewsItemsAsync();

    /// <summary>
    /// Adds a news item
    /// </summary>
    /// <param name="newsItem">The news item to add</param>
    /// <returns>The added news item</returns>
    Task<NewsItem> AddNewsItemAsync(NewsItem newsItem);

    /// <summary>
    /// Updates a news item
    /// </summary>
    /// <param name="newsItem">The news item to update</param>
    /// <returns>The updated news item</returns>
    Task<NewsItem> UpdateNewsItemAsync(NewsItem newsItem);

    /// <summary>
    /// Deletes a news item
    /// </summary>
    /// <param name="id">The ID of the news item to delete</param>
    /// <returns>True if the news item was deleted, false otherwise</returns>
    Task<bool> DeleteNewsItemAsync(int id);

    /// <summary>
    /// Gets the latest news item by title (case-insensitive)
    /// </summary>
    /// <param name="title">The title to search for</param>
    /// <returns>The latest news item matching the title, or null if not found</returns>
    Task<NewsItem?> GetLatestNewsItemByTitleAsync(string title);

    /// <summary>
    /// Gets all news sources
    /// </summary>
    /// <returns>A collection of news sources</returns>
    Task<IEnumerable<NewsSource>> GetAllNewsSourcesAsync();

    /// <summary>
    /// Gets a news source by its ID
    /// </summary>
    /// <param name="id">The ID of the news source</param>
    /// <returns>The news source, or null if not found</returns>
    Task<NewsSource?> GetNewsSourceByIdAsync(int id);

    /// <summary>
    /// Gets active news sources
    /// </summary>
    /// <returns>A collection of active news sources</returns>
    Task<IEnumerable<NewsSource>> GetActiveNewsSourcesAsync();

    /// <summary>
    /// Gets news sources that need to be checked
    /// </summary>
    /// <returns>A collection of news sources that need to be checked</returns>
    Task<IEnumerable<NewsSource>> GetNewsSourcesToCheckAsync();

    /// <summary>
    /// Adds a news source
    /// </summary>
    /// <param name="newsSource">The news source to add</param>
    /// <returns>The added news source</returns>
    Task<NewsSource> AddNewsSourceAsync(NewsSource newsSource);

    /// <summary>
    /// Updates a news source
    /// </summary>
    /// <param name="newsSource">The news source to update</param>
    /// <returns>The updated news source</returns>
    Task<NewsSource> UpdateNewsSourceAsync(NewsSource newsSource);

    /// <summary>
    /// Deletes a news source
    /// </summary>
    /// <param name="id">The ID of the news source to delete</param>
    /// <returns>True if the news source was deleted, false otherwise</returns>
    Task<bool> DeleteNewsSourceAsync(int id);

    /// <summary>
    /// Updates the last checked time for a news source
    /// </summary>
    /// <param name="id">The ID of the news source</param>
    /// <param name="lastChecked">The last checked time</param>
    /// <returns>True if the news source was updated, false otherwise</returns>
    Task<bool> UpdateNewsSourceLastCheckedAsync(int id, DateTime lastChecked);
}
