namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Extensions;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Timers;

public partial class RocketDashboard : IDisposable, IAsyncDisposable
{
    [Inject]
    private ILogger<RocketDashboard> Logger { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!;

    [Inject]
    private RocketMissionService RocketMissionService { get; set; } = default!;

    [Inject]
    private PlayerCardService PlayerCardService { get; set; } = default!;

    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";
    private bool IsLoading = false;
    private Timer? _refreshTimer;
    private Timer? _updateTimer;
    private const int RefreshIntervalMs = 30000; // 30 seconds

    private DashboardPlayer _kingFridayPlayer = new();
    private DashboardPlayer _kingSaturdayPlayer = new();
    private DashboardPlayer _kingSundayPlayer = new();
    private DashboardPlayer _kingMondayPlayer = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            IsLoading = true;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(300); // Small delay to ensure UI updates
            await InitialDataLoad();

            // Set up refresh timer
            SetupRefreshTimer();

            // Set up update timer for the "Last updated" text
            SetupUpdateTimer();

            // Subscribe to player data refresh events
            PlayerCardService.OnPlayerDataRefreshed += OnPlayerDataRefreshed;

            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initialization");
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void SetupRefreshTimer()
    {
        try
        {
            _refreshTimer = new Timer(RefreshIntervalMs);
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Enabled = true;
            Logger.LogInformation("Refresh timer started (30 second interval)");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting up refresh timer");
        }
    }

    private void SetupUpdateTimer()
    {
        try
        {
            // Dispose of any existing timer
            if (_updateTimer != null)
            {
                _updateTimer.Elapsed -= OnUpdateTimerElapsed;
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // Create a timer to update the "Last updated" text every second
            _updateTimer = new Timer(1000); // 1 second
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
            Logger.LogInformation("Update timer started (1 second interval)");

            // Initialize the last updated time
            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting up update timer");
        }
    }

    private async void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // Update the time since last update
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();

            // Only update the time display element, not the entire page
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            // Component is being disposed, stop the timer
            _updateTimer?.Stop();
            Logger.LogInformation("Update timer stopped due to component disposal");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating time display");
        }
    }

    private async void OnRefreshTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await RefreshMissionData();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in refresh timer callback");
        }
    }

    private async Task RefreshMissionData()
    {
        try
        {
            // Set loading state but only update the loading indicator, not the whole page
            IsLoading = true;
            await InvokeAsync(() => StateHasChanged());

            // Use a more targeted approach to refresh mission data
            // Process one player at a time with minimal UI updates
            await LoadMissionDataForPlayer(_kingFridayPlayer);
            await LoadMissionDataForPlayer(_kingSaturdayPlayer);
            await LoadMissionDataForPlayer(_kingSundayPlayer);
            await LoadMissionDataForPlayer(_kingMondayPlayer);

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();

            // Update loading state and refresh UI once at the end
            IsLoading = false;
            await InvokeAsync(() => StateHasChanged());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing mission data");
            IsLoading = false;
            await InvokeAsync(() => StateHasChanged());
        }
    }

    private async void OnPlayerDataRefreshed(string playerName, DashboardPlayer updatedPlayer)
    {
        try
        {
            // Update the appropriate player object based on the player name
            switch (playerName)
            {
                case "King Friday!":
                    _kingFridayPlayer = updatedPlayer;
                    await LoadMissionDataForPlayer(_kingFridayPlayer);
                    break;
                case "King Saturday!":
                    _kingSaturdayPlayer = updatedPlayer;
                    await LoadMissionDataForPlayer(_kingSaturdayPlayer);
                    break;
                case "King Sunday!":
                    _kingSundayPlayer = updatedPlayer;
                    await LoadMissionDataForPlayer(_kingSundayPlayer);
                    break;
                case "King Monday!":
                    _kingMondayPlayer = updatedPlayer;
                    await LoadMissionDataForPlayer(_kingMondayPlayer);
                    break;
            }

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();

            // Update UI
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling player data refresh for {PlayerName}", playerName);
        }
    }

    private async Task InitialDataLoad()
    {
        try
        {
            // Load each player using PlayerCardService's InitialDataLoad method
            _kingFridayPlayer = await PlayerCardService.InitialDataLoad("King Friday!");
            _kingSaturdayPlayer = await PlayerCardService.InitialDataLoad("King Saturday!");
            _kingSundayPlayer = await PlayerCardService.InitialDataLoad("King Sunday!");
            _kingMondayPlayer = await PlayerCardService.InitialDataLoad("King Monday!");

            // Load mission data for each player
            await LoadMissionDataForPlayer(_kingFridayPlayer);
            await LoadMissionDataForPlayer(_kingSaturdayPlayer);
            await LoadMissionDataForPlayer(_kingSundayPlayer);
            await LoadMissionDataForPlayer(_kingMondayPlayer);

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initial data load");
        }
    }

    private async Task LoadMissionDataForPlayer(DashboardPlayer player)
    {
        if (player?.Player == null)
        {
            Logger.LogWarning("Player or Player.Player is null");
            return;
        }

        try
        {
            Logger.LogInformation("Loading mission data for player {PlayerName} with EID {PlayerEid}",
                player.Player.PlayerName, player.Player.EID);

            // Use the extension method to load mission data
            await player.LoadRocketMissionDataAsync(RocketMissionService);

            Logger.LogInformation("Loaded {MissionCount} missions for player {PlayerName}",
                player.Missions?.Count ?? 0, player.Player.PlayerName);

            // Log details of each mission
            if (player.Missions != null && player.Missions.Count > 0)
            {
                foreach (var mission in player.Missions)
                {
                    Logger.LogInformation("Mission in player object for {PlayerName}: Ship={Ship}, Status={Status}, Level={Level}",
                        player.Player.PlayerName, mission.Ship, mission.Status, mission.Level);
                }
            }
            else
            {
                Logger.LogWarning("No missions found for player {PlayerName}", player.Player.PlayerName);
            }

            // Store mission data in DashboardState
            DashboardState.SetPlayerMissions(player.Player.PlayerName, player.Missions != null ? player.Missions : new List<JsonPlayerExtendedMissionInfo>());
            DashboardState.SetPlayerStandbyMission(player.Player.PlayerName, player.StandbyMission);

            Logger.LogInformation("Stored mission data in DashboardState for player {PlayerName}", player.Player.PlayerName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading mission data for player {PlayerName}", player.Player.PlayerName);
        }
    }

    private string GetTimeSinceLastUpdate()
    {
        if (_lastUpdated == default)
        {
            return "Never";
        }

        var timeSince = DateTime.Now - _lastUpdated;

        if (timeSince.TotalSeconds < 60)
        {
            return $"{(int)timeSince.TotalSeconds} seconds ago";
        }
        else if (timeSince.TotalMinutes < 60)
        {
            return $"{(int)timeSince.TotalMinutes} minutes ago";
        }
        else if (timeSince.TotalHours < 24)
        {
            return $"{(int)timeSince.TotalHours} hours ago";
        }
        else
        {
            return $"{(int)timeSince.TotalDays} days ago";
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            Dispose();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during async disposal of RocketDashboard component");
        }
    }

    public void Dispose()
    {
        try
        {
            // Stop and dispose the refresh timer
            if (_refreshTimer != null)
            {
                _refreshTimer.Elapsed -= OnRefreshTimerElapsed;
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }

            // Stop and dispose the update timer
            if (_updateTimer != null)
            {
                _updateTimer.Elapsed -= OnUpdateTimerElapsed;
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // Unsubscribe from PlayerCardService events
            if (PlayerCardService != null)
            {
                PlayerCardService.OnPlayerDataRefreshed -= OnPlayerDataRefreshed;
            }

            // Clear references to help GC
            _kingFridayPlayer = null!;
            _kingSaturdayPlayer = null!;
            _kingSundayPlayer = null!;
            _kingMondayPlayer = null!;

            Logger.LogInformation("RocketDashboard component disposed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing RocketDashboard component");
        }
    }
}
