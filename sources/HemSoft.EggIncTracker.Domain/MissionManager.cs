namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manager class for handling rocket mission data
/// </summary>
public static class MissionManager
{
    /// <summary>
    /// Save missions for a player
    /// </summary>
    /// <param name="playerDto">The player DTO</param>
    /// <param name="fullPlayerInfo">The full player info from the API</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>True if missions were saved, false otherwise</returns>
    public static bool SaveMissions(PlayerDto playerDto, JsonPlayerRoot fullPlayerInfo, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Saving missions for player {PlayerName}", playerDto.PlayerName);

            // Check if we have mission data
            if (fullPlayerInfo?.ArtifactsDb?.MissionInfosList == null)
            {
                logger?.LogWarning("No mission data found for player {PlayerName}", playerDto.PlayerName);
                return false;
            }

            using var context = new EggIncContext();

            // Get the player ID from the database
            var player = context.Players
                .Where(p => p.EID == playerDto.EID)
                .OrderByDescending(p => p.Updated)
                .FirstOrDefault();

            if (player == null)
            {
                logger?.LogWarning("Player {PlayerName} not found in database", playerDto.PlayerName);
                return false;
            }

            // Get current time for created/updated timestamps
            var now = DateTime.UtcNow;
            var approxTime = DateTimeOffset.FromUnixTimeSeconds(fullPlayerInfo.ApproxTime).UtcDateTime;

            // Process active missions
            foreach (var missionInfo in fullPlayerInfo.ArtifactsDb.MissionInfosList)
            {
                // Calculate launch and return times
                var returnTime = approxTime.AddSeconds(missionInfo.SecondsRemaining);
                var launchTime = returnTime.AddSeconds(-(double)missionInfo.DurationSeconds);

                // Check if this mission already exists in the database
                var existingMission = context.Missions
                    .Where(m => m.PlayerName == playerDto.PlayerName &&
                                m.Ship == missionInfo.Ship &&
                                m.Status == missionInfo.Status &&
                                m.ReturnTime == returnTime)
                    .FirstOrDefault();

                if (existingMission != null)
                {
                    // Update existing mission
                    existingMission.SecondsRemaining = (float)missionInfo.SecondsRemaining;
                    existingMission.Updated = now;
                    logger?.LogInformation("Updated existing mission for {PlayerName}: Ship={Ship}, Status={Status}",
                        playerDto.PlayerName, missionInfo.Ship, missionInfo.Status);
                }
                else
                {
                    // Create new mission
                    var mission = new MissionDto
                    {
                        PlayerId = player.Id,
                        PlayerName = playerDto.PlayerName,
                        Ship = missionInfo.Ship,
                        Status = missionInfo.Status,
                        DurationType = missionInfo.DurationType,
                        Level = missionInfo.Level,
                        DurationSeconds = missionInfo.DurationSeconds,
                        Capacity = missionInfo.Capacity,
                        QualityBump = missionInfo.QualityBump,
                        TargetArtifact = missionInfo.TargetArtifact,
                        SecondsRemaining = (float)missionInfo.SecondsRemaining,
                        LaunchTime = launchTime,
                        ReturnTime = returnTime,
                        FuelListObject = missionInfo.FuelList,
                        IsStandby = false,
                        Created = now,
                        Updated = now
                    };

                    context.Missions.Add(mission);
                    logger?.LogInformation("Added new mission for {PlayerName}: Ship={Ship}, Status={Status}",
                        playerDto.PlayerName, missionInfo.Ship, missionInfo.Status);
                }
            }

            // Process standby mission (if any)
            if (fullPlayerInfo.ArtifactsDb.FuelingMission != null)
            {
                var fuelingMission = fullPlayerInfo.ArtifactsDb.FuelingMission;
                bool isFullyFueled = fuelingMission.FuelList.All(f => f.Amount >= 1.0);

                if (isFullyFueled)
                {
                    // Check if this standby mission already exists
                    var existingStandby = context.Missions
                        .Where(m => m.PlayerName == playerDto.PlayerName &&
                                    m.Ship == fuelingMission.Ship &&
                                    m.IsStandby)
                        .FirstOrDefault();

                    if (existingStandby != null)
                    {
                        // Update existing standby mission
                        existingStandby.Updated = now;
                        logger?.LogInformation("Updated existing standby mission for {PlayerName}: Ship={Ship}",
                            playerDto.PlayerName, fuelingMission.Ship);
                    }
                    else
                    {
                        // Create new standby mission
                        var standbyMission = new MissionDto
                        {
                            PlayerId = player.Id,
                            PlayerName = playerDto.PlayerName,
                            Ship = fuelingMission.Ship,
                            Status = 1, // Fueling
                            DurationType = fuelingMission.DurationType,
                            Level = fuelingMission.Level,
                            DurationSeconds = 0, // Not launched yet
                            Capacity = fuelingMission.Capacity,
                            QualityBump = 0, // Not applicable for standby
                            TargetArtifact = fuelingMission.TargetArtifact,
                            SecondsRemaining = 0, // Not launched yet
                            LaunchTime = DateTime.MinValue, // Not launched yet
                            ReturnTime = DateTime.MinValue, // Not launched yet
                            FuelListObject = fuelingMission.FuelList,
                            IsStandby = true,
                            Created = now,
                            Updated = now
                        };

                        context.Missions.Add(standbyMission);
                        logger?.LogInformation("Added new standby mission for {PlayerName}: Ship={Ship}",
                            playerDto.PlayerName, fuelingMission.Ship);
                    }
                }
            }

            // Save changes to the database
            var changes = context.SaveChanges();
            logger?.LogInformation("Saved {Count} mission changes for player {PlayerName}", changes, playerDto.PlayerName);

            return changes > 0;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error saving missions for player {PlayerName}", playerDto.PlayerName);
            return false;
        }
    }

    /// <summary>
    /// Get active missions for a player
    /// </summary>
    /// <param name="playerName">The player name</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>List of active missions</returns>
    public static async Task<List<MissionDto>> GetActiveMissionsAsync(string playerName, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Getting active missions for player {PlayerName}", playerName);

            using var context = new EggIncContext();

            // Get active missions (status 2 = Exploring)
            var missions = await context.Missions
                .Where(m => m.PlayerName == playerName && m.Status == 2 && !m.IsStandby)
                .OrderBy(m => m.ReturnTime)
                .ToListAsync();

            logger?.LogInformation("Found {Count} active missions for player {PlayerName}", missions.Count, playerName);
            return missions;
        }
        catch (Exception ex)
        {
            if (logger != null)
            {
                logger.LogError(ex, "Error getting active missions for player {PlayerName}", playerName);
            }
            return new List<MissionDto>();
        }
    }

    /// <summary>
    /// Get standby mission for a player
    /// </summary>
    /// <param name="playerName">The player name</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Standby mission if available, null otherwise</returns>
    public static async Task<MissionDto?> GetStandbyMissionAsync(string playerName, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Getting standby mission for player {PlayerName}", playerName);

            using var context = new EggIncContext();

            // Get standby mission
            var mission = await context.Missions
                .Where(m => m.PlayerName == playerName && m.IsStandby)
                .OrderByDescending(m => m.Updated)
                .FirstOrDefaultAsync();

            if (mission != null)
            {
                logger?.LogInformation("Found standby mission for player {PlayerName}: Ship={Ship}", playerName, mission.Ship);
            }
            else
            {
                logger?.LogInformation("No standby mission found for player {PlayerName}", playerName);
            }

            return mission;
        }
        catch (Exception ex)
        {
            if (logger != null)
            {
                logger.LogError(ex, "Error getting standby mission for player {PlayerName}", playerName);
            }
            return null;
        }
    }
}
