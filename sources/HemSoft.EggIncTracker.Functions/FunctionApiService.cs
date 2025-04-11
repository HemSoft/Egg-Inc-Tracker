using System.Net.Http.Json;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;

namespace UpdatePlayerFunction
{
    public class FunctionApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl") ?? "https://localhost:5000/";

        public FunctionApiService(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Log the current base address if it exists
            if (_httpClient.BaseAddress != null)
            {
                // Only override if it's not the one we want and not port 5000
                if (!_httpClient.BaseAddress.ToString().Contains("5000"))
                {
                    _httpClient.BaseAddress = new Uri(_apiBaseUrl);
                }
            }
            else
            {
                _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            }

            // Ensure there's a trailing slash on the base address
            if (_httpClient.BaseAddress != null && !_httpClient.BaseAddress.ToString().EndsWith("/"))
            {
                _httpClient.BaseAddress = new Uri(_httpClient.BaseAddress.ToString() + "/");
            }
        }

        public async Task<SurroundingPlayersDto?> GetSurroundingSEPlayersAsync(string playerName, string soulEggs)
        {
            try
            {
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var encodedSoulEggs = Uri.EscapeDataString(soulEggs);
                var url = $"api/v1/majplayerrankings/surrounding/se/{encodedPlayerName}?soulEggs={encodedSoulEggs}";
                return await _httpClient.GetFromJsonAsync<SurroundingPlayersDto>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSurroundingSEPlayersAsync");
                return null;
            }
        }

        public async Task<SurroundingPlayersDto?> GetSurroundingEBPlayersAsync(string playerName, string earningsBonus)
        {
            try
            {
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var encodedEarningsBonus = Uri.EscapeDataString(earningsBonus);
                var url = $"api/v1/majplayerrankings/surrounding/eb/{encodedPlayerName}?earningsBonus={encodedEarningsBonus}";
                return await _httpClient.GetFromJsonAsync<SurroundingPlayersDto>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSurroundingEBPlayersAsync");
                return null;
            }
        }

        public async Task<SurroundingPlayersDto?> GetSurroundingMERPlayersAsync(string playerName, decimal mer)
        {
            try
            {
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var url = $"api/v1/majplayerrankings/surrounding/mer/{encodedPlayerName}?mer={mer}";
                return await _httpClient.GetFromJsonAsync<SurroundingPlayersDto>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSurroundingMERPlayersAsync");
                return null;
            }
        }

        public async Task<SurroundingPlayersDto?> GetSurroundingJERPlayersAsync(string playerName, decimal jer)
        {
            try
            {
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var url = $"api/v1/majplayerrankings/surrounding/jer/{encodedPlayerName}?jer={jer}";
                return await _httpClient.GetFromJsonAsync<SurroundingPlayersDto>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSurroundingJERPlayersAsync");
                return null;
            }
        }

        public async Task<List<MajPlayerRankingDto>?> GetLatestMajPlayerRankingsAsync(int limit = 30)
        {
            try
            {
                var url = $"api/v1/majplayerrankings/latest-all?limit={limit}";
                return await _httpClient.GetFromJsonAsync<List<MajPlayerRankingDto>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLatestMajPlayerRankingsAsync");
                return null;
            }
        }

        public async Task<SaveRankingResponseDto?> SaveMajPlayerRankingAsync(MajPlayerRankingDto majPlayerRanking)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/majplayerrankings", majPlayerRanking);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SaveRankingResponseDto>();
                }

                _logger.LogWarning($"Failed to save MajPlayerRanking, StatusCode: {response.StatusCode}");
                return new SaveRankingResponseDto
                {
                    Success = false,
                    Message = $"API returned {response.StatusCode}",
                    PreviousRanking = new MajPlayerRankingDto()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveMajPlayerRankingAsync");
                return new SaveRankingResponseDto
                {
                    Success = false,
                    Message = $"Exception: {ex.Message}",
                    PreviousRanking = new MajPlayerRankingDto()
                };
            }
        }
    }
}