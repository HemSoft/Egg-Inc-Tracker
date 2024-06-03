namespace HemSoft.EggIncTracker.Domain;

using System.Linq;
using System.Linq.Expressions;
using System.Numerics;

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
            logger?.LogInformation(@"Comparing " + player.EID + " " + player.EarningsBonusPercentage + " to " + getLatestEntry.EarningsBonusPercentage);
            if (getLatestEntry.EarningsBonusPercentage != player.EarningsBonusPercentage)
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

    public static string CalculateEarningsBonusPercentage(PlayerDto player)
    {
        var sefull = BigInteger.Parse(player.SoulEggsFull);
        var eb = sefull * new BigInteger(150 * Math.Pow(1.1, player.ProphecyEggs));
        var ebn = PlayerDto.FormatBigInteger(eb.ToString()) + "%";

        var earningsBonusString = ebn;
        return earningsBonusString;
    }

    public static BigInteger CalculateEarningsBonusPercentageNumber(PlayerDto player)
    {
        var sefull = BigInteger.Parse(player.SoulEggsFull);
        var eb = sefull * new BigInteger(150 * Math.Pow(1.1, player.ProphecyEggs));
        return eb;
    }

    private static readonly List<(BigInteger Limit, string Title)> Titles = new()
    {
        (1000, "Farmer"),
        (10000, "Farmer II"),
        (100000, "Farmer III"),
        (1000000, "Kilo"),
        (10000000, "Kilo II"),
        (100000000, "Kilo III"),
        (1000000000, "Mega"),
        (10000000000, "Mega II"),
        (100000000000, "Mega III"),
        (1000000000000, "Giga"),
        (10000000000000, "Giga II"),
        (100000000000000, "Giga III"),
        (1000000000000000, "Tera"),
        (10000000000000000, "Tera II"),
        (100000000000000000, "Tera III"),
        (1000000000000000000, "Peta"),
        (10000000000000000000, "Peta II"),
        (BigInteger.Parse("100000000000000000000"), "Peta III"),
        (BigInteger.Parse("1000000000000000000000"), "Exa"),
        (BigInteger.Parse("10000000000000000000000"), "Exa II"),
        (BigInteger.Parse("100000000000000000000000"), "Exa III"),
        (BigInteger.Parse("1000000000000000000000000"), "Zetta"),
        (BigInteger.Parse("10000000000000000000000000"), "Zetta II"),
        (BigInteger.Parse("100000000000000000000000000"), "Zetta III"),
        (BigInteger.Parse("1000000000000000000000000000"), "Yotta"),
        (BigInteger.Parse("10000000000000000000000000000"), "Yotta II"),
        (BigInteger.Parse("100000000000000000000000000000"), "Yotta III"),
        (BigInteger.Parse("1000000000000000000000000000000"), "Xenna"),
        (BigInteger.Parse("10000000000000000000000000000000"), "Xenna II"),
        (BigInteger.Parse("100000000000000000000000000000000"), "Xenna III"),
        (BigInteger.Parse("1000000000000000000000000000000000"), "Wecca"),
        (BigInteger.Parse("10000000000000000000000000000000000"), "Wecca II"),
        (BigInteger.Parse("100000000000000000000000000000000000"), "Wecca III"),
        (BigInteger.Parse("1000000000000000000000000000000000000"), "Venda"),
        (BigInteger.Parse("10000000000000000000000000000000000000"), "Venda II"),
        (BigInteger.Parse("100000000000000000000000000000000000000"), "Venda III"),
        (BigInteger.Parse("1000000000000000000000000000000000000000"), "Uada"),
    };

    public static (string CurrentTitle, string NextTitle, double Progress) GetTitleWithProgress(BigInteger earningsBonus)
    {
        for (int i = 0; i < Titles.Count; i++)
        {
            if (earningsBonus < Titles[i].Limit)
            {
                string currentTitle = i == 0 ? "None" : Titles[i].Title;
                string nextTitle = Titles[i + 1].Title;
                BigInteger previousLimit = i == 0 ? BigInteger.Zero : Titles[i - 1].Limit;
                double progress = (double)(earningsBonus - previousLimit) / (double)(Titles[i].Limit - previousLimit) * 100;
                return (currentTitle, nextTitle, progress);
            }
        }

        // If it exceeds all predefined titles
        return (Titles[^1].Title, "Wowa", 100);
    }

    public static DateTime CalculateProjectedTitleChange(PlayerDto player)
    {
        var playerName = player.PlayerName;
        var ebNeeded = CalculateEBNeededForNextTitle(playerName);
        var ebProgressPerHour = CalculateEBProgressPerHour(playerName, 14);
        player.EarningsBonusPerHour = PlayerDto.FormatBigInteger(ebProgressPerHour.ToString());

        if (ebNeeded <= 0 || ebProgressPerHour <= 0)
        {
            return DateTime.MinValue;
        }

        var hoursNeeded = BigInteger.Divide(ebNeeded, ebProgressPerHour);
        var projectedTime = DateTime.Now.AddHours((double) hoursNeeded);
        return projectedTime;
    }

    public static BigInteger CalculateEBNeededForNextTitle(string playerName)
    {
        using var context = new EggIncContext();
        context.Database.EnsureCreated();

        var playerRecord = context.Players
            .Where(p => p.PlayerName == playerName)
            .OrderByDescending(p => p.Updated)
            .First();

        if (playerRecord == null || playerRecord.EarningsBonusPercentage == null)
        {
            return BigInteger.Zero;
        }

        var currentEB = CalculateEarningsBonusPercentageNumber(playerRecord);
        for (int i = 0; i < Titles.Count; i++)
        {
            if (currentEB < Titles[i].Limit)
            {
                return Titles[i].Limit - currentEB;
            }
        }

        return BigInteger.Zero;
    }

    public static BigInteger CalculateEBProgressPerHour(string playerName, int daysToLookBack)
    {
        using var context = new EggIncContext();
        context.Database.EnsureCreated();

        var playerRecords = context.Players
            .Where(p => p.PlayerName == playerName &&
                   p.Updated >= DateTime.UtcNow.AddDays(daysToLookBack * -1) &&
                   p.Updated <= DateTime.UtcNow)
            .OrderBy(p => p.Updated)
            .ToList();

        if (playerRecords.Count < 2)
        {
            throw new InvalidOperationException("Not enough data to calculate progress rate.");
        }

        var initialRecord = playerRecords.First();
        var finalRecord = playerRecords.Last();

        var initEB = CalculateEarningsBonusPercentageNumber(initialRecord);
        var finalEB = CalculateEarningsBonusPercentageNumber(finalRecord);
        var totalProgress = finalEB - initEB;
        var totalHours = (finalRecord.Updated - initialRecord.Updated).TotalHours;

        if (totalHours <= 0)
        {
            throw new InvalidOperationException("Invalid time range.");
        }

        return BigInteger.Divide(totalProgress, (BigInteger) totalHours);
    }
}
