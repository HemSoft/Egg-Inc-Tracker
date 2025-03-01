using HemSoft.EggIncTracker.Data.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace EggDash.Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PlayerDto?> GetLatestPlayerAsync(string playerName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v1/players/latest/{playerName}");
                
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
    }
} 