namespace HemSoft.News.Functions.Models;

/// <summary>
/// Represents a NuGet package
/// </summary>
public class NuGetPackage
{
    /// <summary>
    /// The package ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The package version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The package description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The package author
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The package URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The date the package was published
    /// </summary>
    public DateTime PublishedDate { get; set; }

    /// <summary>
    /// The date the package was discovered
    /// </summary>
    public DateTime DiscoveredDate { get; set; } = DateTime.UtcNow;
}
