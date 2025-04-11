using HemSoft.News.Data.Models;
using HemSoft.News.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HemSoft.News.Data.Tests.Repositories;

public class NewsRepositoryTests
{
    private DbContextOptions<NewsDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<NewsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private NewsDbContext CreateContext()
    {
        var options = CreateNewContextOptions();
        var context = new NewsDbContext(options);
        context.Database.EnsureCreated();

        // Add some test data
        var newsSource = new NewsSource
        {
            Name = "Test Source",
            Type = "Test",
            Url = "https://test.com",
            Query = "test",
            IsActive = true
        };

        var newsItem = new NewsItem
        {
            Title = "Test News Item",
            Description = "This is a test news item",
            Source = "Test",
            Category = "Test",
            PublishedDate = DateTime.UtcNow,
            IsRead = false
        };

        context.NewsSources.Add(newsSource);
        context.NewsItems.Add(newsItem);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task GetAllNewsItemsAsync_ReturnsAllNewsItems()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new NewsRepository(context);

        // Act
        var result = await repository.GetAllNewsItemsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Test News Item", result.First().Title);
    }

    [Fact]
    public async Task GetNewsItemByIdAsync_ReturnsCorrectNewsItem()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new NewsRepository(context);

        // Act
        var result = await repository.GetNewsItemByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test News Item", result.Title);
    }

    [Fact]
    public async Task GetNewsItemsBySourceAsync_ReturnsCorrectNewsItems()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new NewsRepository(context);

        // Act
        var result = await repository.GetNewsItemsBySourceAsync("Test");

        // Assert
        Assert.Single(result);
        Assert.Equal("Test News Item", result.First().Title);
    }

    [Fact]
    public async Task AddNewsItemAsync_AddsNewsItem()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new NewsRepository(context);
        var newsItem = new NewsItem
        {
            Title = "New Test News Item",
            Description = "This is a new test news item",
            Source = "Test",
            Category = "Test",
            PublishedDate = DateTime.UtcNow,
            IsRead = false
        };

        // Act
        var result = await repository.AddNewsItemAsync(newsItem);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal("New Test News Item", result.Title);

        // Verify it was added to the database
        var allItems = await repository.GetAllNewsItemsAsync();
        Assert.Equal(2, allItems.Count());
    }
}
