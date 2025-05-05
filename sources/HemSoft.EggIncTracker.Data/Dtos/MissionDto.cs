namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

/// <summary>
/// Represents a rocket mission in the Egg Inc game
/// </summary>
public class MissionDto
{
    /// <summary>
    /// The unique identifier for the mission
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the player who owns this mission
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Navigation property to the player
    /// </summary>
    [ForeignKey("PlayerId")]
    public PlayerDto? Player { get; set; }

    /// <summary>
    /// The player's name (for easier querying)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// The type of ship (1-10)
    /// 1: Chicken One
    /// 2: BCR
    /// 3: Quintillion Chicken
    /// 4: Cornish-Hen Corvette
    /// 5: Galeggtica
    /// 6: Defihent
    /// 7: Voyegger
    /// 8: Henerprise
    /// 9: Defihent
    /// 10: Henliner
    /// </summary>
    [Required]
    public int Ship { get; set; }

    /// <summary>
    /// The status of the mission
    /// 1: Fueling
    /// 2: In Progress
    /// 3: Returned
    /// </summary>
    [Required]
    public int Status { get; set; }

    /// <summary>
    /// The type of mission duration
    /// </summary>
    [Required]
    public int DurationType { get; set; }

    /// <summary>
    /// The level of the mission
    /// </summary>
    [Required]
    public int Level { get; set; }

    /// <summary>
    /// The duration of the mission in seconds
    /// </summary>
    [Required]
    public float DurationSeconds { get; set; }

    /// <summary>
    /// The capacity of the ship
    /// </summary>
    [Required]
    public int Capacity { get; set; }

    /// <summary>
    /// Quality modifier for artifacts
    /// </summary>
    [Required]
    public double QualityBump { get; set; }

    /// <summary>
    /// Target artifact type
    /// </summary>
    [Required]
    public int TargetArtifact { get; set; }

    /// <summary>
    /// Seconds remaining until mission completion
    /// </summary>
    [Required]
    public float SecondsRemaining { get; set; }

    /// <summary>
    /// When the mission was launched
    /// </summary>
    [Required]
    public DateTime LaunchTime { get; set; }

    /// <summary>
    /// When the mission will return
    /// </summary>
    [Required]
    public DateTime ReturnTime { get; set; }

    /// <summary>
    /// Fuel list stored as JSON
    /// </summary>
    public string? FuelListJson { get; set; }

    /// <summary>
    /// Whether this is a standby mission (fueled and ready to launch)
    /// </summary>
    [Required]
    public bool IsStandby { get; set; }

    /// <summary>
    /// When the mission record was created
    /// </summary>
    [Required]
    public DateTime Created { get; set; }

    /// <summary>
    /// When the mission record was last updated
    /// </summary>
    [Required]
    public DateTime Updated { get; set; }

    /// <summary>
    /// Deserialize the fuel list from JSON
    /// </summary>
    [NotMapped]
    public List<JsonPlayerFuel>? FuelListObject
    {
        get => string.IsNullOrEmpty(FuelListJson)
            ? null
            : JsonSerializer.Deserialize<List<JsonPlayerFuel>>(FuelListJson);
        set => FuelListJson = value == null
            ? null
            : JsonSerializer.Serialize(value);
    }
}
