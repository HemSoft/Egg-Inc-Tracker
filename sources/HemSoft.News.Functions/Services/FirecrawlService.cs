namespace HemSoft.News.Functions.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

/// <summary>
/// Service for interacting with the Firecrawl web scraping API
/// </summary>
public class FirecrawlService : IFirecrawlService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FirecrawlService> _logger;
    private readonly string _apiKey;
    private readonly string _apiUrl = "https://api.firecrawl.dev/v1/scrape";

    public FirecrawlService(HttpClient httpClient, ILogger<FirecrawlService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Firecrawl:ApiKey"] ?? throw new ArgumentNullException("Firecrawl:ApiKey", "API key not found in configuration");
    }

    /// <inheritdoc/>
    public async Task<string> ScrapeUrlAsync(string url, string[] formats)
    {
        try
        {
            var requestData = new
            {
                url = url,
                formats = formats
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            // Add the API key to the request headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping URL {Url}: {Message}", url, ex.Message);
            throw;
        }
    }
}
