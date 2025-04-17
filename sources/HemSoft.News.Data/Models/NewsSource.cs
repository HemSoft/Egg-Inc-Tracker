using System.ComponentModel.DataAnnotations;

namespace HemSoft.News.Data.Models;

/// <summary>
/// Represents a source of news items
/// </summary>
public class NewsSource
{
    /// <summary>
    /// The unique identifier for the news source
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The name of the news source
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the news source
    /// </summary>
    [MaxLength(2048)]
    public string? Url { get; set; }

    /// <summary>
    /// The type of the news source (e.g., "NuGet", "GitHub", "Blog")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The query or filter to use when fetching news from this source
    /// </summary>
    [MaxLength(1024)]
    public string? Query { get; set; }

    /// <summary>
    /// The frequency at which to check for news from this source (in minutes)
    /// </summary>
    public int CheckFrequencyMinutes { get; set; } = 360; // Default to 6 hours

    /// <summary>
    /// The last time this source was checked
    /// </summary>
    public DateTime? LastChecked { get; set; }

    /// <summary>
    /// Whether this source is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Configuration settings stored as JSON
    /// </summary>
    public string? Configuration { get; set; }
}
