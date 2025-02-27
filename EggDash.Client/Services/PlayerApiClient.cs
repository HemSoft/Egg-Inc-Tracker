using System.Net.Http.Json;
using System.Text.Json;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EggDash.Client.Services;

public class PlayerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlayerApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public PlayerApiClient(HttpClient httpClient, ILogger<PlayerApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<PlayerApiClient>.Instance;
    }

    public async Task<PlayerDto?> GetLatestPlayerAsync(string playerName)
    {
        try
        {
            // First try to get the response as string to debug
            var response = await _httpClient.GetAsync($"api/v1/Players");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status code {StatusCode} for players request", response.StatusCode);
                return null;
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("API response for players: {Response}", responseContent);
            
            var players = JsonSerializer.Deserialize<List<PlayerDto>>(responseContent, _jsonOptions);
            return players?.FirstOrDefault(p => p.PlayerName == playerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching player {PlayerName}", playerName);
            return null;
        }
    }

    public async Task<PlayerStatsDto?> GetPlayerStatsAsync(string playerName, int daysBack = 30)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/Players/{playerName}/stats?daysBack={daysBack}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status code {StatusCode} for player stats request", response.StatusCode);
                return null;
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("API response for player stats: {Response}", responseContent);
            
            return JsonSerializer.Deserialize<PlayerStatsDto>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stats for player {PlayerName}", playerName);
            return null;
        }
    }

    public async Task<List<MajPlayerRankingDto>> GetMajPlayerRankingsAsync(int limit = 30)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/MajPlayerRankings?limit={limit}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status code {StatusCode} for rankings request", response.StatusCode);
                return new List<MajPlayerRankingDto>();
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("API response for rankings: {Response}", responseContent);
            
            return JsonSerializer.Deserialize<List<MajPlayerRankingDto>>(responseContent, _jsonOptions) 
                   ?? new List<MajPlayerRankingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maj player rankings");
            return new List<MajPlayerRankingDto>();
        }
    }

    public async Task<(MajPlayerRankingDto?, MajPlayerRankingDto?)> GetSurroundingSEPlayersAsync(string playerName, string soulEggs)
    {
        try
        {
            // Fetch response as string first to debug
            var response = await _httpClient.GetAsync($"api/v1/MajPlayerRankings/surroundingSE?playerName={Uri.EscapeDataString(playerName)}&soulEggs={Uri.EscapeDataString(soulEggs)}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status code {StatusCode} for surrounding SE request", response.StatusCode);
                return (null, null);
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API response for surrounding SE: {Response}", responseContent);
            
            try 
            {
                var result = JsonSerializer.Deserialize<List<MajPlayerRankingDto>>(responseContent, _jsonOptions);
                if (result?.Count >= 2)
                    return (result[0], result[1]);
                return (null, null);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing surrounding SE response: {Response}", responseContent);
                return (null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching surrounding SE players");
            return (null, null);
        }
    }

    // Update similar methods for other surrounding players with the same approach
    public async Task<(MajPlayerRankingDto?, MajPlayerRankingDto?)> GetSurroundingEBPlayersAsync(string playerName, string earningsBonus)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/MajPlayerRankings/surroundingEB?playerName={Uri.EscapeDataString(playerName)}&earningsBonus={Uri.EscapeDataString(earningsBonus)}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status code {StatusCode} for surrounding EB request", response.StatusCode);
                return (null, null);
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API response for surrounding EB: {Response}", responseContent);
            
            try 
            {
                var result = JsonSerializer.Deserialize<List<MajPlayerRankingDto>>(responseContent, _jsonOptions);
                if (result?.Count >= 2)
                    return (result[0], result[1]);
                return (null, null);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing surrounding EB response: {Response}", responseContent);
                return (null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching surrounding EB players");
            return (null, null);
        }
    }

    // Update the remaining methods with the same pattern
    public async Task<(MajPlayerRankingDto?, MajPlayerRankingDto?)> GetSurroundingMERPlayersAsync(string playerName, decimal mer)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<MajPlayerRankingDto>>($"api/v1/MajPlayerRankings/surroundingMER?playerName={playerName}&mer={mer}", _jsonOptions);
            if (response?.Count >= 2)
                return (response[0], response[1]);
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching surrounding MER players");
            return (null, null);
        }
    }

    public async Task<(MajPlayerRankingDto?, MajPlayerRankingDto?)> GetSurroundingJERPlayersAsync(string playerName, decimal jer)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<MajPlayerRankingDto>>($"api/v1/MajPlayerRankings/surroundingJER?playerName={playerName}&jer={jer}", _jsonOptions);
            if (response?.Count >= 2)
                return (response[0], response[1]);
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching surrounding JER players");
            return (null, null);
        }
    }
} 