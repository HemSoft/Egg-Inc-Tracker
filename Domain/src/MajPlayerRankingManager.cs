namespace HemSoft.EggIncTracker.Domain;

using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;

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
                .GroupBy(x => x.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First()) // Get most recent record per IGN
                .ToList() // Bring data into memory for in-memory sorting
                .Select(mrp => new
                {
                    IGN = mrp.IGN,
                    Ranking = mrp,
                    EBValue = BigNumberCalculator.ParseBigNumber(mrp.EBString.TrimEnd('%'))
                })
                .OrderByDescending(x => x.EBValue)
                .ToList();

            var ranking = 1;
            foreach (var mrp in rankings)
            {
                ranking++;
                if (playerEB > mrp.EBValue && mrp.IGN != playerName)
                {
                    p1 = mrp.Ranking;
                    p1.Ranking = ranking;
                    break;
                }
                else if (mrp.EBValue >= playerEB && mrp.IGN != playerName)
                {
                    p2 = mrp.Ranking;
                    p2.Ranking = ranking;
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
        MajPlayerRankingDto lowerPlayer = null;
        MajPlayerRankingDto upperPlayer = null;

        var playerSE = (decimal)BigNumberCalculator.ParseBigNumber(se);

        var context = new EggIncContext();
        var rankings = context.MajPlayerRankings
            .OrderByDescending(mrp => mrp.SENumber)
            .ToList();

        foreach (var mrp in rankings)
        {
            if (mrp.SENumber < playerSE && mrp.IGN != playerName)
            {
                lowerPlayer = mrp;
                break;
            }
            else if (mrp.SENumber > playerSE && mrp.IGN != playerName)
            {
                upperPlayer = mrp;
            }
        }

        return (lowerPlayer, upperPlayer);
    }

    public static (MajPlayerRankingDto, MajPlayerRankingDto) GetSurroundingMERPlayers(string playerName, decimal mer)
    {
        MajPlayerRankingDto p1 = null;
        MajPlayerRankingDto p2 = null;

        try
        {
            var context = new EggIncContext();
            var rankings = context.MajPlayerRankings
                .GroupBy(x => x.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First()) // Get most recent record per IGN
                .ToList() // Bring data into memory for in-memory sorting
                .OrderByDescending(x => x.MER)
                .ToList();

            var ranking = 1;
            foreach (var mrp in rankings)
            {
                ranking++;
                if (mer > mrp.MER && mrp.IGN != playerName)
                {
                    p1 = mrp;
                    p1.Ranking = ranking;
                    break;
                }
                else if (mrp.MER >= mer && mrp.IGN != playerName)
                {
                    p2 = mrp;
                    p2.Ranking = ranking;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetSurroundingMERPlayers for player {playerName} with MER {mer}. Exception: {ex}");
            throw;
        }

        return (p1, p2);
    }

    public static (MajPlayerRankingDto, MajPlayerRankingDto) GetSurroundingJERPlayers(string playerName, decimal jer)
    {
        MajPlayerRankingDto p1 = null;
        MajPlayerRankingDto p2 = null;

        try
        {
            var context = new EggIncContext();
            var rankings = context.MajPlayerRankings
                .GroupBy(x => x.IGN)
                .Select(g => g.OrderByDescending(r => r.Updated).First()) // Get most recent record per IGN
                .ToList() // Bring data into memory for in-memory sorting
                .OrderByDescending(x => x.JER)
                .ToList();

            var ranking = 1;
            foreach (var mrp in rankings)
            {
                ranking++;
                if (jer > mrp.JER && mrp.IGN != playerName)
                {
                    p1 = mrp;
                    p1.Ranking = ranking;
                    break;
                }
                else if (mrp.JER >= jer && mrp.IGN != playerName)
                {
                    p2 = mrp;
                    p2.Ranking = ranking;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetSurroundingjerPlayers for player {playerName} with jer {jer}. Exception: {ex}");
            throw;
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
                .Where(x => x.IGN == majPlayerRanking.IGN && x.DiscordName == majPlayerRanking.DiscordName)
                .OrderByDescending(z => z.Updated)
                .FirstOrDefault();

            if (getLatestEntry != null)
            {
                // This check to avoid Mordeekakh having two entries with 28 PE difference.
                if (getLatestEntry.PE - majPlayerRanking.PE > 5)
                {
                    return (false, null);
                }

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
