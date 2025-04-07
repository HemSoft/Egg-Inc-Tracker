namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GoalDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string PlayerName { get; set; }
    public required string SEGoal { get; set; }
    public required string EBGoal { get; set; }
    public required string MERGoal { get; set; }
    public required string JERGoal { get; set; }
    public required string WeeklySEGainGoal { get; set; }
    public int DailyPrestigeGoal { get; set; }
}
