using HemSoft.EggIncTracker.Data.Dtos;
using System.Net.Http.Json;


namespace EggDash.Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://localhost:51412/");
            }

            // Ensure there's a trailing slash on the base address
            if (_httpClient.BaseAddress != null && !_httpClient.BaseAddress.ToString().EndsWith("/"))
            {
                _httpClient.BaseAddress = new Uri(_httpClient.BaseAddress.ToString() + "/");
            }
            _logger = logger;
        }

        public async Task<PlayerDto?> GetLatestPlayerAsync(string playerName)
        {
            try
            {
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var response = await _httpClient.GetAsync($"api/v1/players/latest/{encodedPlayerName}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PlayerDto>();
                }

                _logger.LogError($"Error fetching player data: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLatestPlayerAsync");
                return null;
            }
        }


        public async Task<PlayerStatsDto?> GetPlayerStatsAsync(string playerName, int recordLimit = 1, int sampleDaysBack = 30)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v1/players/stats/{playerName}?recordLimit={recordLimit}&sampleDaysBack={sampleDaysBack}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PlayerStatsDto>();
                }
                
                _logger.LogError($"Error fetching player stats: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPlayerStatsAsync");
                return null;
            }
        }

        public async Task<List<PlayerDto>?> GetPlayerHistoryAsync(
            string playerName,
            DateTime? from = null,
            DateTime? to = null,
            int? limit = null)
        {
            try
            {
                // Build the query string with optional parameters
                var queryParams = new List<string>();

                if (from.HasValue)
                {
                    queryParams.Add($"from={from.Value:o}");
                }

                if (to.HasValue)
                {
                    queryParams.Add($"to={to.Value:o}");
                }

                if (limit.HasValue)
                {
                    queryParams.Add($"limit={limit.Value}");
                }

                var queryString = queryParams.Count > 0
                    ? $"?{string.Join("&", queryParams)}"
                    : string.Empty;

                var response = await _httpClient.GetAsync($"api/v1/players/{playerName}/history{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<PlayerDto>>();
                }

                _logger.LogError($"Error fetching player history: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPlayerHistoryAsync");
                return null;
            }
        }

        public async Task<PlayerStatsDto?> GetRankedPlayerAsync(string playerName, int recordLimit = 1, int sampleDaysBack = 30)
        {
            try
            {
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var response = await _httpClient.GetAsync($"api/v1/players/{encodedPlayerName}/ranked?recordLimit={recordLimit}&sampleDaysBack={sampleDaysBack}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PlayerStatsDto>();
                }

                _logger.LogError($"Error fetching ranked player data: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRankedPlayerAsync");
                return null;
            }
        }
        
        public async Task<TitleInfoDto?> GetTitleInfoAsync(string playerName)
        {
            try
            {
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var response = await _httpClient.GetAsync($"api/v1/players/{encodedPlayerName}/title-info");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TitleInfoDto>();
                }

                _logger.LogError($"Error fetching title info: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTitleInfoAsync");
                return null;
            }
        }
    }
    
    // DTO for title information to match the API response
    public class TitleInfoDto
    {
        public string CurrentTitle { get; set; } = string.Empty;
        public string NextTitle { get; set; } = string.Empty;
        public double TitleProgress { get; set; }
        public string ProjectedTitleChange { get; set; } = string.Empty;
    }
} 