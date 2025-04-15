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
    // Added method to get active events from the database
    public static async Task<List<CurrentEventDto>?> GetActiveEventsAsync(ILogger? logger)
    {
        logger?.LogWarning("GetActiveEventsAsync is not implemented yet.");
        await Task.Delay(10); // Simulate async work
        // Placeholder implementation - replace with actual DB query
        // Example: Query Events table where EndTime > DateTime.UtcNow
        return new List<CurrentEventDto>();
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
