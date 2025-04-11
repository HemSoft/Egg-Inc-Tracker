using HemSoft.News.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HemSoft.News.Data.Repositories;

/// <summary>
/// Repository for accessing news data
/// </summary>
public class NewsRepository : INewsRepository
{
    private readonly NewsDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsRepository"/> class
    /// </summary>
    /// <param name="context">The database context</param>
    public NewsRepository(NewsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsItem>> GetAllNewsItemsAsync()
    {
        return await _context.NewsItems
            .OrderByDescending(n => n.PublishedDate)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<NewsItem?> GetNewsItemByIdAsync(int id)
    {
        return await _context.NewsItems.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsItem>> GetNewsItemsBySourceAsync(string source)
    {
        return await _context.NewsItems
            .Where(n => n.Source == source)
            .OrderByDescending(n => n.PublishedDate)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsItem>> GetNewsItemsByCategoryAsync(string category)
    {
        return await _context.NewsItems
            .Where(n => n.Category == category)
            .OrderByDescending(n => n.PublishedDate)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsItem>> GetUnreadNewsItemsAsync()
    {
        return await _context.NewsItems
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.PublishedDate)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<NewsItem> AddNewsItemAsync(NewsItem newsItem)
    {
        _context.NewsItems.Add(newsItem);
        await _context.SaveChangesAsync();
        return newsItem;
    }

    /// <inheritdoc/>
    public async Task<NewsItem> UpdateNewsItemAsync(NewsItem newsItem)
    {
        _context.Entry(newsItem).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return newsItem;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteNewsItemAsync(int id)
    {
        var newsItem = await _context.NewsItems.FindAsync(id);
        if (newsItem == null)
        {
            return false;
        }

        _context.NewsItems.Remove(newsItem);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsSource>> GetAllNewsSourcesAsync()
    {
        return await _context.NewsSources.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<NewsSource?> GetNewsSourceByIdAsync(int id)
    {
        return await _context.NewsSources.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsSource>> GetActiveNewsSourcesAsync()
    {
        return await _context.NewsSources
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NewsSource>> GetNewsSourcesToCheckAsync()
    {
        var now = DateTime.UtcNow;
        var newsSourcesToCheck = await _context.NewsSources
            .Where(s => s.IsActive && 
                       (s.LastChecked == null || 
                        s.LastChecked.Value.AddMinutes(s.CheckFrequencyMinutes) <= now))
            .ToListAsync();
        return newsSourcesToCheck;
    }

    /// <inheritdoc/>
    public async Task<NewsSource> AddNewsSourceAsync(NewsSource newsSource)
    {
        _context.NewsSources.Add(newsSource);
        await _context.SaveChangesAsync();
        return newsSource;
    }

    /// <inheritdoc/>
    public async Task<NewsSource> UpdateNewsSourceAsync(NewsSource newsSource)
    {
        _context.Entry(newsSource).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return newsSource;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteNewsSourceAsync(int id)
    {
        var newsSource = await _context.NewsSources.FindAsync(id);
        if (newsSource == null)
        {
            return false;
        }

        _context.NewsSources.Remove(newsSource);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateNewsSourceLastCheckedAsync(int id, DateTime lastChecked)
    {
        var newsSource = await _context.NewsSources.FindAsync(id);
        if (newsSource == null)
        {
            return false;
        }

        newsSource.LastChecked = lastChecked;
        await _context.SaveChangesAsync();
        return true;
    }
}
