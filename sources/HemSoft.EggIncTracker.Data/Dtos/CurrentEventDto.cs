namespace HemSoft.EggIncTracker.Data.Dtos;

using System;
using System.ComponentModel.DataAnnotations;

public class CurrentEventDto
{
    [Required]
    [MaxLength(50)]
    public string SubTitle { get; set; } = null!;

    public DateTime? EndTime { get; set; }
}
