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

/// <summary>
/// DTO for extended surrounding players response with next 3 players in each direction
/// </summary>
public class ExtendedSurroundingPlayersDto
{
    public MajPlayerRankingDto? LowerPlayer { get; set; }
    public MajPlayerRankingDto? UpperPlayer { get; set; }
    public List<MajPlayerRankingDto> NextLowerPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public List<MajPlayerRankingDto> NextUpperPlayers { get; set; } = new List<MajPlayerRankingDto>();
}


public static class MajPlayerRankingManager
{
    // Static HttpClient for API calls
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _apiBaseUrl = "https://localhost:5000/api/v1";
    private const int DefaultNextPlayersCount = 3; // Default number of next players to retrieve

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

    public static async Task<ExtendedSurroundingPlayersDto?> GetExtendedSurroundingSEPlayersAsync(string playerName, string se, int nextPlayersCount = DefaultNextPlayersCount, ILogger? logger = null)
    {
        try
        {
            // Get all players first
            var allPlayers = await GetLatestMajPlayerRankingsAsync(0, logger);
            if (allPlayers == null || !allPlayers.Any())
            {
                return new ExtendedSurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower SE Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper SE Placeholder" }
                };
            }

            var playerSE = (decimal)BigNumberCalculator.ParseBigNumber(se);

            // Order by SE number
            var orderedPlayers = allPlayers
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .OrderByDescending(r => r.SENumber)
                .ToList();

            // Update rankings based on index in ordered list
            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                orderedPlayers[i].Ranking = i + 1; // 1-based ranking
            }

            // Find the player's position
            var playerIndex = orderedPlayers.FindIndex(p =>
                p.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase));

            // Get surrounding players
            var result = new ExtendedSurroundingPlayersDto();

            // Get immediate surrounding players
            var upperPlayers = orderedPlayers
                .Where(r => r.SENumber > playerSE && !r.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.SENumber)
                .ToList();

            var lowerPlayers = orderedPlayers
                .Where(r => r.SENumber < playerSE && !r.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.SENumber)
                .ToList();

            result.UpperPlayer = upperPlayers.FirstOrDefault();
            result.LowerPlayer = lowerPlayers.FirstOrDefault();

            // Get next players
            result.NextUpperPlayers = upperPlayers.Skip(1).Take(nextPlayersCount).ToList();
            result.NextLowerPlayers = lowerPlayers.Skip(1).Take(nextPlayersCount).ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting extended surrounding SE players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new ExtendedSurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower SE Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper SE Placeholder" }
            };
        }
    }

    public static async Task<ExtendedSurroundingPlayersDto?> GetExtendedSurroundingEBPlayersAsync(string playerName, string eb, int nextPlayersCount = DefaultNextPlayersCount, ILogger? logger = null)
    {
        try
        {
            // Get all players first
            var allPlayers = await GetLatestMajPlayerRankingsAsync(0, logger);
            if (allPlayers == null || !allPlayers.Any())
            {
                return new ExtendedSurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower EB Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper EB Placeholder" }
                };
            }

            var playerEB = BigNumberCalculator.ParseBigNumber(eb.TrimEnd('%'));

            // Transform to include parsed EB for sorting
            var playersWithEB = allPlayers
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .Select(r => new
                {
                    Player = r,
                    EBValue = BigNumberCalculator.ParseBigNumber(r.EBString.TrimEnd('%'))
                })
                .ToList();

            // Order by EB
            var orderedPlayers = playersWithEB
                .OrderByDescending(r => r.EBValue)
                .ToList();

            // Update rankings based on index in ordered list
            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                orderedPlayers[i].Player.Ranking = i + 1; // 1-based ranking
            }

            // Get surrounding players
            var result = new ExtendedSurroundingPlayersDto();

            // Get immediate surrounding players
            var upperPlayers = orderedPlayers
                .Where(r => r.EBValue > playerEB && !r.Player.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.EBValue)
                .Select(r => r.Player)
                .ToList();

            var lowerPlayers = orderedPlayers
                .Where(r => r.EBValue < playerEB && !r.Player.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.EBValue)
                .Select(r => r.Player)
                .ToList();

            result.UpperPlayer = upperPlayers.FirstOrDefault();
            result.LowerPlayer = lowerPlayers.FirstOrDefault();

            // Get next players
            result.NextUpperPlayers = upperPlayers.Skip(1).Take(nextPlayersCount).ToList();
            result.NextLowerPlayers = lowerPlayers.Skip(1).Take(nextPlayersCount).ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting extended surrounding EB players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new ExtendedSurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower EB Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper EB Placeholder" }
            };
        }
    }

    public static async Task<ExtendedSurroundingPlayersDto?> GetExtendedSurroundingMERPlayersAsync(string playerName, decimal mer, int nextPlayersCount = DefaultNextPlayersCount, ILogger? logger = null)
    {
        try
        {
            // Get all players first
            var allPlayers = await GetLatestMajPlayerRankingsAsync(0, logger);
            if (allPlayers == null || !allPlayers.Any())
            {
                return new ExtendedSurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower MER Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper MER Placeholder" }
                };
            }

            // Order by MER
            var orderedPlayers = allPlayers
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .OrderByDescending(r => r.MER)
                .ToList();

            // Update rankings based on index in ordered list
            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                orderedPlayers[i].Ranking = i + 1; // 1-based ranking
            }

            // Get surrounding players
            var result = new ExtendedSurroundingPlayersDto();

            // Get immediate surrounding players
            var upperPlayers = orderedPlayers
                .Where(r => r.MER > mer && !r.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.MER)
                .ToList();

            var lowerPlayers = orderedPlayers
                .Where(r => r.MER < mer && !r.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.MER)
                .ToList();

            result.UpperPlayer = upperPlayers.FirstOrDefault();
            result.LowerPlayer = lowerPlayers.FirstOrDefault();

            // Get next players
            result.NextUpperPlayers = upperPlayers.Skip(1).Take(nextPlayersCount).ToList();
            result.NextLowerPlayers = lowerPlayers.Skip(1).Take(nextPlayersCount).ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting extended surrounding MER players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new ExtendedSurroundingPlayersDto
            {
                LowerPlayer = new MajPlayerRankingDto { IGN = "Lower MER Placeholder" },
                UpperPlayer = new MajPlayerRankingDto { IGN = "Upper MER Placeholder" }
            };
        }
    }

    public static async Task<ExtendedSurroundingPlayersDto?> GetExtendedSurroundingJERPlayersAsync(string playerName, decimal jer, int nextPlayersCount = DefaultNextPlayersCount, ILogger? logger = null)
    {
        try
        {
            // Get all players first
            var allPlayers = await GetLatestMajPlayerRankingsAsync(0, logger);
            if (allPlayers == null || !allPlayers.Any())
            {
                return new ExtendedSurroundingPlayersDto
                {
                    LowerPlayer = new MajPlayerRankingDto { IGN = "Lower JER Placeholder" },
                    UpperPlayer = new MajPlayerRankingDto { IGN = "Upper JER Placeholder" }
                };
            }

            // Order by JER
            var orderedPlayers = allPlayers
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .OrderByDescending(r => r.JER)
                .ToList();

            // Update rankings based on index in ordered list
            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                orderedPlayers[i].Ranking = i + 1; // 1-based ranking
            }

            // Get surrounding players
            var result = new ExtendedSurroundingPlayersDto();

            // Get immediate surrounding players
            var upperPlayers = orderedPlayers
                .Where(r => r.JER > jer && !r.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.JER)
                .ToList();

            var lowerPlayers = orderedPlayers
                .Where(r => r.JER < jer && !r.IGN.Trim().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.JER)
                .ToList();

            result.UpperPlayer = upperPlayers.FirstOrDefault();
            result.LowerPlayer = lowerPlayers.FirstOrDefault();

            // Get next players
            result.NextUpperPlayers = upperPlayers.Skip(1).Take(nextPlayersCount).ToList();
            result.NextLowerPlayers = lowerPlayers.Skip(1).Take(nextPlayersCount).ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting extended surrounding JER players for {PlayerName}", playerName);

            // Return a default response in case of error
            return new ExtendedSurroundingPlayersDto
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
