using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace EggIncTrackerApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PlayersController(EggIncContext context, ILogger<PlayersController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all players with their latest data
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
        {
            // Get distinct player names
            var playerNames = await context.Players
                .Select(p => p.PlayerName)
                .Distinct()
                .ToListAsync();

            // For each name, get the most recent record
            var latestPlayers = new List<PlayerDto>();
            foreach (var name in playerNames)
            {
                var player = await context.Players
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
            var player = await context.Players
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
            var query = context.Players
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
                var stats = await context.Set<PlayerStatsDto>()
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
                logger.LogError(ex, "Error retrieving player stats");
                return StatusCode(500, "An error occurred while retrieving player stats");
            }
        }

        /// <summary>
        /// Get latest player data by name
        /// </summary>
        [HttpGet("latest/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlayerDto>> GetLatestPlayer(string name)
        {
            var player = await context.Players
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
        /// Get player by EID
        /// </summary>
        [HttpGet("eid/{eid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlayerDto>> GetPlayerByEID(string eid)
        {
            var player = await context.Players
                .Where(p => p.EID == eid)
                .OrderByDescending(p => p.Updated)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound($"Player with EID {eid} not found");
            }

            return Ok(player);
        }

        /// <summary>
        /// Get ranked player records
        /// </summary>
        [HttpGet("{name}/ranked")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlayerStatsDto>> GetRankedPlayers(
            string name,
            [FromQuery, Range(1, 100)] int recordLimit = 1,
            [FromQuery, Range(1, 90)] int sampleDaysBack = 30)
        {
            try
            {
                var rankings = await context.Set<PlayerStatsDto>()
                    .FromSql($"EXEC GetRankedPlayerRecords @RecordLimit = {recordLimit}, @SampleDaysBack = {sampleDaysBack}")
                    .ToListAsync();

                var playerStats = rankings.FirstOrDefault(x => x.PlayerName == name);

                if (playerStats == null)
                {
                    return NotFound($"No ranked data found for player {name}");
                }

                logger.LogInformation("Successfully retrieved player rankings for {PlayerName}", name);
                return Ok(playerStats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve player rankings for {PlayerName}", name);
                return StatusCode(500, "An error occurred while retrieving player rankings");
            }
        }

        /// <summary>
        /// Calculate title progress and next title information
        /// </summary>
        [HttpGet("{name}/title-info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TitleInfoDto>> GetTitleInfo(string name)
        {
            try
            {
                var player = await context.Players
                    .Where(p => p.PlayerName == name)
                    .OrderByDescending(p => p.Updated)
                    .FirstOrDefaultAsync();

                if (player == null)
                {
                    return NotFound($"Player with name {name} not found");
                }

                // Get current earnings bonus as BigInteger
                BigInteger earningsBonus = CalculateEarningsBonusPercentageNumber(player);

                // Calculate title progress
                var (currentTitle, nextTitle, progress) = GetTitleWithProgress(earningsBonus);

                // Calculate projected title change
                var projectedTitleChange = CalculateProjectedTitleChange(player);

                var titleInfo = new TitleInfoDto
                {
                    CurrentTitle = player.Title,
                    NextTitle = player.NextTitle,
                    TitleProgress = progress,
                    ProjectedTitleChange = projectedTitleChange.ToString("o")
                };

                return Ok(titleInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calculating title info for {PlayerName}", name);
                return StatusCode(500, "An error occurred while calculating title info");
            }
        }

        /// <summary>
        /// Get player goals
        /// </summary>
        [HttpGet("{name}/goals")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GoalDto>> GetPlayerGoals(string name)
        {
            try
            {
                var goal = await context.Goals
                    .FirstOrDefaultAsync(g => g.PlayerName == name);

                if (goal == null)
                {
                    return NotFound($"No goals found for player {name}");
                }

                return Ok(goal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving goals for {PlayerName}", name);
                return StatusCode(500, "An error occurred while retrieving player goals");
            }
        }

        #region Title calculation methods
        private static readonly List<(BigInteger Limit, string Title)> Titles =
        [
            (1000, "Farmer"),
            (10000, "Farmer II"),
            (100000, "Farmer III"),
            (1000000, "Kilo"),
            (10000000, "Kilo II"),
            (100000000, "Kilo III"),
            (1000000000, "Mega"),
            (10000000000, "Mega II"),
            (100000000000, "Mega III"),
            (1000000000000, "Giga"),
            (10000000000000, "Giga II"),
            (100000000000000, "Giga III"),
            (1000000000000000, "Tera"),
            (10000000000000000, "Tera II"),
            (100000000000000000, "Tera III"),
            (1000000000000000000, "Peta"),
            (10000000000000000000, "Peta II"),
            (BigInteger.Parse("100000000000000000000"), "Peta III"),
            (BigInteger.Parse("1000000000000000000000"), "Exa"),
            (BigInteger.Parse("10000000000000000000000"), "Exa II"),
            (BigInteger.Parse("100000000000000000000000"), "Exa III"),
            (BigInteger.Parse("1000000000000000000000000"), "Zetta"),
            (BigInteger.Parse("10000000000000000000000000"), "Zetta II"),
            (BigInteger.Parse("100000000000000000000000000"), "Zetta III"),
            (BigInteger.Parse("1000000000000000000000000000"), "Yotta"),
            (BigInteger.Parse("10000000000000000000000000000"), "Yotta II"),
            (BigInteger.Parse("100000000000000000000000000000"), "Yotta III"),
            (BigInteger.Parse("1000000000000000000000000000000"), "Xenna"),
            (BigInteger.Parse("10000000000000000000000000000000"), "Xenna II"),
            (BigInteger.Parse("100000000000000000000000000000000"), "Xenna III"),
            (BigInteger.Parse("1000000000000000000000000000000000"), "Wecca"),
            (BigInteger.Parse("10000000000000000000000000000000000"), "Wecca II"),
            (BigInteger.Parse("100000000000000000000000000000000000"), "Wecca III"),
            (BigInteger.Parse("1000000000000000000000000000000000000"), "Venda"),
            (BigInteger.Parse("10000000000000000000000000000000000000"), "Venda II"),
            (BigInteger.Parse("100000000000000000000000000000000000000"), "Venda III"),
            (BigInteger.Parse("1000000000000000000000000000000000000000"), "Uada")
        ];

        private (string CurrentTitle, string NextTitle, double Progress) GetTitleWithProgress(BigInteger earningsBonus)
        {
            for (int i = 0; i < Titles.Count; i++)
            {
                if (earningsBonus < Titles[i].Limit)
                {
                    string currentTitle = i == 0 ? "None" : Titles[i].Title;
                    string nextTitle = Titles[i + 1].Title;
                    BigInteger previousLimit = i == 0 ? BigInteger.Zero : Titles[i - 1].Limit;
                    double progress = (double)(earningsBonus - previousLimit) / (double)(Titles[i].Limit - previousLimit) * 100;
                    return (currentTitle, nextTitle, progress);
                }
            }

            // If it exceeds all predefined titles
            return (Titles[^1].Title, "Uada+", 100);
        }

        private BigInteger CalculateEarningsBonusPercentageNumber(PlayerDto player)
        {
            var sefull = BigInteger.Parse(player.SoulEggsFull);
            var eb = sefull * new BigInteger(150 * Math.Pow(1.1, player.ProphecyEggs));
            return eb;
        }

        private DateTime CalculateProjectedTitleChange(PlayerDto player)
        {
            // For now, returning a simplified implementation
            // In a real implementation, you would need to port the full logic from PlayerManager
            var ebNeeded = CalculateEBNeededForNextTitle(player.PlayerName);
            var ebProgressPerHour = CalculateEBProgressPerHour(player.PlayerName, 30);

            if (ebNeeded <= 0 || ebProgressPerHour <= 0)
            {
                return DateTime.MaxValue; // No projection available
            }

            var hoursNeeded = BigInteger.Divide(ebNeeded, ebProgressPerHour);
            return DateTime.Now.AddHours((double)hoursNeeded);
        }

        private BigInteger CalculateEBNeededForNextTitle(string playerName)
        {
            var playerRecord = context.Players
                .Where(p => p.PlayerName == playerName)
                .OrderByDescending(p => p.Updated)
                .FirstOrDefault();

            if (playerRecord == null || playerRecord.EarningsBonusPercentage == null)
            {
                return BigInteger.Zero;
            }

            var currentEB = CalculateEarningsBonusPercentageNumber(playerRecord);
            for (int i = 0; i < Titles.Count; i++)
            {
                if (currentEB < Titles[i].Limit)
                {
                    return Titles[i].Limit - currentEB;
                }
            }

            return BigInteger.Zero;
        }

        private BigInteger CalculateEBProgressPerHour(string playerName, int daysToLookBack)
        {
            var playerRecords = context.Players
                .Where(p => p.PlayerName == playerName &&
                       p.Updated >= DateTime.UtcNow.AddDays(daysToLookBack * -1) &&
                       p.Updated <= DateTime.UtcNow)
                .OrderBy(p => p.Updated)
                .ToList();

            if (playerRecords.Count < 2)
            {
                return 0;
            }

            var initialRecord = playerRecords.First();
            var finalRecord = playerRecords.Last();

            var initEB = CalculateEarningsBonusPercentageNumber(initialRecord);
            var finalEB = CalculateEarningsBonusPercentageNumber(finalRecord);
            var totalProgress = finalEB - initEB;
            var totalHours = (finalRecord.Updated - initialRecord.Updated).TotalHours;

            if ((BigInteger)totalHours <= 0 || totalProgress == 0)
            {
                return 0;
            }

            return BigInteger.Divide(totalProgress, (BigInteger)totalHours);
        }
        #endregion
    }

    // DTO for title information
    public class TitleInfoDto
    {
        public string CurrentTitle { get; set; } = string.Empty;
        public string NextTitle { get; set; } = string.Empty;
        public double TitleProgress { get; set; }
        public string ProjectedTitleChange { get; set; } = string.Empty;
    }
}
