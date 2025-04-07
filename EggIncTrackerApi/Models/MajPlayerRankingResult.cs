namespace EggIncTrackerApi.Models;

using HemSoft.EggIncTracker.Data.Dtos;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Result class for the GetLatestMajPlayerRankingsByIGN stored procedure
/// </summary>
public class MajPlayerRankingResult
{
    public int Id { get; set; }
    public int Ranking { get; set; }
    public string IGN { get; set; }
    public string DiscordName { get; set; }
    public string EBString { get; set; }
    public string Role { get; set; }
    public decimal SENumber { get; set; }
    public string SEString { get; set; }
    public string SEGains { get; set; }
    public string SEGainsWeek { get; set; }
    public int PE { get; set; }
    public string Prestiges { get; set; }
    public int PrestigeGains { get; set; }
    public decimal MER { get; set; }
    public decimal JER { get; set; }
    public DateTime Updated { get; set; }

    /// <summary>
    /// Convert to MajPlayerRankingDto
    /// </summary>
    public MajPlayerRankingDto ToDto()
    {
        return new MajPlayerRankingDto
        {
            Id = Id,
            Ranking = Ranking,
            IGN = IGN,
            DiscordName = DiscordName,
            EBString = EBString,
            Role = Role,
            SENumber = SENumber,
            SEString = SEString,
            SEGains = SEGains,
            SEGainsWeek = SEGainsWeek,
            PE = PE,
            Prestiges = Prestiges,
            MER = MER,
            JER = JER,
            Updated = Updated
        };
    }
}
