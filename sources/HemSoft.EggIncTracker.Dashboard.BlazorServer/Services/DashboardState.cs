namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System.Numerics;
using HemSoft.EggIncTracker.Domain;
using Microsoft.AspNetCore.Components;

public class DashboardState
{
    private readonly Dispatcher _dispatcher;

    // Add this event declaration at the top of the class
    public event Action? OnChange;

    public DashboardState(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }


    public DateTime LastUpdated { get; private set; } = DateTime.Now;

    // Store last updated timestamps for each player
    private readonly Dictionary<string, DateTime> _playerLastUpdated = new()
    {
        { "King Friday!", DateTime.MinValue },
        { "King Saturday!", DateTime.MinValue },
        { "King Sunday!", DateTime.MinValue },
        { "King Monday!", DateTime.MinValue }
    };

    public DateTime GetPlayerLastUpdated(string playerName)
    {
        return _playerLastUpdated.GetValueOrDefault(playerName, DateTime.MinValue);
    }

    public void SetLastUpdated(DateTime lastUpdated)
    {
        LastUpdated = lastUpdated;
        // Use Dispatcher to ensure we're on the UI thread
        _dispatcher?.InvokeAsync(NotifyStateChanged);
    }

    // Update to accept player name
    public void SetPlayerLastUpdated(string playerName, DateTime playerLastUpdated)
    {
        _playerLastUpdated[playerName] = playerLastUpdated;
        // Use Dispatcher to ensure we're on the UI thread
        _dispatcher?.InvokeAsync(NotifyStateChanged);
    }

    private void NotifyStateChanged()
    {
        try
        {
            // Use the dispatcher to ensure we're on the UI thread
            // This is a simpler approach that works because we're already using the dispatcher
            // to call this method
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            // Log the exception but don't rethrow to prevent cascading failures
            System.Diagnostics.Debug.WriteLine($"Error in NotifyStateChanged: {ex.Message}");
        }
    }

    private readonly Dictionary<string, BigInteger?> _playerSEThisWeek = new();

    public BigInteger? GetPlayerSEThisWeek(string playerName)
    {
        return _playerSEThisWeek.GetValueOrDefault(playerName, null);
    }

    public void SetPlayerSEThisWeek(string playerName, BigInteger? seThisWeek)
    {
        _playerSEThisWeek[playerName] = seThisWeek;
        // Use Dispatcher to ensure we're on the UI thread
        _dispatcher?.InvokeAsync(NotifyStateChanged);
    }

    // Rocket mission data storage
    private readonly Dictionary<string, List<JsonPlayerExtendedMissionInfo>> _playerMissions = new();
    private readonly Dictionary<string, JsonPlayerExtendedMissionInfo?> _playerStandbyMissions = new();
    private readonly Dictionary<string, DateTime> _missionLastUpdated = new();

    /// <summary>
    /// Get active missions for a player
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <returns>List of active missions</returns>
    public List<JsonPlayerExtendedMissionInfo> GetPlayerMissions(string playerName)
    {
        return _playerMissions.GetValueOrDefault(playerName, new List<JsonPlayerExtendedMissionInfo>());
    }

    /// <summary>
    /// Set active missions for a player
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="missions">List of active missions</param>
    public void SetPlayerMissions(string playerName, List<JsonPlayerExtendedMissionInfo> missions)
    {
        _playerMissions[playerName] = missions;
        _missionLastUpdated[playerName] = DateTime.UtcNow;
        // Use Dispatcher to ensure we're on the UI thread
        _dispatcher?.InvokeAsync(NotifyStateChanged);
    }

    /// <summary>
    /// Get standby mission for a player
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <returns>Standby mission if available, null otherwise</returns>
    public JsonPlayerExtendedMissionInfo? GetPlayerStandbyMission(string playerName)
    {
        return _playerStandbyMissions.GetValueOrDefault(playerName);
    }

    /// <summary>
    /// Set standby mission for a player
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="mission">Standby mission</param>
    public void SetPlayerStandbyMission(string playerName, JsonPlayerExtendedMissionInfo? mission)
    {
        _playerStandbyMissions[playerName] = mission;
        // Use Dispatcher to ensure we're on the UI thread
        _dispatcher?.InvokeAsync(NotifyStateChanged);
    }

    /// <summary>
    /// Get last updated timestamp for missions
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <returns>Last updated timestamp</returns>
    public DateTime GetMissionLastUpdated(string playerName)
    {
        return _missionLastUpdated.GetValueOrDefault(playerName, DateTime.MinValue);
    }
}
