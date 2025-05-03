namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        { 2, "Traveling" },
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
            _logger.LogInformation("Getting active missions for player {PlayerName} with EID {PlayerEid}", playerName, playerEid);

            var (_, fullPlayerInfo) = await Task.Run(() => Api.CallPlayerInfoApi(playerEid, playerName));

            if (fullPlayerInfo == null)
            {
                _logger.LogWarning("Player info is null for player {PlayerName}", playerName);
                return new List<JsonPlayerExtendedMissionInfo>();
            }

            if (fullPlayerInfo.ArtifactsDb == null)
            {
                _logger.LogWarning("ArtifactsDb is null for player {PlayerName}", playerName);
                return new List<JsonPlayerExtendedMissionInfo>();
            }

            if (fullPlayerInfo.ArtifactsDb.MissionInfosList == null)
            {
                _logger.LogWarning("MissionInfosList is null for player {PlayerName}", playerName);
                return new List<JsonPlayerExtendedMissionInfo>();
            }

            _logger.LogInformation("Found {Count} missions for player {PlayerName}",
                fullPlayerInfo.ArtifactsDb.MissionInfosList.Count, playerName);

            // Log all missions regardless of status
            foreach (var mission in fullPlayerInfo.ArtifactsDb.MissionInfosList)
            {
                _logger.LogInformation("All missions for {PlayerName}: Ship={Ship}, Status={Status}, Level={Level}",
                    playerName, mission.Ship, mission.Status, mission.Level);
            }

            // Filter active missions - include ALL missions for now for debugging
            var activeMissions = fullPlayerInfo.ArtifactsDb.MissionInfosList
                .OrderBy(m => m.Status)
                .ThenBy(m => m.SecondsRemaining)
                .ToList();

            _logger.LogInformation("Filtered to {Count} active missions for player {PlayerName}",
                activeMissions.Count, playerName);

            // Log details of each mission
            foreach (var mission in activeMissions)
            {
                _logger.LogInformation("Mission for {PlayerName}: Ship={Ship}, Status={Status}, Level={Level}",
                    playerName, mission.Ship, mission.Status, mission.Level);
            }

            return activeMissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active missions for player {PlayerName}", playerName);
            return new List<JsonPlayerExtendedMissionInfo>();
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
            var (_, fullPlayerInfo) = await Task.Run(() => Api.CallPlayerInfoApi(playerEid, playerName));

            if (fullPlayerInfo?.ArtifactsDb?.FuelingMission == null)
            {
                return null;
            }

            // Check if the fueling mission is fully fueled
            var fuelingMission = fullPlayerInfo.ArtifactsDb.FuelingMission;
            bool isFullyFueled = fuelingMission.FuelList.All(f => f.Amount >= 1.0);

            if (isFullyFueled)
            {
                // Convert FuelingMission to ExtendedMissionInfo
                return new JsonPlayerExtendedMissionInfo
                {
                    Ship = fuelingMission.Ship,
                    Status = 1, // Fueling
                    DurationType = fuelingMission.DurationType,
                    FuelList = fuelingMission.FuelList,
                    Level = fuelingMission.Level,
                    Capacity = fuelingMission.Capacity,
                    TargetArtifact = fuelingMission.TargetArtifact
                };
            }

            return null;
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
        if (mission.Status != 2 || mission.DurationSeconds <= 0)
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
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
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
        return ShipTypes.TryGetValue(shipType, out string? name) ? name : $"Unknown Ship ({shipType})";
    }

    /// <summary>
    /// Get mission status name from status code
    /// </summary>
    /// <param name="statusCode">Status code</param>
    /// <returns>Status name</returns>
    public string GetStatusName(int statusCode)
    {
        return MissionStatuses.TryGetValue(statusCode, out string? name) ? name : $"Unknown Status ({statusCode})";
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
