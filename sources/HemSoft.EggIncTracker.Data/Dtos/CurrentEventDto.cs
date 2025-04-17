namespace HemSoft.EggIncTracker.Data.Dtos;

using System;
using System.ComponentModel.DataAnnotations;

public class CurrentEventDto
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventId { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string SubTitle { get; set; } = null!;

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [MaxLength(50)]
    public string? EventType { get; set; }
}
