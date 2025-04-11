using HemSoft.News.Data.Models;
using HemSoft.News.Data.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using HemSoft.News.Functions.Functions;
using HemSoft.News.Functions.Services;

namespace HemSoft.News.Functions.Tests.Functions;

public class NuGetMonitorFunctionTests
{
    private readonly Mock<ILogger<NuGetMonitorFunction>> _loggerMock;
    private readonly Mock<IFirecrawlService> _firecrawlServiceMock;
    private readonly Mock<INewsRepository> _newsRepositoryMock;
    private readonly Mock<IAIContentParsingService> _aiContentParsingServiceMock;
    private readonly NuGetMonitorFunction _function;

    public NuGetMonitorFunctionTests()
    {
        _loggerMock = new Mock<ILogger<NuGetMonitorFunction>>();
        _firecrawlServiceMock = new Mock<IFirecrawlService>();
        _newsRepositoryMock = new Mock<INewsRepository>();
        _aiContentParsingServiceMock = new Mock<IAIContentParsingService>();
        _function = new NuGetMonitorFunction(_loggerMock.Object, _firecrawlServiceMock.Object, _newsRepositoryMock.Object, _aiContentParsingServiceMock.Object);
    }

    [Fact]
    public async Task Run_ProcessesNewsSources_WhenSourcesNeedToBeChecked()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var newsSource = new NewsSource
        {
            Id = 1,
            Name = "Test Source",
            Type = "NuGet",
            Url = "https://test.com",
            Query = "test",
            IsActive = true
        };

        _newsRepositoryMock
            .Setup(r => r.GetNewsSourcesToCheckAsync())
            .ReturnsAsync(new List<NewsSource> { newsSource });

        _firecrawlServiceMock
            .Setup(s => s.ScrapeUrlAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync("[Test Package](https://test.com/package) This is a test package");

        _aiContentParsingServiceMock
            .Setup(s => s.ParseNuGetPackagesAsync(It.IsAny<NewsSource>(), It.IsAny<string>()))
            .ReturnsAsync(new List<NewsItem>
            {
                new NewsItem
                {
                    Title = "Test Package",
                    Url = "https://test.com/package",
                    Description = "This is a test package",
                    Source = "NuGet"
                }
            });

        // Act
        await _function.Run(timerInfo);

        // Assert
        _newsRepositoryMock.Verify(r => r.GetNewsSourcesToCheckAsync(), Times.Once);
        _firecrawlServiceMock.Verify(
            s => s.ScrapeUrlAsync(
                It.Is<string>(url => url == "https://test.com"),
                It.IsAny<string[]>()),
            Times.Once);
        _newsRepositoryMock.Verify(
            r => r.UpdateNewsSourceLastCheckedAsync(It.Is<int>(id => id == 1), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var expectedException = new Exception("Test exception");

        _newsRepositoryMock
            .Setup(r => r.GetNewsSourcesToCheckAsync())
            .ThrowsAsync(expectedException);

        // Act
        await _function.Run(timerInfo);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_ProcessesNewsItems_AndSavesToDatabase()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var newsSource = new NewsSource
        {
            Id = 1,
            Name = "Test Source",
            Type = "NuGet",
            Url = "https://test.com",
            Query = "Test",
            IsActive = true
        };

        _newsRepositoryMock
            .Setup(r => r.GetNewsSourcesToCheckAsync())
            .ReturnsAsync(new List<NewsSource> { newsSource });

        _firecrawlServiceMock
            .Setup(s => s.ScrapeUrlAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync("[Test Package](https://test.com/package) This is a test package");

        _aiContentParsingServiceMock
            .Setup(s => s.ParseNuGetPackagesAsync(It.IsAny<NewsSource>(), It.IsAny<string>()))
            .ReturnsAsync(new List<NewsItem>
            {
                new NewsItem
                {
                    Title = "Test Package",
                    Url = "https://test.com/package",
                    Description = "This is a test package",
                    Source = "NuGet"
                }
            });

        _newsRepositoryMock
            .Setup(r => r.AddNewsItemAsync(It.IsAny<NewsItem>()))
            .ReturnsAsync((NewsItem item) => item);

        // Act
        await _function.Run(timerInfo);

        // Assert
        _newsRepositoryMock.Verify(
            r => r.AddNewsItemAsync(It.Is<NewsItem>(item =>
                item.Title == "Test Package" &&
                item.Url == "https://test.com/package" &&
                item.Description == "This is a test package")),
            Times.Once);
    }
}
