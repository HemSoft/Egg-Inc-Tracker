namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations.Schema;

public class PlayerStatsDto
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    [Column("P")]
    public float ProgressPercentage { get; set; }
    public string? Updated { get; set; }
    [Column("PE")]
    public int ProphecyEggs { get; set; }
    public string EB { get; set; } = string.Empty;
    [Column("EBG")]
    public string? EBGoal { get; set; } = string.Empty;
    public string SE { get; set; } = string.Empty;
    [Column("SEG")]
    public string? SEGoal { get; set; } = string.Empty;
    [Column("SEH")]
    public string SEPerHour { get; set; } = string.Empty;
    [Column("SED")]
    public string SEPerDay { get; set; } = string.Empty;
    [Column("SEW")]
    public string SEPerWeek { get; set; } = string.Empty;
    [Column("Projected")]
    public string ProjectedTitleChange { get; set; }
    public float MER { get; set; }
    public string? MERGoal { get; set; }
    public float JER { get; set; }
    public string? JERGoal { get; set; }
    public string Title { get; set; } = string.Empty;
    [Column("NTitle")]
    public string NextTitle { get; set; } = string.Empty;
    public float LLC { get; set; }
}
