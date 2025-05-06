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
            logger?.LogInformation("Getting latest major player rankings from database");

            using var context = new EggIncContext();

            // Get the most recent date (within the last 3 weeks)
            var threeWeeksAgo = DateTime.UtcNow.AddDays(-21);

            // Get all rankings from the last 3 weeks
            var rankings = await context.MajPlayerRankings
                .Where(r => r.Updated >= threeWeeksAgo)
                .OrderByDescending(r => r.Updated)
                .ToListAsync();

            // Group by player name and take the most recent record for each player
            var latestRankings = rankings
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .OrderByDescending(r => r.Updated) // Sort by newest first
                .ToList();

            logger?.LogInformation("Found {Count} latest major player rankings in database", latestRankings.Count);

            // Calculate SEGains for each player
            var playerGroups = rankings
                .GroupBy(r => r.IGN)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Updated).ToList());

            foreach (var ranking in latestRankings)
            {
                if (playerGroups.TryGetValue(ranking.IGN, out var playerRecords) && playerRecords.Count > 1)
                {
                    // Get the current and previous records
                    var currentRecord = playerRecords[0];
                    var previousRecord = playerRecords[1];

                    // Calculate the gain
                    var gain = currentRecord.SENumber - previousRecord.SENumber;

                    if (gain > 0)
                    {
                        // For scientific notation
                        if (currentRecord.SEString.EndsWith('s') && previousRecord.SEString.EndsWith('s'))
                        {
                            var currentValue = decimal.Parse(currentRecord.SEString.TrimEnd('s'));
                            var previousValue = decimal.Parse(previousRecord.SEString.TrimEnd('s'));
                            var diff = currentValue - previousValue;

                            if (diff > 0)
                            {
                                ranking.SEGains = $"{diff:0.00}s";
                            }
                            else
                            {
                                ranking.SEGains = "0";
                            }
                        }
                        else
                        {
                            ranking.SEGains = FormatSEGain(gain);
                        }
                    }
                    else
                    {
                        ranking.SEGains = "0";
                    }
                }
                else
                {
                    ranking.SEGains = "N/A";
                }
            }

            // Calculate weekly gains
            await CalculateWeeklyGains(latestRankings, context, logger);

            // Apply the limit if specified
            return limitTo > 0 && latestRankings.Count > limitTo
                ? [.. latestRankings.Take(limitTo)]
                : latestRankings;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting latest major player rankings from database");

            // Return an empty list in case of error
            return [];
        }
    }

    /// <summary>
    /// Format a Soul Egg gain value
    /// </summary>
    private static string FormatSEGain(decimal gain)
    {
        if (gain >= 1_000_000_000_000_000_000) // Quintillion
        {
            return $"{gain / 1_000_000_000_000_000_000:0.00}Q";
        }
        else if (gain >= 1_000_000_000_000_000) // Quadrillion
        {
            return $"{gain / 1_000_000_000_000_000:0.00}q";
        }
        else if (gain >= 1_000_000_000_000) // Trillion
        {
            return $"{gain / 1_000_000_000_000:0.00}T";
        }
        else if (gain >= 1_000_000_000) // Billion
        {
            return $"{gain / 1_000_000_000:0.00}B";
        }
        else if (gain >= 1_000_000) // Million
        {
            return $"{gain / 1_000_000:0.00}M";
        }
        else if (gain >= 1_000) // Thousand
        {
            return $"{gain / 1_000:0.00}K";
        }
        else
        {
            return gain.ToString("0.00");
        }
    }

    /// <summary>
    /// Calculate weekly gains for all players
    /// </summary>
    private static async Task CalculateWeeklyGains(List<MajPlayerRankingDto> rankings, EggIncContext context, ILogger? logger)
    {
        try
        {
            // Get the start of the week (Monday)
            var today = DateTime.Today;
            // Calculate Monday of the current week
            // In C#, DayOfWeek.Sunday = 0, DayOfWeek.Monday = 1, etc.
            // So for Monday, we need to subtract (DayOfWeek - 1) or 0 if it's Sunday
            int daysToSubtract = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
            var weekStartDate = today.AddDays(-daysToSubtract);
            logger?.LogInformation($"Calculating weekly gains from {weekStartDate} to {today}");

            // Create a dictionary to store the weekly gains for each player
            var weeklyGainsByPlayer = new Dictionary<string, string>();

            // Get unique player names from the rankings
            var uniquePlayerNames = rankings.Select(r => r.IGN).Distinct().ToList();
            logger?.LogInformation($"Processing weekly gains for {uniquePlayerNames.Count} unique players");

            // Get all records for all players in a single query to reduce database calls
            var allPlayerRecords = await context.MajPlayerRankings
                .Where(r => uniquePlayerNames.Contains(r.IGN) && r.Updated >= weekStartDate.AddDays(-7))
                .OrderBy(r => r.IGN)
                .ThenBy(r => r.Updated)
                .ToListAsync();

            logger?.LogInformation($"Retrieved {allPlayerRecords.Count} total records for all players");

            // Group records by player name
            var recordsByPlayer = allPlayerRecords.GroupBy(r => r.IGN).ToDictionary(g => g.Key, g => g.ToList());

            // Process each player
            foreach (var playerName in uniquePlayerNames)
            {
                try
                {
                    // Skip processing if we don't have records for this player
                    if (!recordsByPlayer.TryGetValue(playerName, out var playerRecords) || !playerRecords.Any())
                    {
                        weeklyGainsByPlayer[playerName] = "N/A";
                        continue;
                    }

                    // Find the record closest to but before the week start
                    var recordsBeforeWeek = playerRecords.Where(r => r.Updated < weekStartDate).ToList();
                    var recordsThisWeek = playerRecords.Where(r => r.Updated >= weekStartDate).ToList();

                    // Get the baseline record (record just before week start or earliest record of the week)
                    var baselineRecord = recordsBeforeWeek.Count > 0
                        ? recordsBeforeWeek.OrderByDescending(r => r.Updated).First()
                        : (recordsThisWeek.Count > 0 ? recordsThisWeek.OrderBy(r => r.Updated).First() : null);

                    // Get the latest record
                    var latestRecord = playerRecords.OrderByDescending(r => r.Updated).First();

                    if (baselineRecord != null)
                    {
                        // Calculate the weekly gain
                        if (latestRecord.SEString.EndsWith('s') && baselineRecord.SEString.EndsWith('s'))
                        {
                            // Scientific notation
                            var latestValue = decimal.Parse(latestRecord.SEString.TrimEnd('s'));
                            var baselineValue = decimal.Parse(baselineRecord.SEString.TrimEnd('s'));
                            var diff = latestValue - baselineValue;

                            if (diff > 0)
                            {
                                weeklyGainsByPlayer[playerName] = $"{diff:0.00}s";
                            }
                            else
                            {
                                weeklyGainsByPlayer[playerName] = "0";
                            }
                        }
                        else
                        {
                            // Regular notation
                            var weeklyGain = latestRecord.SENumber - baselineRecord.SENumber;

                            if (weeklyGain > 0)
                            {
                                weeklyGainsByPlayer[playerName] = FormatSEGain(weeklyGain);
                            }
                            else
                            {
                                weeklyGainsByPlayer[playerName] = "0";
                            }
                        }
                    }
                    else
                    {
                        weeklyGainsByPlayer[playerName] = "N/A";
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"Error calculating weekly gains for player {playerName}");
                    weeklyGainsByPlayer[playerName] = "Error";
                }
            }

            // Now update the SEGainsWeek property for each record in the rankings list
            foreach (var record in rankings)
            {
                if (weeklyGainsByPlayer.TryGetValue(record.IGN, out var weeklyGain))
                {
                    record.SEGainsWeek = weeklyGain;
                }
                else
                {
                    record.SEGainsWeek = "N/A";
                }
            }

            // Log a summary of the results
            logger?.LogInformation($"Weekly gains calculated for {weeklyGainsByPlayer.Count} players");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error calculating weekly gains");
            // Set all weekly gains to N/A in case of error
            foreach (var record in rankings)
            {
                record.SEGainsWeek = "N/A";
            }
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
