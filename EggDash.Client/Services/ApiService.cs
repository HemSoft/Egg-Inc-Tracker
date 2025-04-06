using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using System.Net.Http.Json;

namespace EggDash.Client.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private const int MaxRetries = 3;
        private const int InitialRetryDelayMs = 300;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://localhost:5000/");
            }

            // Ensure there's a trailing slash on the base address
            if (_httpClient.BaseAddress != null && !_httpClient.BaseAddress.ToString().EndsWith("/"))
            {
                _httpClient.BaseAddress = new Uri(_httpClient.BaseAddress.ToString() + "/");
            }
            _logger = logger;

            // Set a reasonable timeout for the HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Helper method to execute an API call with retry logic
        /// </summary>
        private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> apiCall, string methodName)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    // If this is a retry, log it
                    if (attempt > 0)
                    {
                        _logger.LogInformation($"{methodName} - Retry attempt {attempt} of {MaxRetries}");
                    }

                    // Execute the API call
                    return await apiCall();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, $"{methodName} - Error on attempt {attempt + 1} of {MaxRetries}");

                    // If this is not the last attempt, wait before retrying with exponential backoff
                    if (attempt < MaxRetries - 1)
                    {
                        int delayMs = InitialRetryDelayMs * (int)Math.Pow(2, attempt);
                        _logger.LogInformation($"{methodName} - Waiting {delayMs}ms before retry");
                        await Task.Delay(delayMs);
                    }
                }
            }

            // If we get here, all retries failed
            _logger.LogError(lastException, $"{methodName} - All {MaxRetries} attempts failed");
            return default;
        }

        public async Task<PlayerDto?> GetLatestPlayerAsync(string playerName)
        {
            return await ExecuteWithRetryAsync<PlayerDto>(async () =>
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
            }, nameof(GetLatestPlayerAsync));
        }

        public async Task<PlayerDto?> GetPlayerByEIDAsync(string eid)
        {
            if (string.IsNullOrEmpty(eid))
            {
                _logger.LogWarning("EID is null or empty");
                return null;
            }

            return await ExecuteWithRetryAsync<PlayerDto>(async () =>
            {
                var response = await _httpClient.GetAsync($"api/v1/players/eid/{eid}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PlayerDto>();
                }

                _logger.LogError($"Error fetching player data by EID: {response.StatusCode}");
                return null;
            }, nameof(GetPlayerByEIDAsync));
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
            return await ExecuteWithRetryAsync<List<PlayerDto>>(async () =>
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

                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var response = await _httpClient.GetAsync($"api/v1/players/{encodedPlayerName}/history{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<PlayerDto>>();
                }

                _logger.LogError($"Error fetching player history: {response.StatusCode}");
                return null;
            }, nameof(GetPlayerHistoryAsync));
        }

        public async Task<PlayerStatsDto?> GetRankedPlayerAsync(string playerName, int recordLimit = 1, int sampleDaysBack = 30)
        {
            return await ExecuteWithRetryAsync<PlayerStatsDto>(async () =>
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
            }, nameof(GetRankedPlayerAsync));
        }

        public async Task<TitleInfoDto?> GetTitleInfoAsync(string playerName)
        {
            return await ExecuteWithRetryAsync<TitleInfoDto>(async () =>
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
            }, nameof(GetTitleInfoAsync));
        }

        // New methods for MajPlayerRankings endpoints

        public async Task<SurroundingPlayersDto?> GetSurroundingSEPlayersAsync(string playerName, string soulEggs)
        {
            try
            {
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var encodedSoulEggs = Uri.EscapeDataString(soulEggs);

                var response = await _httpClient.GetAsync($"api/v1/majplayerrankings/surrounding/se/{encodedPlayerName}?soulEggs={encodedSoulEggs}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                    return result;
                }

                _logger.LogError($"Error fetching surrounding SE players: {response.StatusCode}");
                return null;
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
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);
                var encodedEB = Uri.EscapeDataString(earningsBonus);

                var response = await _httpClient.GetAsync($"api/v1/majplayerrankings/surrounding/eb/{encodedPlayerName}?earningsBonus={encodedEB}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                    return result;
                }

                _logger.LogError($"Error fetching surrounding EB players: {response.StatusCode}");
                return null;
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
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);

                var response = await _httpClient.GetAsync($"api/v1/majplayerrankings/surrounding/mer/{encodedPlayerName}?mer={mer}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                    return result;
                }

                _logger.LogError($"Error fetching surrounding MER players: {response.StatusCode}");
                return null;
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
                // URL encode the playerName to handle spaces and special characters
                var encodedPlayerName = Uri.EscapeDataString(playerName);

                var response = await _httpClient.GetAsync($"api/v1/majplayerrankings/surrounding/jer/{encodedPlayerName}?jer={jer}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                    return result;
                }

                _logger.LogError($"Error fetching surrounding JER players: {response.StatusCode}");
                return null;
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
                var response = await _httpClient.GetAsync($"api/v1/majplayerrankings/latest-all?limit={limit}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<MajPlayerRankingDto>>();
                }

                _logger.LogError($"Error fetching latest major player rankings: {response.StatusCode}");
                return null;
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
                    var result = await response.Content.ReadFromJsonAsync<SaveRankingResponseDto>();
                    return result;
                }

                _logger.LogError($"Error saving major player ranking: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveMajPlayerRankingAsync");
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
