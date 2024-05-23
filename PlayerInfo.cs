namespace HemSoft.EggIncTracker.Models;

using static System.Net.Mime.MediaTypeNames;
using System.Numerics;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;
using System.Xml.Linq;

public class PlayerInfo
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int TotalCraftsThatCanBeLegendary { get; set; }
    public float ExpectedLegendaryCrafts { get; set; }
    public float ExpectedLegendaryDropsFromShips { get; set; }
    public float ExpectedLegendaries { get; set; }
    public float PlayerLegendaries { get; set; }
    public float PlayerLegendariesExcludingLunarTotem { get; set; }
    public float LLC { get; set; }
    public int ProphecyEggs { get; set; }
    public BigInteger SoulEggs { get; set; }
    public float MER { get; set; }
    public float JER { get; set; }
    public int CraftingLevel { get; set; }
    public int PiggyConsumeValue { get; set; }
    public float ShipLaunchPoints { get; set; }
    public float HoarderScore { get; set; }

    public static PlayerInfo ApiToPLayerInfo(string playerId, string playerName, string apiResponse)
    {
        var playerInfo = new PlayerInfo();
        playerInfo.PlayerId = playerId;
        playerInfo.PlayerName = playerName;
        playerInfo.TotalCraftsThatCanBeLegendary = GetNumber(apiResponse, @"legendary:\s+(\d+)");
        playerInfo.ExpectedLegendaryCrafts = GetFloat(apiResponse, @"Expected legendary crafts:\s+(\d+\.\d+)");
        playerInfo.ExpectedLegendaryDropsFromShips = GetFloat(apiResponse, @"Expected legendary drops from ships:\s+(\d+\.\d+)");
        playerInfo.ExpectedLegendaries = GetFloat(apiResponse, @"Expected legendaries:\s+(\d+\.\d+)");
        playerInfo.PlayerLegendaries = GetFloat(apiResponse, @"Your legendaries:\s+(\d+\.\d+)");
        playerInfo.PlayerLegendariesExcludingLunarTotem = GetFloat(apiResponse, @"Your legendaries excluding Lunar Totem:\s+(\d+\.\d+)");
        playerInfo.LLC = GetFloat(apiResponse, @"Your LLC:\s+(\d+\.\d+)");
        playerInfo.ProphecyEggs = GetNumber(apiResponse, @"Prophecy eggs:\s+(\d+)");
        playerInfo.MER = GetFloat(apiResponse, @"Your MER is:\s+(\d+\.\d+)");
        playerInfo.SoulEggs = GetBigNumber(apiResponse, @"Soul eggs: ([\d,]+)");
        playerInfo.ProphecyEggs = GetNumber(apiResponse, @"Prophecy eggs:\s+(\d+)");
        playerInfo.JER = GetFloat(apiResponse, @"Your JER is:\s+(\d+\.\d+)");
        playerInfo.CraftingLevel = GetNumber(apiResponse, @"Crafting level:\s+(\d+)");
        playerInfo.ShipLaunchPoints = GetFloat(apiResponse, @"Henliner launch points:\s+(\d+\.\d+)");
        playerInfo.HoarderScore = GetFloat(apiResponse, @"Hoarder score:\s+(\d+\.\d+)");
        return playerInfo;
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

    public override string ToString()
    {
        return
            $"Name .........: {PlayerName}\n" +
            $"PlayerId .....: {PlayerId}\n" +
            $"Prophecy Eggs : {ProphecyEggs}\n" +
            $"Soul Eggs ....: {SoulEggs.ToString("N0")}\n" +
            $"LLC ..........: {LLC}\n" +
            $"MER ..........: {MER}\n" +
            $"JER ..........: {JER}\n" +
            $"Crafting Level: {CraftingLevel}\n";
    }
}
