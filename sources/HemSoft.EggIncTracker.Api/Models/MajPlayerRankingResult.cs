namespace HemSoft.EggIncTracker.Api.Models;

using HemSoft.EggIncTracker.Data.Dtos;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Result class for the GetLatestMajPlayerRankingsByIGN stored procedure
/// </summary>
public class MajPlayerRankingResult
{
    public int Id { get; set; }
    public int Ranking { get; set; }
    public required string IGN { get; set; }
    public required string DiscordName { get; set; }
    public required string EBString { get; set; }
    public required string Role { get; set; }
    public decimal SENumber { get; set; }
    public required string SEString { get; set; }
    public required string SEGains { get; set; }
    public required string SEGainsWeek { get; set; }
    public int PE { get; set; }
    public required string Prestiges { get; set; }
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
