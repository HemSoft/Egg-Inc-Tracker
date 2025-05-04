namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Utilities;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for handling rocket mission data and operations
/// </summary>
public class RocketMissionService
{
    private readonly ILogger<RocketMissionService> _logger;

    /// <summary>
    /// Ship type mapping dictionary
    /// </summary>
    public static readonly Dictionary<int, string> ShipTypes = new()
    {
        { 1, "Chicken One" },
        { 2, "Chicken Heavy" },
        { 3, "BCR" },
        { 4, "Quintillion Chicken" },
        { 5, "Corellihen Corvette" },
        { 6, "Galeggtica" },
        { 7, "Henerprise" }
    };

    /// <summary>
    /// Mission status mapping dictionary
    /// </summary>
    public static readonly Dictionary<int, string> MissionStatuses = new()
    {
        { 0, "Unknown" },
        { 1, "Fueling" },
        { 2, "Exploring" },
        { 3, "Returned" },
        { 4, "Complete" },
        { 5, "Archived" }
    };

    /// <summary>
    /// Egg type mapping dictionary
    /// </summary>
    public static readonly Dictionary<int, string> EggTypes = new()
    {
        { 1, "Regular" },
        { 2, "Superfood" },
        { 3, "Medical" },
        { 4, "Rocket Fuel" },
        { 5, "Super Material" },
        { 6, "Fusion" },
        { 7, "Quantum" },
        { 8, "Immortality" },
        { 9, "Tachyon" },
        { 10, "Graviton" },
        { 11, "Dilithium" },
        { 12, "Prodigy" },
        { 13, "Terraform" },
        { 14, "Antimatter" },
        { 15, "Dark Matter" },
        { 16, "AI" },
        { 17, "Nebula" },
        { 18, "Universe" },
        { 19, "Enlightenment" }
    };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public RocketMissionService(ILogger<RocketMissionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get active missions for a player
    /// </summary>
    /// <param name="playerEid">Player EID</param>
    /// <param name="playerName">Player name</param>
    /// <returns>List of active missions</returns>
    public async Task<List<JsonPlayerExtendedMissionInfo>> GetActiveMissionsAsync(string playerEid, string playerName)
    {
        try
        {
            _logger.LogInformation("Getting active missions for player {PlayerName} from database", playerName);

            // Get missions from the database
            var dbMissions = await MissionManager.GetActiveMissionsAsync(playerName, _logger);

            if (dbMissions == null || dbMissions.Count == 0)
            {
                _logger.LogWarning("No missions found in database for player {PlayerName}", playerName);
                return new List<JsonPlayerExtendedMissionInfo>();
            }

            // Convert database missions to JsonPlayerExtendedMissionInfo
            var missions = dbMissions.Select(m => new JsonPlayerExtendedMissionInfo
            {
                Ship = m.Ship,
                Status = m.Status,
                DurationType = m.DurationType,
                Level = m.Level,
                DurationSeconds = m.DurationSeconds,
                Capacity = m.Capacity,
                QualityBump = m.QualityBump,
                TargetArtifact = m.TargetArtifact,
                SecondsRemaining = CalculateSecondsRemaining(m.ReturnTime),
                FuelList = DeserializeFuelList(m.FuelListJson),
                StartTimeDerived = new DateTimeOffset(m.LaunchTime).ToUnixTimeSeconds(),
                // Add a custom property to store the actual return time
                MissionLog = m.ReturnTime.ToString("o") // ISO 8601 format to preserve the exact time
            }).ToList();

            _logger.LogInformation("Found {Count} active missions from database for player {PlayerName}",
                missions.Count, playerName);

            return missions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active missions for player {PlayerName}", playerName);
            return new List<JsonPlayerExtendedMissionInfo>();
        }
    }

    /// <summary>
    /// Calculate seconds remaining for a mission
    /// </summary>
    /// <param name="returnTime">Mission return time</param>
    /// <returns>Seconds remaining</returns>
    private float CalculateSecondsRemaining(DateTime returnTime)
    {
        var now = DateTime.UtcNow;
        if (returnTime <= now)
        {
            return 0;
        }

        var timeSpan = returnTime - now;
        return (float)timeSpan.TotalSeconds;
    }

    /// <summary>
    /// Deserialize fuel list from JSON string
    /// </summary>
    /// <param name="fuelListJson">JSON string of fuel list</param>
    /// <returns>List of JsonPlayerFuel objects</returns>
    private List<JsonPlayerFuel> DeserializeFuelList(string? fuelListJson)
    {
        if (string.IsNullOrEmpty(fuelListJson))
        {
            return new List<JsonPlayerFuel>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<JsonPlayerFuel>>(fuelListJson) ?? new List<JsonPlayerFuel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing fuel list: {FuelListJson}", fuelListJson);
            return new List<JsonPlayerFuel>();
        }
    }

    /// <summary>
    /// Get standby mission for a player (mission that is fueled and ready to launch)
    /// </summary>
    /// <param name="playerEid">Player EID</param>
    /// <param name="playerName">Player name</param>
    /// <returns>Standby mission if available, null otherwise</returns>
    public async Task<JsonPlayerExtendedMissionInfo?> GetStandbyMissionAsync(string playerEid, string playerName)
    {
        try
        {
            _logger.LogInformation("Getting standby mission for player {PlayerName} from database", playerName);

            // Get standby mission from the database
            var dbMission = await MissionManager.GetStandbyMissionAsync(playerName, _logger);

            if (dbMission == null)
            {
                _logger.LogWarning("No standby mission found in database for player {PlayerName}", playerName);
                return null;
            }

            // Convert database mission to JsonPlayerExtendedMissionInfo
            return new JsonPlayerExtendedMissionInfo
            {
                Ship = dbMission.Ship,
                Status = dbMission.Status,
                DurationType = dbMission.DurationType,
                Level = dbMission.Level,
                DurationSeconds = dbMission.DurationSeconds,
                Capacity = dbMission.Capacity,
                QualityBump = dbMission.QualityBump,
                TargetArtifact = dbMission.TargetArtifact,
                FuelList = DeserializeFuelList(dbMission.FuelListJson)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting standby mission for player {PlayerName}", playerName);
            return null;
        }
    }

    /// <summary>
    /// Calculate mission progress percentage
    /// </summary>
    /// <param name="mission">Mission information</param>
    /// <returns>Progress percentage (0-100)</returns>
    public double CalculateMissionProgressPercentage(JsonPlayerExtendedMissionInfo mission)
    {
        // Status 2 = Exploring, Status 10 = In Progress
        if ((mission.Status != 2 && mission.Status != 10) || mission.DurationSeconds <= 0)
        {
            return 0;
        }

        double elapsed = mission.DurationSeconds - mission.SecondsRemaining;
        double percentage = (elapsed / mission.DurationSeconds) * 100;
        return Math.Clamp(percentage, 0, 100);
    }

    /// <summary>
    /// Format time remaining in a human-readable format
    /// </summary>
    /// <param name="seconds">Seconds remaining</param>
    /// <returns>Formatted time string</returns>
    public string FormatTimeRemaining(double seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalDays >= 1)
        {
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }

    /// <summary>
    /// Calculate mission completion time
    /// </summary>
    /// <param name="startTimeDerived">Mission start time (Unix timestamp)</param>
    /// <param name="durationSeconds">Mission duration in seconds</param>
    /// <returns>Formatted completion time</returns>
    public string CalculateMissionCompletionTime(double startTimeDerived, float durationSeconds)
    {
        try
        {
            DateTime startTime = DateTimeOffset.FromUnixTimeSeconds((long)startTimeDerived).DateTime;
            DateTime completionTime = startTime.AddSeconds(durationSeconds);

            // Convert to local time
            DateTime localCompletionTime = TimeZoneInfo.ConvertTimeFromUtc(completionTime, TimeZoneInfo.Local);

            return localCompletionTime.ToString("MM/dd HH:mm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating mission completion time");
            return "Unknown";
        }
    }

    /// <summary>
    /// Get ship name from ship type
    /// </summary>
    /// <param name="shipType">Ship type code</param>
    /// <returns>Ship name</returns>
    public string GetShipName(int shipType)
    {
        return ShipNameMapper.GetShipName(shipType);
    }

    /// <summary>
    /// Get mission status name from status code
    /// </summary>
    /// <param name="statusCode">Status code</param>
    /// <returns>Status name</returns>
    public string GetStatusName(int statusCode)
    {
        return ShipNameMapper.GetStatusDescription(statusCode);
    }

    /// <summary>
    /// Get egg name from egg type
    /// </summary>
    /// <param name="eggType">Egg type code</param>
    /// <returns>Egg name</returns>
    public string GetEggName(int eggType)
    {
        return EggTypes.TryGetValue(eggType, out string? name) ? name : $"Unknown Egg ({eggType})";
    }

    /// <summary>
    /// Calculate fuel percentage
    /// </summary>
    /// <param name="fuel">Fuel information</param>
    /// <returns>Fuel percentage (0-100)</returns>
    public double CalculateFuelPercentage(JsonPlayerFuel fuel)
    {
        return Math.Min(fuel.Amount * 100, 100);
    }
}
