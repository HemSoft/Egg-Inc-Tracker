using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EggIncTrackerApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly EggIncContext _context;
        private readonly ILogger<EventsController> _logger;

        public EventsController(EggIncContext context, ILogger<EventsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all events
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
            [FromQuery] bool activeOnly = false,
            [FromQuery] string? eventType = null)
        {
            var query = _context.Events.AsQueryable();

            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(e => e.EndTime > now);
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            query = query.OrderByDescending(e => e.StartTime);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Get event by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventDto>> GetEvent(string id)
        {
            var eventEntry = await _context.Events
                .Where(e => e.EventId == id)
                .OrderByDescending(e => e.StartTime)
                .FirstOrDefaultAsync();

            if (eventEntry == null)
            {
                return NotFound($"Event with ID {id} not found");
            }

            return Ok(eventEntry);
        }

        /// <summary>
        /// Get event types
        /// </summary>
        [HttpGet("types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<string>>> GetEventTypes()
        {
            var eventTypes = await _context.Events
                .Select(e => e.EventType)
                .Distinct()
                .ToListAsync();

            return Ok(eventTypes);
        }

        /// <summary>
        /// Get active events
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetActiveEvents()
        {
            var now = DateTime.UtcNow;
            var activeEvents = await _context.Events
                .Where(e => e.StartTime <= now && e.EndTime >= now)
                .OrderBy(e => e.EndTime)
                .ToListAsync();

            return Ok(activeEvents);
        }
    }
} 