namespace HemSoft.EggIncTracker.Domain;

using System.Linq;
using System.Linq.Expressions;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;

public static class PlayerManager
{
    public static void SavePlayer(PlayerDto player, ILogger ?logger)
    {
        logger?.LogInformation(@"Received request to saver player " + player.EID + " " + player.SoulEggs);

        var context = new EggIncContext();
        var getLatestEntry = context.Players
            .Where(x => x.EID == player.EID)
            .OrderByDescending(z => z.Updated)
            .FirstOrDefault();

        if (getLatestEntry != null)
        {
            logger?.LogInformation(@"Comparing " + player.EID + " " + player.SoulEggs + " to " + getLatestEntry.SoulEggs);
            if (getLatestEntry.SoulEggs != player.SoulEggs)
            {
                logger?.LogInformation("Saving ...");
                context.Players.Add(player);
                context.SaveChanges();
                logger?.LogInformation("Done ...");
                return;
            }
        }
        else
        {
            logger?.LogInformation("Saving ...");
            context.Players.Add(player);
            context.SaveChanges();
        }

        logger?.LogInformation("Done.");
    }
}
