namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System.Timers;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Data.Dtos; // Ensure DTOs using is present
using HemSoft.EggIncTracker.Domain; // Add Domain using for static managers

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging; // Added missing using for ILogger

using MudBlazor;

// Ensure the partial class matches the file name and namespace
public partial class PlayerDetail : IDisposable
{
    private readonly System.Timers.Timer? _historyTimer;

    public void Dispose()
    {
        // Dispose of any resources
        _historyTimer?.Dispose();

        // Unsubscribe from PlayerCardService events
        if (PlayerCardService != null)
        {
            PlayerCardService.OnPlayerDataRefreshed -= OnPlayerDataRefreshed;
        }

        // Suppress finalization
        GC.SuppressFinalize(this);
    }
    [Parameter]
    public string EID { get; set; } = string.Empty;

    [Inject]
    private PlayerDataService PlayerDataService { get; set; } = default!;

    [Inject]
    private PlayerCardService PlayerCardService { get; set; } = default!;

    [Inject]
    private ILogger<PlayerDetail> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private DashboardPlayer _dashboardPlayer = new();
    private bool isLoading = true;
    private PlayerDto? Player => _dashboardPlayer.Player;

    // Fields for the history chart
    private List<ChartSeries> _playerHistorySeries = [];
    private string[] _playerHistoryLabels = Array.Empty<string>();
    private double[] _playerHistoryData = Array.Empty<double>();
    private readonly ChartOptions _historyChartOptions = new()
    {
        ChartPalette = new[] { "#4CAF50" },
        InterpolationOption = InterpolationOption.Straight,
        YAxisFormat = "E",
        XAxisLines = true,
        YAxisLines = true,
        YAxisTicks = 7
    };
    private int _selectedHistoryDays = 14;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await Task.Delay(300);
            Logger.LogInformation("Starting initial player data load");
            await LoadPlayerData();

            // Subscribe to player data refresh events
            PlayerCardService.OnPlayerDataRefreshed += OnPlayerDataRefreshed;

            // Set up auto-refresh timer
            PlayerCardService.SetupAutoRefreshTimer();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during player detail initialization");
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ManualRefresh()
    {
        Logger.LogInformation("Manual refresh requested");
        await LoadPlayerData();
    }

    private async void OnPlayerDataRefreshed(string playerName, DashboardPlayer updatedPlayer)
    {
        try
        {
            // Only update if this is the player we're viewing
            if (_dashboardPlayer?.Player?.PlayerName == playerName)
            {
                _dashboardPlayer = updatedPlayer;
                await InvokeAsync(StateHasChanged);
                Logger.LogInformation("Player data updated from refresh event");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling player data refresh for {PlayerName}", playerName);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // Check if the EID parameter has changed
        if (EID != Player?.EID)
        {
            Logger.LogInformation("EID parameter changed, reloading player data.");
            await LoadPlayerData();
        }
    }

    private async Task LoadPlayerData(int retryCount = 0)
    {
        const int maxRetries = 2;
        isLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Logger.LogInformation("Loading player data for EID: {EID} (Attempt {RetryCount})", EID, retryCount + 1);

            // First get the player by EID
            var player = await PlayerManager.GetPlayerByEIDAsync(EID, Logger);

            if (player == null)
            {
                Logger.LogWarning("Player with EID {EID} not found", EID);

                // Retry logic for player data
                if (retryCount < maxRetries)
                {
                    Logger.LogInformation("Retrying player data load (Attempt {RetryCount})", retryCount + 2);
                    isLoading = false;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(500);
                    await LoadPlayerData(retryCount + 1);
                    return;
                }

                // Max retries reached
                isLoading = false;
                await InvokeAsync(StateHasChanged);
                return; // Exit if player not found after retries
            }

            Logger.LogInformation("Player found: {PlayerName}", player.PlayerName);

            // Now use PlayerCardService to get the full dashboard player data
            _dashboardPlayer = await PlayerCardService.GetDashboardPlayer(player.PlayerName);

            // Load player history for the chart
            await LoadPlayerHistory();

            Logger.LogInformation("Player data loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player data for EID {EID}", EID);

            // Retry on exception
            if (retryCount < maxRetries)
            {
                Logger.LogInformation("Retrying after error (Attempt {RetryCount})", retryCount + 2);
                await Task.Delay(1000); // Longer wait after error
                await LoadPlayerData(retryCount + 1); // Recursive call for retry
                return; // Exit current attempt after retry initiated
            }
            // Max retries reached after exception
            _dashboardPlayer.Player = null; // Ensure player is null on final failure
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged); // Ensure UI update on UI thread
        }
    }

    // Removed CalculateSEThisWeek method - now handled by PlayerCard.GetDashboardPlayer

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/");
    }

    // Removed CalculateDaysToNextTitle method - now in PlayerCard.razor.cs

    private async Task LoadPlayerHistory()
    {
        if (_dashboardPlayer.Player == null || string.IsNullOrEmpty(_dashboardPlayer.Player.PlayerName))
        {
            _playerHistorySeries = new();
            _playerHistoryLabels = Array.Empty<string>();
            _playerHistoryData = Array.Empty<double>(); // Initialize data array
            return;
        }

        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-_selectedHistoryDays);
            var historicalData = await PlayerManager.GetPlayerHistoryAsync(_dashboardPlayer.Player.PlayerName, startDate, Logger);

            if (historicalData?.Count > 0)
            {
                var sortedData = historicalData.OrderBy(p => p.Updated).ToList();

                // Group data by day and take only the last entry for each day
                var groupedByDay = sortedData
                    .GroupBy(p => p.Updated.Date)
                    .Select(g => g.OrderByDescending(p => p.Updated).First())
                    .OrderBy(p => p.Updated)
                    .ToList();

                _playerHistoryLabels = groupedByDay
                    .Select(p => p.Updated.ToString("MM/dd"))
                    .ToArray();

                // Use PlayerDataService's ParseBigNumber with the full SE string for chart data
                // This might be okay if PlayerDataService is refactored
                var parsedData = groupedByDay
                   .Select(p => PlayerDataService.ParseBigNumber(p.SoulEggsFull))
                   .ToArray();

                _playerHistoryData = parsedData; // Assign parsed data
                _playerHistorySeries = new List<ChartSeries>
                {
                    new() { Name = "Soul Eggs", Data = _playerHistoryData }
                };
                Logger.LogInformation("Loaded {Count} historical data points for chart", _playerHistoryData.Length);
            }
            else
            {
                Logger.LogWarning("No historical data found for {PlayerName}", _dashboardPlayer.Player.PlayerName);
                _playerHistorySeries = new();
                _playerHistoryLabels = Array.Empty<string>();
                _playerHistoryData = Array.Empty<double>(); // Clear data array
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player history data for {PlayerName}", _dashboardPlayer.Player.PlayerName);
            _playerHistorySeries = new(); // Clear chart on error
            _playerHistoryLabels = Array.Empty<string>();
            _playerHistoryData = Array.Empty<double>(); // Clear data array
        }
        await InvokeAsync(StateHasChanged); // Update UI after loading history
    }

    // Handle the slider value change for history days
    private async void OnHistoryDaysSliderChanged(int value)
    {
        _selectedHistoryDays = value;
        await LoadPlayerHistory(); // Reload history data with new range
    }
}
