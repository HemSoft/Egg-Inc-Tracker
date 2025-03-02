namespace HemSoft.EggIncTracker.Domain;

using System.Linq;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;

public static class ContractManager
{
    public static bool SaveContract(ContractDto contract, ILogger ?logger)
    {
        var context = new EggIncContext();
        var getLatestEntry = context.Contracts
            .Where(x => x.KevId == contract.KevId)
            .OrderByDescending(z => z.StartTime)
            .FirstOrDefault();

        if (getLatestEntry != null)
        {
            if (getLatestEntry.StartTime < contract.StartTime)
            {
                logger?.LogInformation($"\u001b[New contract found! \" + contract.Name + (" + contract.KevId + ") -- Saving ...\u001b[0m");
                context.Contracts.Add(contract);
                context.SaveChanges();
                logger?.LogInformation("Done ...");
                return true;
            }

            return false;
        }

        logger?.LogInformation(@$"Saving new contract ...");
        context.Contracts.Add(contract);
        context.SaveChanges();
        logger?.LogInformation("Done.");
        return true;
    }
}
