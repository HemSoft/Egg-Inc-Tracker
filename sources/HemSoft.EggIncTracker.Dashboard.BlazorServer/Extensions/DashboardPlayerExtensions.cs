namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Extensions;

using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Domain;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for the DashboardPlayer class
/// </summary>
public static class DashboardPlayerExtensions
{
    /// <summary>
    /// Load rocket mission data for a dashboard player
    /// </summary>
    /// <param name="dashboardPlayer">The dashboard player to load data for</param>
    /// <param name="rocketMissionService">The rocket mission service</param>
    /// <returns>The updated dashboard player</returns>
    public static async Task<DashboardPlayer> LoadRocketMissionDataAsync(
        this DashboardPlayer dashboardPlayer,
        RocketMissionService rocketMissionService)
    {
        if (dashboardPlayer.Player == null)
        {
            System.Diagnostics.Debug.WriteLine($"Player is null in LoadRocketMissionDataAsync");
            return dashboardPlayer;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"Loading mission data for player {dashboardPlayer.Player.PlayerName} with EID {dashboardPlayer.Player.EID}");

            // Get active missions
            var missions = await rocketMissionService.GetActiveMissionsAsync(
                dashboardPlayer.Player.EID,
                dashboardPlayer.Player.PlayerName);

            System.Diagnostics.Debug.WriteLine($"Loaded {missions.Count} missions for player {dashboardPlayer.Player.PlayerName}");

            // Ensure we have a non-null list
            dashboardPlayer.Missions = missions ?? new List<JsonPlayerExtendedMissionInfo>();

            // Log details of each mission
            foreach (var mission in dashboardPlayer.Missions)
            {
                System.Diagnostics.Debug.WriteLine($"Mission for {dashboardPlayer.Player.PlayerName}: Ship={mission.Ship}, Status={mission.Status}, Level={mission.Level}");
            }

            // Get standby mission
            dashboardPlayer.StandbyMission = await rocketMissionService.GetStandbyMissionAsync(
                dashboardPlayer.Player.EID,
                dashboardPlayer.Player.PlayerName);

            if (dashboardPlayer.StandbyMission != null)
            {
                System.Diagnostics.Debug.WriteLine($"Loaded standby mission for player {dashboardPlayer.Player.PlayerName}: Ship={dashboardPlayer.StandbyMission.Ship}, Level={dashboardPlayer.StandbyMission.Level}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No standby mission found for player {dashboardPlayer.Player.PlayerName}");
            }

            return dashboardPlayer;
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent cascading failures
            System.Diagnostics.Debug.WriteLine($"Error loading rocket mission data: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return dashboardPlayer;
        }
    }
}
