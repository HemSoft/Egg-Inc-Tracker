namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;
using HemSoft.EggIncTracker.Data; // Added for EggIncContext
using Microsoft.EntityFrameworkCore; // Added for EF Core methods
using System.Numerics; // Added for BigInteger

// Removed IApiService interface and related DTOs as they are no longer needed here

/// <summary>
/// DTO for surrounding players response (Moved from previous version)
/// </summary>
public class SurroundingPlayersDto
{
    public MajPlayerRankingDto? LowerPlayer { get; set; } // Made nullable
    public MajPlayerRankingDto? UpperPlayer { get; set; } // Made nullable
}


public static class MajPlayerRankingManager
{
    // Static HttpClient for API calls
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _apiBaseUrl = "https://localhost:5000/api/v1"; // Default to localhost

    public static async Task<SurroundingPlayersDto?> GetSurroundingEBPlayersAsync(string playerName, string eb, ILogger? logger)
    {
        try
        {
            logger?.LogInformation("Calling API to get surrounding EB players for {PlayerName} with EB {EB}", playerName, eb);

            // Encode the player name for URL
            var encodedPlayerName = Uri.EscapeDataString(playerName);
            var encodedEB = Uri.EscapeDataString(eb);

            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings/surrounding/eb/{encodedPlayerName}?earningsBonus={encodedEB}";

            // Make the API call
            var response = await _httpClient.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                logger?.LogInformation("Successfully retrieved surrounding EB players for {PlayerName}", playerName);
                return result;
            }
            else
            {
                logger?.LogWarning("API call failed with status code {StatusCode}: {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                // Return a default response
                return new SurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower EB Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper EB Placeholder" }
                };
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting surrounding EB players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new SurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower EB Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper EB Placeholder" }
            };
        }
    }

    public static async Task<SurroundingPlayersDto?> GetSurroundingSEPlayersAsync(string playerName, string se, ILogger? logger)
    {
        try
        {
            logger?.LogInformation("Calling API to get surrounding SE players for {PlayerName} with SE {SE}", playerName, se);

            // Encode the player name for URL
            var encodedPlayerName = Uri.EscapeDataString(playerName);
            var encodedSE = Uri.EscapeDataString(se);

            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings/surrounding/se/{encodedPlayerName}?soulEggs={encodedSE}";

            // Make the API call
            var response = await _httpClient.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                logger?.LogInformation("Successfully retrieved surrounding SE players for {PlayerName}", playerName);
                return result;
            }
            else
            {
                logger?.LogWarning("API call failed with status code {StatusCode}: {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                // Return a default response
                return new SurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower SE Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper SE Placeholder" }
                };
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting surrounding SE players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new SurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower SE Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper SE Placeholder" }
            };
        }
    }

    public static async Task<SurroundingPlayersDto?> GetSurroundingMERPlayersAsync(string playerName, decimal mer, ILogger? logger)
    {
        try
        {
            logger?.LogInformation("Calling API to get surrounding MER players for {PlayerName} with MER {MER}", playerName, mer);

            // Encode the player name for URL
            var encodedPlayerName = Uri.EscapeDataString(playerName);

            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings/surrounding/mer/{encodedPlayerName}?mer={mer}";

            // Make the API call
            var response = await _httpClient.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                logger?.LogInformation("Successfully retrieved surrounding MER players for {PlayerName}", playerName);
                return result;
            }
            else
            {
                logger?.LogWarning("API call failed with status code {StatusCode}: {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                // Return a default response
                return new SurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower MER Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper MER Placeholder" }
                };
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting surrounding MER players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new SurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower MER Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper MER Placeholder" }
            };
        }
    }

    public static async Task<SurroundingPlayersDto?> GetSurroundingJERPlayersAsync(string playerName, decimal jer, ILogger? logger)
    {
        try
        {
            logger?.LogInformation("Calling API to get surrounding JER players for {PlayerName} with JER {JER}", playerName, jer);

            // Encode the player name for URL
            var encodedPlayerName = Uri.EscapeDataString(playerName);

            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings/surrounding/jer/{encodedPlayerName}?jer={jer}";

            // Make the API call
            var response = await _httpClient.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<SurroundingPlayersDto>();
                logger?.LogInformation("Successfully retrieved surrounding JER players for {PlayerName}", playerName);
                return result;
            }
            else
            {
                logger?.LogWarning("API call failed with status code {StatusCode}: {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                // Return a default response
                return new SurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower JER Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper JER Placeholder" }
                };
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting surrounding JER players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new SurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower JER Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper JER Placeholder" }
            };
        }
    }

    public static async Task<List<MajPlayerRankingDto>?> GetLatestMajPlayerRankingsAsync(int limitTo = 30, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Calling API to get latest major player rankings with limit {LimitTo}", limitTo);

            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings/latest-all";

            // Make the API call
            var response = await _httpClient.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<List<MajPlayerRankingDto>>();
                logger?.LogInformation("Successfully retrieved {Count} latest major player rankings", result?.Count ?? 0);

                // Apply the limit if specified
                return result != null && limitTo > 0 && result.Count > limitTo
                    ? [.. result.Take(limitTo)]
                    : result;
            }
            else
            {
                logger?.LogWarning("API call failed with status code {StatusCode}: {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                // Return an empty list in case of error
                return [];
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting latest major player rankings");

            // Return an empty list in case of error
            return [];
        }
    }

    public static async Task<(bool Success, MajPlayerRankingDto? PreviousRanking)> SaveMajPlayerRankingAsync(MajPlayerRankingDto majPlayerRanking, ILogger? logger)
    {
        try
        {
            logger?.LogInformation("Calling API to save major player ranking for {PlayerName}", majPlayerRanking.IGN);

            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings";

            // Make the API call
            var response = await _httpClient.PostAsJsonAsync(apiUrl, majPlayerRanking);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<SaveRankingResponseDto>();
                logger?.LogInformation("Successfully saved major player ranking for {PlayerName}: {Message}",
                    majPlayerRanking.IGN, result?.Message);

                return (true, result?.PreviousRanking);
            }
            else
            {
                logger?.LogWarning("API call failed with status code {StatusCode}: {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                // Return failure in case of error
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error saving major player ranking for {PlayerName}", majPlayerRanking.IGN);

            // Return failure in case of error
            return (false, null);
        }
    }
}

// Removed ServiceLocator as it's an anti-pattern and not needed with static managers

/// <summary>
/// DTO for save ranking response
/// </summary>
public class SaveRankingResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MajPlayerRankingDto? PreviousRanking { get; set; }
}
