namespace HemSoft.EggIncTracker.Domain;

using System.Linq;
using System.Numerics;

using global::Domain.src;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public static class PlayerManager
{
    public static async Task<PlayerStatsDto?> GetRankedPlayersAsync(string playerName, int recordLimit, int sampleDaysBack, ILogger? logger)
    {
        try
        {
            await using var context = new EggIncContext();

            var rankings = await context.Set<PlayerStatsDto>()
                .FromSqlRaw(
                    $"EXEC GetRankedPlayerRecords @RecordLimit = {recordLimit}, @SampleDaysBack = {sampleDaysBack}")
                .ToListAsync();

            logger?.LogInformation("Successfully retrieved player rankings");
            return rankings.First(x => x.PlayerName == playerName);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to retrieve player rankings");
            return null;
        }
    }

    public static (bool, PlayerDto) SavePlayer(PlayerDto player, JsonPlayerRoot fullPlayerInfo, ILogger ?logger)
    {
        var context = new EggIncContext();
        var getLatestEntry = context.Players
            .Where(x => x.EID == player.EID)
            .OrderByDescending(z => z.Updated)
            .FirstOrDefault();

        if (getLatestEntry != null)
        {
            if (getLatestEntry.EarningsBonusPercentage != player.EarningsBonusPercentage)
            {
                logger?.LogInformation($"\u001b[32mEB increased for " + player.PlayerName + " " + getLatestEntry.EarningsBonusPercentage + " to " + player.EarningsBonusPercentage + "\u001b[0m");
                context.Players.Add(player);
                context.SaveChanges();
                logger?.LogInformation("Done ...");
                return (true, getLatestEntry);
            }

            return (false, null);
        }

        try
        {
            logger?.LogInformation(@$"Saving first entry for {player.PlayerName}...");
            context.Players.Add(player);
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

    public static string CalculateEarningsBonusPercentage(PlayerDto player)
    {
        var sefull = BigInteger.Parse(player.SoulEggsFull);
        var eb = sefull * new BigInteger(150 * Math.Pow(1.1, player.ProphecyEggs));
        var ebn = Utils.FormatBigInteger(eb.ToString()) + "%";

        var earningsBonusString = ebn;
        return earningsBonusString;
    }

    public static string CalculateEarningsBonusPercentage(string soulEggString, int prophecyEggs)
    {
        var sefull = BigInteger.Parse(soulEggString);
        var eb = sefull * new BigInteger(150 * Math.Pow(1.1, prophecyEggs));
        var ebn = Utils.FormatBigInteger(eb.ToString()) + "%";

        var earningsBonusString = ebn;
        return earningsBonusString;
    }

    public static BigInteger CalculateEarningsBonusPercentageNumber(PlayerDto player)
    {
        var sefull = BigInteger.Parse(player.SoulEggsFull);
        var eb = sefull * new BigInteger(150 * Math.Pow(1.1, player.ProphecyEggs));
        return eb;
    }

    public static readonly List<(BigInteger Limit, string Title)> Titles =
    [
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
        (BigInteger.Parse("1000000000000000000000000000000000000000"), "Uada")
    ];

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
        var exponentialRegressionProjection = ProjectionCalculator.CalculateProjectedTitleChange(player);

        var playerName = player.PlayerName;
        var ebNeeded = CalculateEBNeededForNextTitle(playerName);
        var ebProgressPerHour = CalculateEBProgressPerHour(playerName, 30);
        player.EarningsBonusPerHour = Utils.FormatBigInteger(ebProgressPerHour.ToString());

        if (ebNeeded <= 0 || ebProgressPerHour <= 0)
        {
            return DateTime.MinValue;
        }

        var hoursNeeded = BigInteger.Divide(ebNeeded, ebProgressPerHour);
        var projectedTime = DateTime.Now.AddHours((double) hoursNeeded);


        var returnTime = exponentialRegressionProjection;

        var comparison = DateTime.Compare(exponentialRegressionProjection, projectedTime);
        if (comparison >= 0)
        {
            returnTime = projectedTime;
        }

        return returnTime;
    }

    public static BigInteger CalculateEBNeededForNextTitle(string playerName)
    {
        using var context = new EggIncContext();
        context.Database.EnsureCreated();

        var playerRecord = context.Players
            .Where(p => p.PlayerName == playerName)
            .OrderByDescending(p => p.Updated)
            .FirstOrDefault();

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
            return 0;
        }

        var initialRecord = playerRecords.First();
        var finalRecord = playerRecords.Last();

        var initEB = CalculateEarningsBonusPercentageNumber(initialRecord);
        var finalEB = CalculateEarningsBonusPercentageNumber(finalRecord);
        var totalProgress = finalEB - initEB;
        var totalHours = (finalRecord.Updated - initialRecord.Updated).TotalHours;

        if ((BigInteger) totalHours <= 0 || totalProgress == 0)
        {
            return 0;
        }

        return BigInteger.Divide(totalProgress, (BigInteger) totalHours);
    }
}
