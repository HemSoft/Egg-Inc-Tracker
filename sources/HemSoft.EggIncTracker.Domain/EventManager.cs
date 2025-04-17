namespace HemSoft.EggIncTracker.Domain;

using System.Linq;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // Added for EF Core methods
using System.Collections.Generic; // Added for List<>
using System.Threading.Tasks; // Added for Task

public static class EventManager
{
    // Method to get active events from the database
    public static async Task<List<CurrentEventDto>> GetActiveEventsAsync(ILogger? logger)
    {
        try
        {
            logger?.LogInformation("Getting active events from database");

            using var context = new EggIncContext();
            var now = DateTime.UtcNow;

            // Get events that are currently active (EndTime > now)
            var activeEvents = await context.Events
                .Where(e => e.EndTime == null || e.EndTime > now)
                .OrderBy(e => e.EndTime)
                .Select(e => new CurrentEventDto
                {
                    EventId = e.EventId,
                    Title = e.SubTitle, // Use SubTitle as Title since EventDto doesn't have a Title property
                    SubTitle = e.SubTitle,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    EventType = e.EventType
                })
                .ToListAsync();

            logger?.LogInformation("Found {0} active events", activeEvents.Count);
            return activeEvents;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting active events");
            return new List<CurrentEventDto>();
        }
    }

    public static bool SaveEvent(EventDto eventInfo, ILogger? logger)
    {
        logger?.LogInformation(@"Received request to save event " + eventInfo.SubTitle);

        var context = new EggIncContext();
        var getLatestEntry = context.Events
            .Where(x => x.EventId == eventInfo.EventId)
            .OrderByDescending(z => z.StartTime)
            .FirstOrDefault();

        if (getLatestEntry != null)
        {
            logger?.LogInformation(@"Checking if this is a new event -- " + eventInfo.SubTitle);
            if (getLatestEntry.StartTime < eventInfo.StartTime)
            {
                logger?.LogInformation($"\u001b[New event found! -- Saving ...\u001b[0m");
                context.Events.Add(eventInfo);
                context.SaveChanges();
                logger?.LogInformation("Done ...");
                return true;
            }

            logger?.LogInformation("Not a new event.");
            logger?.LogInformation("Done ...");
            return false;
        }

        logger?.LogInformation(@$"Saving first event of this type ...");
        context.Events.Add(eventInfo);
        context.SaveChanges();
        logger?.LogInformation("Done.");
        return true;
    }
}
