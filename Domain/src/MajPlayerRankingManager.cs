namespace HemSoft.EggIncTracker.Domain;

using System.Linq;
using System.Numerics;

using global::Domain.src;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public static class MajPlayerRankingManager
{
    public static (MajPlayerRankingDto, MajPlayerRankingDto) GetSurroundingEBPlayers(string playerName, string eb)
    {
        MajPlayerRankingDto p1 = null;
        MajPlayerRankingDto p2 = null;

        try
        {
            var playerEB = BigNumberCalculator.ParseBigNumber(eb);

            var context = new EggIncContext();
            var rankings = context.MajPlayerRankings
                .ToList() // Bring data into memory for in-memory sorting
                .Select(mrp => new
                {
                    IGN = mrp.IGN,
                    Ranking = mrp,
                    EBValue = BigNumberCalculator.ParseBigNumber(mrp.EBString.TrimEnd('%'))
                })
                .OrderBy(x => x.EBValue)
                .ToList();

            foreach (var mrp in rankings)
            {
                if (playerEB > mrp.EBValue && mrp.IGN != playerName)
                {
                    p1 = mrp.Ranking;
                }
                else if (mrp.EBValue >= playerEB && mrp.IGN != playerName)
                {
                    p2 = mrp.Ranking;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetSurroundingEBPlayers for player {playerName} with EB {eb}. Exception: {ex}");
            throw;
        }

        return (p1, p2);
    }


    public static (MajPlayerRankingDto, MajPlayerRankingDto) GetSurroundingSEPlayers(string playerName, string se)
    {
        MajPlayerRankingDto p1 = null;
        MajPlayerRankingDto p2 = null;

        var playerSE = (decimal)BigNumberCalculator.ParseBigNumber(se);

        var context = new EggIncContext();
        var rankings = context.MajPlayerRankings
            .OrderBy(mrp => mrp.SENumber)
            .ToList();

        foreach (var mrp in rankings)
        {
            if (mrp.SENumber < playerSE && mrp.IGN != playerName)
            {
                p1 = mrp;
            }
            else if (mrp.SENumber > playerSE)
            {
                p2 = mrp;
                break;
            }
        }

        return (p1, p2);
    }

    public static (bool, MajPlayerRankingDto) SaveMajPlayerRanking(MajPlayerRankingDto majPlayerRanking, ILogger ?logger)
    {
        try
        {
            var context = new EggIncContext();
            var prs = context.MajPlayerRankings;
            var getLatestEntry = context.MajPlayerRankings
                .Where(x => x.IGN == majPlayerRanking.IGN)
                .OrderByDescending(z => z.Updated)
                .FirstOrDefault();

            if (getLatestEntry != null)
            {
                if (getLatestEntry.SEString != majPlayerRanking.SEString)
                {
                    logger?.LogInformation($"\u001b[32mSE increased for " + getLatestEntry.IGN + " " + getLatestEntry.SEString + " to " + majPlayerRanking.SEString + "\u001b[0m");
                    context.MajPlayerRankings.Add(majPlayerRanking);
                    context.SaveChanges();
                    logger?.LogInformation("Done ...");
                    return (true, getLatestEntry);
                }

                if (getLatestEntry.PE != majPlayerRanking.PE)
                {
                    logger?.LogInformation($"\u001b[32mPE increased for " + getLatestEntry.IGN + " " + getLatestEntry.PE + " to " + majPlayerRanking.PE + "\u001b[0m");
                    context.MajPlayerRankings.Add(majPlayerRanking);
                    context.SaveChanges();
                    logger?.LogInformation("Done ...");
                    return (true, getLatestEntry);
                }

                if (getLatestEntry.EBString != majPlayerRanking.EBString)
                {
                    logger?.LogInformation($"\u001b[32mEB increased for " + getLatestEntry.IGN + " " + getLatestEntry.EBString + " to " + majPlayerRanking.EBString + "\u001b[0m");
                    context.MajPlayerRankings.Add(majPlayerRanking);
                    context.SaveChanges();
                    logger?.LogInformation("Done ...");
                    return (true, getLatestEntry);
                }

                return (false, null);
            }

            logger?.LogInformation(@$"Saving first entry for {majPlayerRanking.IGN}...");
            context.MajPlayerRankings.Add(majPlayerRanking);
            context.SaveChanges();
            logger?.LogInformation("Done.");
            return (true, null);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
