namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Extensions;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
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
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting up refresh timer");
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
            IsLoading = true;
            await InvokeAsync(StateHasChanged);

            // Refresh mission data for all players
            await LoadMissionDataForPlayer(_kingFridayPlayer);
            await LoadMissionDataForPlayer(_kingSaturdayPlayer);
            await LoadMissionDataForPlayer(_kingSundayPlayer);
            await LoadMissionDataForPlayer(_kingMondayPlayer);

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();

            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing mission data");
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
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
            return;
        }

        try
        {
            // Use the extension method to load mission data
            await player.LoadRocketMissionDataAsync(RocketMissionService);

            // Store mission data in DashboardState
            DashboardState.SetPlayerMissions(player.Player.PlayerName, player.Missions);
            DashboardState.SetPlayerStandbyMission(player.Player.PlayerName, player.StandbyMission);
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
        if (timeSince.TotalMinutes < 60)
        {
            return $"{(int)timeSince.TotalMinutes} minutes ago";
        }
        return $"{(int)timeSince.TotalHours} hours ago";
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
