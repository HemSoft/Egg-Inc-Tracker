using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HemSoft.EggIncTracker.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly EggIncContext _context;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(EggIncContext context, ILogger<ContractsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all contracts
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ContractDto>>> GetContracts(
            [FromQuery] bool activeOnly = false,
            [FromQuery] bool prophecyEggOnly = false)
        {
            var query = _context.Contracts.AsQueryable();

            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(c => c.EndTime > now);
            }

            if (prophecyEggOnly)
            {
                query = query.Where(c => c.HasProphecyEggReward);
            }

            query = query.OrderByDescending(c => c.StartTime);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Get contract by ID
        /// </summary>
        [HttpGet("{kevId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ContractDto>> GetContract(string kevId)
        {
            var contract = await _context.Contracts
                .Where(c => c.KevId == kevId)
                .OrderByDescending(c => c.StartTime)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound($"Contract with ID {kevId} not found");
            }

            return Ok(contract);
        }

        /// <summary>
        /// Get player contracts
        /// </summary>
        [HttpGet("player/{playerName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PlayerContractDto>>> GetPlayerContracts(
            string playerName,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var player = await _context.Players
                .Where(p => p.PlayerName == playerName)
                .OrderByDescending(p => p.Updated)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound($"Player with name {playerName} not found");
            }

            var query = _context.PlayerContracts
                .Where(pc => pc.EID == player.EID);

            if (from.HasValue)
            {
                query = query.Where(pc => pc.TimeAccepted >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(pc => pc.TimeAccepted <= to.Value);
            }

            query = query.OrderByDescending(pc => pc.TimeAccepted);

            var playerContracts = await query.ToListAsync();

            return Ok(playerContracts);
        }
    }
} 
