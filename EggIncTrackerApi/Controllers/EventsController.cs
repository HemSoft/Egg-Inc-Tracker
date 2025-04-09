namespace EggIncTrackerApi.Controllers;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EventsController(EggIncContext context, ILogger<EventsController> logger) : ControllerBase
{
    private readonly EggIncContext _context = context;
    private readonly ILogger<EventsController> _logger = logger;

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
    /// Get active events
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CurrentEventDto>>> GetCurrentEvents()
    {
        // Use a raw SQL query with explicit mapping to CurrentEventDto
        var currentEvents = new List<CurrentEventDto>();

        try
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC CurrentEvents";

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        currentEvents.Add(new CurrentEventDto
                        {
                            SubTitle = reader.GetString(0),
                            EndTime = reader.IsDBNull(1) ? null : reader.GetDateTime(1)
                        });
                    }
                }
            }

            _logger.LogInformation($"Found {currentEvents.Count} active events");
            return Ok(currentEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current events");
            return StatusCode(500, "An error occurred while retrieving current events");
        }
    }
}
