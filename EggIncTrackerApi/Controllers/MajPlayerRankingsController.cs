using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
} 