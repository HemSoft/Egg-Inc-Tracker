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
            return dashboardPlayer;
        }

        try
        {
            // Get active missions
            dashboardPlayer.Missions = await rocketMissionService.GetActiveMissionsAsync(
                dashboardPlayer.Player.EID,
                dashboardPlayer.Player.PlayerName);

            // Get standby mission
            dashboardPlayer.StandbyMission = await rocketMissionService.GetStandbyMissionAsync(
                dashboardPlayer.Player.EID,
                dashboardPlayer.Player.PlayerName);

            return dashboardPlayer;
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent cascading failures
            System.Diagnostics.Debug.WriteLine($"Error loading rocket mission data: {ex.Message}");
            return dashboardPlayer;
        }
    }
}
