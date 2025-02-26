using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EggIncTrackerApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly EggIncContext _context;
        private readonly ILogger<PlayersController> _logger;

        public PlayersController(EggIncContext context, ILogger<PlayersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all players with their latest data
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
        {
            // Get distinct player names
            var playerNames = await _context.Players
                .Select(p => p.PlayerName)
                .Distinct()
                .ToListAsync();

            // For each name, get the most recent record
            var latestPlayers = new List<PlayerDto>();
            foreach (var name in playerNames)
            {
                var player = await _context.Players
                    .Where(p => p.PlayerName == name)
                    .OrderByDescending(p => p.Updated)
                    .FirstOrDefaultAsync();

                if (player != null)
                {
                    latestPlayers.Add(player);
                }
            }

            return Ok(latestPlayers);
        }

        /// <summary>
        /// Get player by name
        /// </summary>
        [HttpGet("{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlayerDto>> GetPlayer(string name)
        {
            var player = await _context.Players
                .Where(p => p.PlayerName == name)
                .OrderByDescending(p => p.Updated)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound($"Player with name {name} not found");
            }

            return Ok(player);
        }

        /// <summary>
        /// Get player history by name
        /// </summary>
        [HttpGet("{name}/history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<PlayerDto>>> GetPlayerHistory(
            string name, 
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int? limit = null)
        {
            var query = _context.Players
                .Where(p => p.PlayerName == name);

            if (from.HasValue)
            {
                query = query.Where(p => p.Updated >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(p => p.Updated <= to.Value);
            }

            query = query.OrderByDescending(p => p.Updated);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var playerHistory = await query.ToListAsync();

            if (playerHistory.Count == 0)
            {
                return NotFound($"No history found for player {name}");
            }

            return Ok(playerHistory);
        }

        /// <summary>
        /// Get player stats
        /// </summary>
        [HttpGet("{name}/stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlayerStatsDto>> GetPlayerStats(
            string name,
            [FromQuery, Range(1, 90)] int daysBack = 30)
        {
            try
            {
                var stats = await _context.Set<PlayerStatsDto>()
                    .FromSql($"EXEC GetRankedPlayerRecords @RecordLimit = 100, @SampleDaysBack = {daysBack}")
                    .ToListAsync();

                var playerStats = stats.FirstOrDefault(s => s.PlayerName == name);

                if (playerStats == null)
                {
                    return NotFound($"No stats found for player {name}");
                }

                return Ok(playerStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving player stats");
                return StatusCode(500, "An error occurred while retrieving player stats");
            }
        }
    }
} 