using System.ComponentModel.DataAnnotations;

namespace HemSoft.News.Data.Models;

/// <summary>
/// Represents a news item from any source
/// </summary>
public class NewsItem
{
    /// <summary>
    /// The unique identifier for the news item
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The title or name of the news item
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The description or content of the news item
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The URL to the news item
    /// </summary>
    [MaxLength(2048)]
    public string? Url { get; set; }

    /// <summary>
    /// The source of the news item (e.g., "NuGet", "GitHub", "Blog")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The category of the news item (e.g., "Package", "Release", "Article")
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// The date the news item was published
    /// </summary>
    public DateTime PublishedDate { get; set; }

    /// <summary>
    /// The date the news item was discovered
    /// </summary>
    public DateTime DiscoveredDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the news item has been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Additional data stored as JSON
    /// </summary>
    public string? AdditionalData { get; set; }
}
