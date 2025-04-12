using System.Net.Http.Json;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;

namespace UpdatePlayerFunction
{
    public class FunctionApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionApiService> _logger;
        private readonly string _apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl") ?? "https://localhost:5000/";

        public FunctionApiService(HttpClient httpClient, ILogger<FunctionApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Set a reasonable timeout for the HttpClient (30 seconds instead of the default 100)
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

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

            _logger.LogInformation($"FunctionApiService initialized with BaseAddress: {_httpClient.BaseAddress} and Timeout: {_httpClient.Timeout.TotalSeconds} seconds");
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
                _logger.LogInformation($"Saving MajPlayerRanking for {majPlayerRanking.IGN} with SE: {majPlayerRanking.SEString}");

                // Use a separate timeout for this specific operation
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var response = await _httpClient.PostAsJsonAsync("api/v1/majplayerrankings", majPlayerRanking, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SaveRankingResponseDto>();
                    _logger.LogInformation($"Successfully saved MajPlayerRanking for {majPlayerRanking.IGN}");
                    return result;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to save MajPlayerRanking for {majPlayerRanking.IGN}, StatusCode: {response.StatusCode}, Response: {responseContent}");
                return new SaveRankingResponseDto
                {
                    Success = false,
                    Message = $"API returned {response.StatusCode}: {responseContent}",
                    PreviousRanking = new MajPlayerRankingDto()
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, $"Request timeout in SaveMajPlayerRankingAsync for {majPlayerRanking.IGN}");
                return new SaveRankingResponseDto
                {
                    Success = false,
                    Message = $"Request timed out: {ex.Message}",
                    PreviousRanking = new MajPlayerRankingDto()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SaveMajPlayerRankingAsync for {majPlayerRanking.IGN}");
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
