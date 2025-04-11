using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using HemSoft.News.Functions.Services;

namespace HemSoft.News.Functions.Tests.Services;

public class FirecrawlServiceTests
{
    private readonly Mock<ILogger<FirecrawlService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public FirecrawlServiceTests()
    {
        _loggerMock = new Mock<ILogger<FirecrawlService>>();
        _configurationMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Setup configuration to return a test API key
        _configurationMock.Setup(c => c["Firecrawl:ApiKey"]).Returns("test-api-key");
    }

    [Fact]
    public async Task ScrapeUrlAsync_ReturnsExpectedResponse()
    {
        // Arrange
        var expectedResponse = "{\"content\":\"Test content\"}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedResponse, Encoding.UTF8, "application/json")
            });

        var service = new FirecrawlService(_httpClient, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await service.ScrapeUrlAsync("https://test.com", new[] { "markdown" });

        // Assert
        Assert.Equal(expectedResponse, result);

        // Verify that the request was made with the correct headers and content
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null && req.RequestUri.ToString() == "https://api.firecrawl.dev/v1/scrape" &&
                req.Headers.Contains("Authorization")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ScrapeUrlAsync_ThrowsException_WhenApiReturnsError()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Bad request\"}", Encoding.UTF8, "application/json")
            });

        var service = new FirecrawlService(_httpClient, _loggerMock.Object, _configurationMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.ScrapeUrlAsync("https://test.com", new[] { "markdown" }));
    }
}
