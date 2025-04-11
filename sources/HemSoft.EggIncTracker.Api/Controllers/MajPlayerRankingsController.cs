namespace HemSoft.EggIncTracker.Api.Controllers;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MajPlayerRankingsController : ControllerBase
{
    private readonly EggIncContext _context;
    private readonly ILogger<MajPlayerRankingsController> _logger;

    public MajPlayerRankingsController(EggIncContext context, ILogger<MajPlayerRankingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all player rankings
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MajPlayerRankingDto>>> GetAllRankings([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var query = _context.MajPlayerRankings.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(r => r.Updated >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.Updated <= to.Value);
        }

        query = query.OrderByDescending(r => r.Updated)
                     .ThenBy(r => r.Ranking);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Get the latest rankings snapshot
    /// </summary>
    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MajPlayerRankingDto>>> GetLatestRankings()
    {
        // Get the most recent date
        var latestDate = await _context.MajPlayerRankings
            .OrderByDescending(r => r.Updated)
            .Select(r => r.Updated.Date) // Using Date to get just the date part without time
            .FirstOrDefaultAsync();

        if (latestDate == default)
        {
            return Ok(new List<MajPlayerRankingDto>());
        }

        // Get all rankings from that date
        var latestRankings = await _context.MajPlayerRankings
            .Where(r => r.Updated.Date == latestDate)
            .OrderBy(r => r.Ranking)
            .ToListAsync();

        return Ok(latestRankings);
    }

    /// <summary>
    /// Get rankings for a specific player
    /// </summary>
    [HttpGet("player/{playerName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MajPlayerRankingDto>>> GetPlayerRankings(
        string playerName,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? limit = null)
    {
        var query = _context.MajPlayerRankings
            .Where(r => r.IGN == playerName);

        if (from.HasValue)
        {
            query = query.Where(r => r.Updated >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.Updated <= to.Value);
        }

        query = query.OrderByDescending(r => r.Updated);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var rankings = await query.ToListAsync();

        if (rankings.Count == 0)
        {
            return NotFound($"No rankings found for player {playerName}");
        }

        return Ok(rankings);
    }

    /// <summary>
    /// Get a player's latest ranking
    /// </summary>
    [HttpGet("player/{playerName}/latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MajPlayerRankingDto>> GetPlayerLatestRanking(string playerName)
    {
        var latestRanking = await _context.MajPlayerRankings
            .Where(r => r.IGN == playerName)
            .OrderByDescending(r => r.Updated)
            .FirstOrDefaultAsync();

        if (latestRanking == null)
        {
            return NotFound($"No rankings found for player {playerName}");
        }

        return Ok(latestRanking);
    }

    /// <summary>
    /// Get ranking history by rank position
    /// </summary>
    [HttpGet("position/{ranking:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MajPlayerRankingDto>>> GetRankHistory(
        int ranking,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = _context.MajPlayerRankings
            .Where(r => r.Ranking == ranking);

        if (from.HasValue)
        {
            query = query.Where(r => r.Updated >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.Updated <= to.Value);
        }

        var rankings = await query
            .OrderByDescending(r => r.Updated)
            .ToListAsync();

        return Ok(rankings);
    }

    /// <summary>
    /// Get players surrounding a player based on Soul Eggs
    /// </summary>
    [HttpGet("surrounding/se/{playerName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurroundingPlayersDto>> GetSurroundingSEPlayers(string playerName, [FromQuery] string soulEggs)
    {
        try
        {
            var playerSE = (decimal)HemSoft.EggIncTracker.Domain.BigNumberCalculator.ParseBigNumber(soulEggs);

            // Get latest ranking for each player
            var latestRankings = await _context.MajPlayerRankings
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .ToListAsync();

            // Order by SE number and find surrounding players
            var orderedRankings = latestRankings
                .OrderByDescending(r => r.SENumber)
                .ToList();

            MajPlayerRankingDto? lowerPlayer = null;
            MajPlayerRankingDto? upperPlayer = null;
            int rank = 0;

            foreach (var ranking in orderedRankings)
            {
                rank++;
                ranking.Ranking = rank; // Update the ranking

                if (ranking.SENumber < playerSE && ranking.IGN != playerName)
                {
                    lowerPlayer = ranking;
                    break;
                }

                if (ranking.SENumber > playerSE && ranking.IGN != playerName)
                {
                    upperPlayer = ranking;
                }
            }

            return Ok(new SurroundingPlayersDto
            {
                LowerPlayer = lowerPlayer,
                UpperPlayer = upperPlayer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetSurroundingSEPlayers for player {playerName} with SE {soulEggs}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get players surrounding a player based on Earnings Bonus
    /// </summary>
    [HttpGet("surrounding/eb/{playerName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurroundingPlayersDto>> GetSurroundingEBPlayers(string playerName, [FromQuery] string earningsBonus)
    {
        try
        {
            var playerEB = HemSoft.EggIncTracker.Domain.BigNumberCalculator.ParseBigNumber(earningsBonus);

            // Get latest ranking for each player
            var latestRankings = await _context.MajPlayerRankings
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .ToListAsync();

            // Transform to include parsed EB for sorting
            var rankingsWithEB = latestRankings
                .Select(r => new
                {
                    Ranking = r,
                    EBValue = HemSoft.EggIncTracker.Domain.BigNumberCalculator.ParseBigNumber(r.EBString.TrimEnd('%'))
                })
                .OrderByDescending(x => x.EBValue)
                .ToList();

            MajPlayerRankingDto? lowerPlayer = null;
            MajPlayerRankingDto? upperPlayer = null;
            int rank = 0;

            foreach (var item in rankingsWithEB)
            {
                rank++;
                item.Ranking.Ranking = rank; // Update the ranking

                if (playerEB > item.EBValue && item.Ranking.IGN != playerName)
                {
                    lowerPlayer = item.Ranking;
                    break;
                }
                else if (item.EBValue >= playerEB && item.Ranking.IGN != playerName)
                {
                    upperPlayer = item.Ranking;
                }
            }

            return Ok(new SurroundingPlayersDto
            {
                LowerPlayer = lowerPlayer,
                UpperPlayer = upperPlayer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetSurroundingEBPlayers for player {playerName} with EB {earningsBonus}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get players surrounding a player based on MER
    /// </summary>
    [HttpGet("surrounding/mer/{playerName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurroundingPlayersDto>> GetSurroundingMERPlayers(string playerName, [FromQuery] decimal mer)
    {
        try
        {
            // Get latest ranking for each player
            var latestRankings = await _context.MajPlayerRankings
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .ToListAsync();

            // Order by MER
            var orderedRankings = latestRankings
                .OrderByDescending(r => r.MER)
                .ToList();

            MajPlayerRankingDto? lowerPlayer = null;
            MajPlayerRankingDto? upperPlayer = null;
            int rank = 0;

            foreach (var ranking in orderedRankings)
            {
                rank++;
                ranking.Ranking = rank; // Update the ranking

                if (mer > ranking.MER && ranking.IGN != playerName)
                {
                    lowerPlayer = ranking;
                    break;
                }
                else if (ranking.MER >= mer && ranking.IGN != playerName)
                {
                    upperPlayer = ranking;
                }
            }

            return Ok(new SurroundingPlayersDto
            {
                LowerPlayer = lowerPlayer,
                UpperPlayer = upperPlayer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetSurroundingMERPlayers for player {playerName} with MER {mer}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get players surrounding a player based on JER
    /// </summary>
    [HttpGet("surrounding/jer/{playerName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurroundingPlayersDto>> GetSurroundingJERPlayers(string playerName, [FromQuery] decimal jer)
    {
        try
        {
            // Get latest ranking for each player
            var latestRankings = await _context.MajPlayerRankings
                .GroupBy(r => r.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First())
                .ToListAsync();

            // Order by JER
            var orderedRankings = latestRankings
                .OrderByDescending(r => r.JER)
                .ToList();

            MajPlayerRankingDto? lowerPlayer = null;
            MajPlayerRankingDto? upperPlayer = null;
            int rank = 0;

            foreach (var ranking in orderedRankings)
            {
                rank++;
                ranking.Ranking = rank; // Update the ranking

                if (jer > ranking.JER && ranking.IGN != playerName)
                {
                    lowerPlayer = ranking;
                    break;
                }
                else if (ranking.JER >= jer && ranking.IGN != playerName)
                {
                    upperPlayer = ranking;
                }
            }

            return Ok(new SurroundingPlayersDto
            {
                LowerPlayer = lowerPlayer,
                UpperPlayer = upperPlayer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GetSurroundingJERPlayers for player {playerName} with JER {jer}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Calculate weekly gains for all players
    /// </summary>
    private async Task CalculateWeeklyGains(List<MajPlayerRankingDto> rankings)
    {
        // Get the start of the week (Monday)
        var today = DateTime.Today;
        // Calculate Monday of the current week
        // In C#, DayOfWeek.Sunday = 0, DayOfWeek.Monday = 1, etc.
        // So for Monday, we need to subtract (DayOfWeek - 1) or 0 if it's Sunday
        int daysToSubtract = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
        var weekStartDate = today.AddDays(-daysToSubtract);
        _logger.LogInformation($"Calculating weekly gains from {weekStartDate} to {today}");

        // Create a dictionary to store the weekly gains for each player
        var weeklyGainsByPlayer = new Dictionary<string, string>();

        // Get unique player names from the rankings
        var uniquePlayerNames = rankings.Select(r => r.IGN).Distinct().ToList();
        _logger.LogInformation($"Processing weekly gains for {uniquePlayerNames.Count} unique players");

        // Get all records for all players in a single query to reduce database calls
        var allPlayerRecords = await _context.MajPlayerRankings
            .Where(r => uniquePlayerNames.Contains(r.IGN) && r.Updated >= weekStartDate.AddDays(-7))
            .OrderBy(r => r.IGN)
            .ThenBy(r => r.Updated)
            .ToListAsync();

        _logger.LogInformation($"Retrieved {allPlayerRecords.Count} total records for all players");

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
                _logger.LogError(ex, $"Error calculating weekly gains for player {playerName}");
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
        _logger.LogInformation($"Weekly gains calculated for {weeklyGainsByPlayer.Count} players");
    }

    /// <summary>
    /// Format SE gain value to match the game's notation
    /// </summary>
    private string FormatSEGain(decimal gain)
    {
        // Simple formatting based on magnitude
        if (gain == 0)
        {
            return "0";
        }
        else if (gain < 1000)
        {
            return gain.ToString("0.00");
        }
        else if (gain < 1000000)
        {
            return (gain / 1000).ToString("0.00") + "K";
        }
        else if (gain < 1000000000)
        {
            return (gain / 1000000).ToString("0.00") + "M";
        }
        else if (gain < 1000000000000)
        {
            return (gain / 1000000000).ToString("0.00") + "B";
        }
        else if (gain < 1000000000000000)
        {
            return (gain / 1000000000000).ToString("0.00") + "t";
        }
        else if (gain < 1000000000000000000)
        {
            return (gain / 1000000000000000).ToString("0.00") + "q";
        }
        else if (gain < 1000000000000000000000m)
        {
            return (gain / 1000000000000000000).ToString("0.00") + "Q";
        }
        else
        {
            // For extremely large numbers, use scientific notation
            return $"{gain / 1000000000000000000000m:0.00}s";
        }
    }

    /// <summary>
    /// Get latest major player rankings
    /// </summary>
    [HttpGet("latest-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MajPlayerRankingDto>>> GetLatestMajPlayerRankings()
    {
        try
        {
            // Use Entity Framework to get the data directly from the database
            // Get the most recent 1000 records
            var rankings = await _context.MajPlayerRankings
                .OrderByDescending(r => r.Updated)
                .Take(1000)
                .Select(r => new MajPlayerRankingDto
                {
                    Id = r.Id,
                    Ranking = r.Ranking,
                    IGN = r.IGN,
                    DiscordName = r.DiscordName,
                    EBString = r.EBString,
                    Role = r.Role,
                    SENumber = r.SENumber,
                    SEString = r.SEString,
                    PE = r.PE,
                    Prestiges = r.Prestiges,
                    MER = r.MER,
                    JER = r.JER,
                    Updated = r.Updated,
                    // Initialize these fields, they will be calculated later
                    SEGains = "0",
                    SEGainsWeek = "0"
                })
                .ToListAsync();

            // Calculate SEGains for each player
            var playerGroups = rankings.GroupBy(r => r.IGN).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Updated).ToList());

            foreach (var group in playerGroups)
            {
                var playerRecords = group.Value;
                if (playerRecords.Count > 1)
                {
                    // Calculate SEGains for each record except the oldest one
                    for (int i = 0; i < playerRecords.Count - 1; i++)
                    {
                        var currentRecord = playerRecords[i];
                        var previousRecord = playerRecords[i + 1];

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
                                    currentRecord.SEGains = $"{diff:0.00}s";
                                }
                            }
                            else
                            {
                                currentRecord.SEGains = FormatSEGain(gain);
                            }
                        }
                    }
                }
            }

            // Let's directly calculate the weekly gains for King Friday as a test case
            var kingFridayRecords = await _context.MajPlayerRankings
                .Where(r => r.IGN == "King Friday")
                .OrderBy(r => r.Updated)
                .ToListAsync();

            if (kingFridayRecords.Count > 0)
            {
                // Get the start of the week (Monday)
                var today = DateTime.Today;
                // Calculate Monday of the current week
                int daysToSubtract = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
                var weekStartDate = today.AddDays(-daysToSubtract);
                _logger.LogInformation($"Week start date: {weekStartDate}");

                // Find the record closest to but before the week start
                var recordsBeforeWeek = kingFridayRecords.Where(r => r.Updated < weekStartDate).ToList();
                var baselineRecord = recordsBeforeWeek.Count > 0
                    ? recordsBeforeWeek.OrderByDescending(r => r.Updated).First()
                    : null;

                // Find the latest record
                var latestRecord = kingFridayRecords.OrderByDescending(r => r.Updated).First();

                if (baselineRecord != null)
                {
                    _logger.LogInformation($"King Friday baseline: {baselineRecord.Updated}, SE: {baselineRecord.SEString}");
                    _logger.LogInformation($"King Friday latest: {latestRecord.Updated}, SE: {latestRecord.SEString}");

                    // Calculate the weekly gain
                    if (latestRecord.SEString.EndsWith('s') && baselineRecord.SEString.EndsWith('s'))
                    {
                        // Scientific notation
                        var latestValue = decimal.Parse(latestRecord.SEString.TrimEnd('s'));
                        var baselineValue = decimal.Parse(baselineRecord.SEString.TrimEnd('s'));
                        var diff = latestValue - baselineValue;

                        _logger.LogInformation($"King Friday calculation: {latestValue} - {baselineValue} = {diff}");

                        // Find King Friday in the rankings and update
                        var kfInRankings = rankings.FirstOrDefault(r => r.IGN == "King Friday");
                        if (kfInRankings != null && diff > 0)
                        {
                            kfInRankings.SEGainsWeek = $"{diff:0.00}s";
                            _logger.LogInformation($"Set King Friday weekly gain to {kfInRankings.SEGainsWeek}");
                        }
                    }
                }
            }

            // Calculate weekly gains for all players in a single batch
            await CalculateWeeklyGains(rankings);

            // Debug: Log a sample of the weekly gains (just a few players)
            var samplePlayers = rankings.GroupBy(r => r.IGN).Select(g => g.First()).Take(3).ToList();
            foreach (var player in samplePlayers)
            {
                _logger.LogInformation($"Sample weekly gain for {player.IGN}: {player.SEGainsWeek}");
            }

            // Return all rankings but ensure we have a reasonable number of records
            // Group by player name and take the most recent records for each player
            var groupedRankings = rankings
                .GroupBy(r => r.IGN)
                .SelectMany(g => g.OrderByDescending(r => r.Updated).Take(10))
                .OrderByDescending(r => r.Updated)
                .ToList();

            _logger.LogInformation($"Returning {groupedRankings.Count} records after grouping by player");
            return Ok(groupedRankings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest maj player rankings");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Save a major player ranking
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveRankingResponseDto>> SaveMajPlayerRanking([FromBody] MajPlayerRankingDto majPlayerRanking)
    {
        try
        {
            if (majPlayerRanking == null)
            {
                return BadRequest("MajPlayerRanking data is required");
            }

            var getLatestEntry = await _context.MajPlayerRankings
                .Where(x => x.IGN == majPlayerRanking.IGN && x.DiscordName == majPlayerRanking.DiscordName)
                .OrderByDescending(z => z.Updated)
                .FirstOrDefaultAsync();

            if (getLatestEntry != null)
            {
                // This check to avoid Mordeekakh having two entries with 28 PE difference.
                if (getLatestEntry.PE - majPlayerRanking.PE > 5)
                {
                    return Ok(new SaveRankingResponseDto
                    {
                        Success = false,
                        Message = "PE difference too large"
                    });
                }

                bool changed = false;
                string message = string.Empty;

                if (getLatestEntry.SEString != majPlayerRanking.SEString)
                {
                    message = $"SE increased for {getLatestEntry.IGN} {getLatestEntry.SEString} to {majPlayerRanking.SEString}";
                    changed = true;
                }
                else if (getLatestEntry.PE != majPlayerRanking.PE)
                {
                    message = $"PE increased for {getLatestEntry.IGN} {getLatestEntry.PE} to {majPlayerRanking.PE}";
                    changed = true;
                }
                else if (getLatestEntry.EBString != majPlayerRanking.EBString)
                {
                    message = $"EB increased for {getLatestEntry.IGN} {getLatestEntry.EBString} to {majPlayerRanking.EBString}";
                    changed = true;
                }

                if (changed)
                {
                    _context.MajPlayerRankings.Add(majPlayerRanking);
                    await _context.SaveChangesAsync();

                    return Ok(new SaveRankingResponseDto
                    {
                        Success = true,
                        Message = message,
                        PreviousRanking = getLatestEntry
                    });
                }

                return Ok(new SaveRankingResponseDto
                {
                    Success = false,
                    Message = "No changes detected"
                });
            }

            // First entry for this player
            _context.MajPlayerRankings.Add(majPlayerRanking);
            await _context.SaveChangesAsync();

            return Ok(new SaveRankingResponseDto
            {
                Success = true,
                Message = $"Saved first entry for {majPlayerRanking.IGN}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving maj player ranking");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}

/// <summary>
/// DTO for surrounding players response
/// </summary>
public class SurroundingPlayersDto
{
    public MajPlayerRankingDto? LowerPlayer { get; set; }
    public MajPlayerRankingDto? UpperPlayer { get; set; }
}

/// <summary>
/// DTO for save ranking response
/// </summary>
public class SaveRankingResponseDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public MajPlayerRankingDto? PreviousRanking { get; set; }
}
