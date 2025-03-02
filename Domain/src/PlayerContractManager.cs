namespace HemSoft.EggIncTracker.Domain;

using System.Linq;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;

public static class PlayerContractManager
{
    public static bool SavePlayerContract(PlayerContractDto playerContract, ILogger ?logger)
    {
        try
        {

            var context = new EggIncContext();
            var getLatestEntry = context.PlayerContracts
                .Where(x => x.KevId == playerContract.KevId && x.CoopId == playerContract.CoopId)
                .OrderByDescending(z => z.CompletionTime)
                .FirstOrDefault();

            if (getLatestEntry != null)
            {
                if (getLatestEntry.CompletionTime < playerContract.CompletionTime)
                {
                    logger?.LogInformation($"\u001b[New player contract found! " + playerContract.CoopId + "/" + playerContract.KevId + " -- Saving ...\u001b[0m");
                    context.PlayerContracts.Add(playerContract);
                    context.SaveChanges();
                    logger?.LogInformation("Done ...");
                    return true;
                }

                return false;
            }

            logger?.LogInformation(@$"Saving new player contract " + playerContract.KevId + "/" + playerContract.CoopId + " -- Score: " + playerContract.ContractScore + " ...");
            context.PlayerContracts.Add(playerContract);
            context.SaveChanges();
            logger?.LogInformation("Done.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}
