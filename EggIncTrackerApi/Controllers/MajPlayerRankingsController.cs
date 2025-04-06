using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace EggIncTrackerApi.Controllers
{
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

                MajPlayerRankingDto lowerPlayer = null;
                MajPlayerRankingDto upperPlayer = null;
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

                MajPlayerRankingDto lowerPlayer = null;
                MajPlayerRankingDto upperPlayer = null;
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

                MajPlayerRankingDto lowerPlayer = null;
                MajPlayerRankingDto upperPlayer = null;
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

                MajPlayerRankingDto lowerPlayer = null;
                MajPlayerRankingDto upperPlayer = null;
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
        /// Get latest major player rankings
        /// </summary>
        [HttpGet("latest-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MajPlayerRankingDto>>> GetLatestMajPlayerRankings([FromQuery] int limit = 30)
        {
            try
            {
                // Use a direct SQL query to get the data from the stored procedure
                var rankings = new List<MajPlayerRankingDto>();

                // Create a SQL connection
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    // Create a command to execute the stored procedure
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "EXEC GetLatestMajPlayerRankingsByIGN @ShowSENumber = 1, @RowCount = @Limit";
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@Limit";
                        parameter.Value = limit * 2;
                        command.Parameters.Add(parameter);

                        // Execute the command and read the results
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var dto = new MajPlayerRankingDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Ranking = reader.GetInt32(reader.GetOrdinal("Ranking")),
                                    IGN = reader.GetString(reader.GetOrdinal("IGN")),
                                    DiscordName = reader.GetString(reader.GetOrdinal("DiscordName")),
                                    EBString = reader.GetString(reader.GetOrdinal("EBString")),
                                    Role = reader.GetString(reader.GetOrdinal("Role")),
                                    SENumber = reader.GetDecimal(reader.GetOrdinal("SENumber")),
                                    SEString = reader.GetString(reader.GetOrdinal("SEString")),
                                    SEGains = reader.IsDBNull(reader.GetOrdinal("SEGains")) ? null : reader.GetString(reader.GetOrdinal("SEGains")),
                                    PE = reader.GetInt32(reader.GetOrdinal("PE")),
                                    Prestiges = reader.IsDBNull(reader.GetOrdinal("Prestiges")) ? null : reader.GetString(reader.GetOrdinal("Prestiges")),
                                    MER = reader.GetDecimal(reader.GetOrdinal("MER")),
                                    JER = reader.GetDecimal(reader.GetOrdinal("JER")),
                                    Updated = reader.GetDateTime(reader.GetOrdinal("Updated"))
                                };

                                rankings.Add(dto);
                            }
                        }
                    }
                }

                return Ok(rankings.Take(limit).ToList());
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
        public MajPlayerRankingDto LowerPlayer { get; set; }
        public MajPlayerRankingDto UpperPlayer { get; set; }
    }

    /// <summary>
    /// DTO for save ranking response
    /// </summary>
    public class SaveRankingResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MajPlayerRankingDto PreviousRanking { get; set; }
    }
}
