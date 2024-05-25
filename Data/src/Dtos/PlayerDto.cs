namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Text.RegularExpressions;

public class PlayerDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string EID { get; set; }
    public required string PlayerName { get; set; }
    public DateTime Updated { get; set; }
    public int TotalCraftsThatCanBeLegendary { get; set; }
    public float ExpectedLegendaryCrafts { get; set; }
    public float ExpectedLegendaryDropsFromShips { get; set; }
    public float ExpectedLegendaries { get; set; }
    public float PlayerLegendaries { get; set; }
    public float PlayerLegendariesExcludingLunarTotem { get; set; }
    public float LLC { get; set; }
    public int ProphecyEggs { get; set; }
    public required string SoulEggs { get; set; }
    public float MER { get; set; }
    public float JER { get; set; }
    public int CraftingLevel { get; set; }
    public int PiggyConsumeValue { get; set; }
    public float ShipLaunchPoints { get; set; }
    public float HoarderScore { get; set; }

    public static PlayerDto ApiToPlayer(string playerId, string playerName, string apiResponse)
    {
        var se = Regex.Replace(GetString(apiResponse, @"Soul eggs: ([\d,]+)"), @",", "");
        var player = new PlayerDto
        {
            EID = playerId,
            PlayerName = playerName,
            Updated = DateTime.UtcNow,
            TotalCraftsThatCanBeLegendary = GetNumber(apiResponse, @"legendary:\s+(\d+)"),
            ExpectedLegendaryCrafts = GetFloat(apiResponse, @"Expected legendary crafts:\s+(\d+\.\d+)"),
            ExpectedLegendaryDropsFromShips = GetFloat(apiResponse, @"Expected legendary drops from ships:\s+(\d+\.\d+)"),
            ExpectedLegendaries = GetFloat(apiResponse, @"Expected legendaries:\s+(\d+\.\d+)"),
            PlayerLegendaries = GetFloat(apiResponse, @"Your legendaries:\s+(\d+\.\d+)"),
            PlayerLegendariesExcludingLunarTotem = GetFloat(apiResponse, @"Your legendaries excluding Lunar Totem:\s+(\d+\.\d+)"),
            LLC = GetFloat(apiResponse, @"Your LLC:\s+(\d+\.\d+)"),
            ProphecyEggs = GetNumber(apiResponse, @"Prophecy eggs:\s+(\d+)"),
            SoulEggs = FormatBigInteger(se),
            JER = GetFloat(apiResponse, @"Your JER is:\s+(\d+\.\d+)"),
            MER = GetFloat(apiResponse, @"Your MER is:\s+(\d+\.\d+)"),
            CraftingLevel = GetNumber(apiResponse, @"Crafting level:\s+(\d+)"),
            ShipLaunchPoints = GetFloat(apiResponse, @"Henliner launch points:\s+(\d+\.\d+)"),
            HoarderScore = GetFloat(apiResponse, @"Hoarder score:\s+(\d+\.\d+)")
        };
        return player;
    }

    private static int GetNumber(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return 0;
    }

    private static BigInteger GetBigNumber(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        string pureString = string.Empty;
        if (match.Success)
        {
            pureString = match.Groups[1].Value;
            var pureNumber = Regex.Replace(pureString, @",", "");
            if (!string.IsNullOrEmpty(pureNumber))
            {
                return BigInteger.Parse(pureNumber);
            }
        }

        return 0;
    }

    private static float GetFloat(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        if (match.Success)
        {
            return float.Parse(match.Groups[1].Value);
        }
        return 0;
    }

    private static string GetString(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }

    public static string FormatBigInteger(string bigInteger)
    {
        var bi = BigInteger.Parse(bigInteger);

        string[] suffixes = { "", "K", "M", "B", "T", "q", "Q", "s", "S", "o", "N", "d", "U" };
        BigInteger divisor = 1;
        int suffixIndex = 0;

        while (bi / divisor >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            divisor *= 1000;
            suffixIndex++;
        }

        decimal formattedValue = (decimal)bi / (decimal)divisor;
        return $"{formattedValue:F3}{suffixes[suffixIndex]}";
    }

    public static bool CompareBigNumber(BigInteger bigNumber, string limit)
    {
        var lim = BigInteger.Parse(limit);
        return bigNumber <= lim;
    }

    public override string ToString()
    {
        return
            $"Id ...........: {Id}\n" +
            $"EID ..........: {EID}\n" +
            $"Name .........: {PlayerName}\n" +
            $"Prophecy Eggs : {ProphecyEggs}\n" +
            $"Soul Eggs ....: {SoulEggs}\n" +
            $"LLC ..........: {LLC}\n" +
            $"MER ..........: {MER}\n" +
            $"JER ..........: {JER}\n" +
            $"Crafting Level: {CraftingLevel}\n";
    }
}
