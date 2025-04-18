// Updated namespace for Blazor Server project
namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System.Globalization;

// Updated namespace for Blazor Server services
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain; // Keep Domain for potential future use

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging; // Added missing using for ILogger

using MudBlazor;

using ST = System.Timers;

// Ensure the partial class matches the file name and namespace
public partial class MajPlayerUpdates : IDisposable
{
    // Removed IApiService injection
    // [Inject]
    // private HemSoft.EggIncTracker.Dashboard.BlazorServer.Services.IApiService ApiService { get; set; } = default!;

    [Inject]
    private ILogger<MajPlayerUpdates> Logger { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!; // Uses updated namespace

    private List<MajPlayerRankingDto> playerUpdates = new();
    private bool isLoading = true;
    private string searchString = string.Empty;
    private ST.Timer? _updateTimer; // Timer for updating the current time
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";

    protected override async Task OnInitializedAsync()
    {
        await LoadPlayerUpdates();
        SetupUpdateTimer();
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
            _updateTimer = new ST.Timer(1000);
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

    private async void OnUpdateTimerElapsed(object? sender, ST.ElapsedEventArgs e)
    {
        try
        {
            // Only update if we have a valid last updated time
            if (_lastUpdated != default)
            {
                try
                {
                    // Use InvokeAsync for UI updates from timer threads
                    await InvokeAsync(() =>
                    {
                        try
                        {
                            // Update the time since last update
                            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                            StateHasChanged();
                        }
                        catch (Exception innerEx)
                        {
                            // Log but don't rethrow to prevent timer from stopping
                            Logger.LogError(innerEx, "Error in UI update");
                        }
                    });
                }
                catch (ObjectDisposedException)
                {
                    // Component might have been disposed, stop the timer
                    _updateTimer?.Stop();
                    Logger.LogInformation("Update timer stopped due to component disposal");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in update timer");
            // Don't rethrow to prevent timer from stopping
        }
    }

    private string GetTimeSinceLastUpdate()
    {
        var timeSince = DateTime.Now - _lastUpdated;

        if (timeSince.TotalHours >= 1)
        {
            return $"{(int)timeSince.TotalHours}h {timeSince.Minutes}m ago";
        }
        else if (timeSince.TotalMinutes >= 1)
        {
            return $"{(int)timeSince.TotalMinutes}m {timeSince.Seconds}s ago";
        }
        else
        {
            return $"{timeSince.Seconds}s ago";
        }
    }

    private async Task LoadPlayerUpdates()
    {
        isLoading = true;
        await InvokeAsync(StateHasChanged); // Ensure UI update on UI thread

        try
        {
            Logger.LogInformation("Fetching player updates from Domain Manager...");
            // --- Use static MajPlayerRankingManager ---
            var rankings = await MajPlayerRankingManager.GetLatestMajPlayerRankingsAsync(100, Logger); // Pass logger
            // ----------------------------------------

            if (rankings != null)
            {
                Logger.LogInformation($"Received {rankings.Count} player updates from API");

                // Process the data more efficiently - avoid LINQ in loops
                // Group by player name once and take the most recent entry for each player
                var latestByPlayer = rankings
                    .GroupBy(r => r.IGN)
                    .Select(g => g.OrderByDescending(r => r.Updated).First())
                    .ToList();

                playerUpdates = latestByPlayer;

                // Debug: Check if we have King Friday data - do this once outside of loops
                var kingFridayEntry = latestByPlayer.FirstOrDefault(r => r.IGN != null && r.IGN.Contains("King Friday"));
                if (kingFridayEntry != null)
                {
                    Logger.LogInformation($"King Friday latest entry: {kingFridayEntry.Updated}, SE: {kingFridayEntry.SEString}, SEGainsWeek: {kingFridayEntry.SEGainsWeek}");
                }
            }
            else
            {
                Logger.LogWarning("API returned null for player updates");
                playerUpdates = new List<MajPlayerRankingDto>();
            }

            // Update the last updated time
            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player updates");
            playerUpdates = new List<MajPlayerRankingDto>(); // Ensure list is initialized on error
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged); // Ensure UI update on UI thread
        }
    }

    // Filter function for the MudTable
    private bool FilterFunc(MajPlayerRankingDto player)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;

        // Check if either IGN or DiscordName contains the search string (case-insensitive)
        bool matchesIGN = player.IGN?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;
        bool matchesDiscord = player.DiscordName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;

        // Log when we find a match for debugging
        if (matchesIGN || matchesDiscord)
        {
            // Logger.LogInformation($"Filter match found: {player.IGN} (Discord: {player.DiscordName})"); // Reduce log noise
        }

        return matchesIGN || matchesDiscord;
    }

    public void Dispose()
    {
        // Clean up the timer when the component is disposed
        try
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Elapsed -= OnUpdateTimerElapsed; // Remove the event handler
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // Remove any event handlers from shared state if subscribed
            // DashboardState?.OnChange -= StateHasChanged; // Example if it were subscribed

            Logger.LogInformation("MajPlayerUpdates component disposed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing MajPlayerUpdates component");
        }
    }
}
