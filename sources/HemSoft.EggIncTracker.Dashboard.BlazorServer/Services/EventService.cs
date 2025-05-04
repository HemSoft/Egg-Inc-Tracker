namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Service for managing Egg Inc events
/// </summary>
public class EventService
{
    private readonly ILogger<EventService> _logger;
    private List<EventDto> _activeEvents = new();
    private DateTime _lastRefreshed = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get active events
    /// </summary>
    /// <returns>List of active events</returns>
    public async Task<List<EventDto>> GetActiveEventsAsync()
    {
        try
        {
            // Check if we need to refresh the events
            if (_activeEvents.Count == 0 || DateTime.Now - _lastRefreshed > _refreshInterval)
            {
                _logger.LogInformation("Refreshing active events");
                await RefreshEventsAsync();
            }

            return _activeEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active events");
            return new List<EventDto>();
        }
    }

    /// <summary>
    /// Refresh events from the database
    /// </summary>
    private async Task RefreshEventsAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing events from database");

            // Get events from the database
            var events = await EventManager.GetActiveEventDtosAsync(_logger);

            _activeEvents = events;
            _lastRefreshed = DateTime.Now;

            _logger.LogInformation("Found {Count} active events", _activeEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing events");
        }
    }
}
