using HemSoft.News.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HemSoft.News.Data;

/// <summary>
/// Database context for the news database
/// </summary>
public class NewsDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the news items
    /// </summary>
    public DbSet<NewsItem> NewsItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the news sources
    /// </summary>
    public DbSet<NewsSource> NewsSources { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsDbContext"/> class
    /// </summary>
    /// <param name="options">The options to be used by the context</param>
    public NewsDbContext(DbContextOptions<NewsDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the NewsItem entity
        modelBuilder.Entity<NewsItem>(entity =>
        {
            entity.HasIndex(e => new { e.Source, e.Title, e.PublishedDate })
                .IsUnique();
        });

        // Configure the NewsSource entity
        modelBuilder.Entity<NewsSource>(entity =>
        {
            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        // Seed some default news sources
        modelBuilder.Entity<NewsSource>().HasData(
            new NewsSource
            {
                Id = 1,
                Name = "Microsoft.Extensions.AI NuGet Packages",
                Type = "NuGet",
                Url = "https://www.nuget.org/packages?q=Microsoft.Extensions.AI",
                Query = "Microsoft.Extensions.AI",
                CheckFrequencyMinutes = 360, // 6 hours
                IsActive = true
            }
        );
    }
}
