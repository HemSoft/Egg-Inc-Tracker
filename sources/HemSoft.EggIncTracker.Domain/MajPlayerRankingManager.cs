namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;
using HemSoft.EggIncTracker.Data;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

/// <summary>
/// DTO for surrounding players response (Moved from previous version)
/// </summary>
public class SurroundingPlayersDto
{
    public MajPlayerRankingDto? LowerPlayer { get; set; }
    public MajPlayerRankingDto? UpperPlayer { get; set; }
}


public static class MajPlayerRankingManager
{
    // Static HttpClient for API calls
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _apiBaseUrl = "https://localhost:5000/api/v1";

    public static async Task<SurroundingPlayersDto?> GetSurroundingEBPlayersAsync(string playerName, string eb, ILogger? logger)
    {
        try
        {
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
            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings/latest-all";

            // Make the API call
            var response = await _httpClient.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<List<MajPlayerRankingDto>>();

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
            // Build the API URL
            var apiUrl = $"{_apiBaseUrl}/MajPlayerRankings";

            // Make the API call
            var response = await _httpClient.PostAsJsonAsync(apiUrl, majPlayerRanking);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                var result = await response.Content.ReadFromJsonAsync<SaveRankingResponseDto>();
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

/// <summary>
/// DTO for save ranking response
/// </summary>
public class SaveRankingResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MajPlayerRankingDto? PreviousRanking { get; set; }
}
