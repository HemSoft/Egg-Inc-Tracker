// Updated namespace for Blazor Server project
namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System.Globalization;
using System.Numerics; // Added for BigInteger

// Updated namespace for Blazor Server services
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Data.Dtos; // Ensure DTOs using is present
using HemSoft.EggIncTracker.Domain; // Add Domain using for static managers

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging; // Added missing using for ILogger

using MudBlazor;

// Ensure the partial class matches the file name and namespace
public partial class PlayerDetail
{
    [Parameter]
    public string EID { get; set; } = string.Empty;

    // Removed IApiService injection
    // [Inject]
    // private HemSoft.EggIncTracker.Dashboard.BlazorServer.Services.IApiService ApiService { get; set; } = default!;

    [Inject]
    private PlayerDataService PlayerDataService { get; set; } = default!; // Uses updated namespace

    [Inject]
    private ILogger<PlayerDetail> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private DashboardPlayer _dashboardPlayer = new DashboardPlayer();
    private bool isLoading = true;
    private const int NameCutOff = 10;

    // Properties to access the DashboardPlayer data for easier reference in the code
    private PlayerDto? _player => _dashboardPlayer.Player;
    private PlayerStatsDto? _playerStats => _dashboardPlayer.Stats;

    // Fields for the history chart
    private List<ChartSeries> _playerHistorySeries = new();
    private string[] _playerHistoryLabels = Array.Empty<string>();
    private double[] _playerHistoryData = Array.Empty<double>();
    private readonly ChartOptions _historyChartOptions = new()
    {
        ChartPalette = new[] { "#4CAF50" }, // Green color for Soul Eggs
        InterpolationOption = InterpolationOption.Straight,
        YAxisFormat = "E", // Revert to Scientific Notation
        XAxisLines = true,
        YAxisLines = true,
        YAxisTicks = 7, // Try 7 ticks
        // ShowLabels = true, // Commented out - might not exist
        // ShowLegend = false, // Commented out - might not exist
        // ShowLegendLabels = false, // Commented out - might not exist
        // ShowToolTips = false // Commented out - might not exist
    };
    private int _selectedHistoryDays = 14; // Default days for history chart


    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Initial load with a delay to ensure all dependencies are initialized
            await Task.Delay(300); // Longer delay to allow for initialization
            Logger.LogInformation("Starting initial player data load");
            await LoadPlayerData();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during player detail initialization");
            // We'll show the error state in the UI
            isLoading = false;
            await InvokeAsync(StateHasChanged); // Ensure UI update on UI thread
        }
    }

    private async Task ManualRefresh()
    {
        Logger.LogInformation("Manual refresh requested");
        await LoadPlayerData();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Check if the EID parameter has changed
        if (EID != _player?.EID)
        {
            Logger.LogInformation("EID parameter changed, reloading player data.");
            await LoadPlayerData();
        }
    }

    private async Task LoadPlayerData(int retryCount = 0)
    {
        const int maxRetries = 2; // Maximum number of retries
        isLoading = true;
        await InvokeAsync(StateHasChanged); // Ensure UI update on UI thread

        try
        {
            Logger.LogInformation($"Loading player data for EID: {EID} (Attempt {retryCount + 1})");

            // --- Use PlayerManager ---
            _dashboardPlayer.Player = await PlayerManager.GetPlayerByEIDAsync(EID, Logger); // Use EID lookup
            // -------------------------

            if (_dashboardPlayer.Player == null)
            {
                Logger.LogWarning($"Player with EID {EID} not found");

                // Retry logic for player data
                if (retryCount < maxRetries)
                {
                    Logger.LogInformation($"Retrying player data load (Attempt {retryCount + 2})");
                    isLoading = false; // Temporarily set loading false for retry delay
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(500); // Wait before retry
                    await LoadPlayerData(retryCount + 1); // Recursive call for retry
                    return; // Exit current attempt after retry initiated
                }

                // Max retries reached
                isLoading = false;
                await InvokeAsync(StateHasChanged);
                return; // Exit if player not found after retries
            }

            Logger.LogInformation($"Player found: {_dashboardPlayer.Player.PlayerName}");
            await InvokeAsync(StateHasChanged); // Force UI update after player is loaded

            // --- Use PlayerManager ---
            _dashboardPlayer.Stats = await PlayerManager.GetRankedPlayersAsync(_dashboardPlayer.Player.PlayerName, 1, 30, Logger); // Pass Logger
            // -------------------------

            if (_dashboardPlayer.Stats == null && retryCount < maxRetries)
            {
                Logger.LogWarning("Player stats not found, retrying...");
                await Task.Delay(500); // Wait before retry
                await LoadPlayerData(retryCount + 1); // Retry loading everything for consistency
                return;
            }

            // Calculate SE This Week (might be okay if PlayerDataService is refactored)
            await CalculateSEThisWeek();
            await InvokeAsync(StateHasChanged); // Force UI update after SE this week is calculated

            // Calculate Prestiges Today/Week (might be okay if PlayerDataService is refactored)
            if (_dashboardPlayer.Player != null)
            {
                var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_dashboardPlayer.Player);
                _dashboardPlayer.Player.PrestigesToday = prestigesToday;
                _dashboardPlayer.Player.PrestigesThisWeek = prestigesThisWeek;
            }

            // Calculate days to next title (uses _playerStats)
            CalculateDaysToNextTitle();

            // Get player goals
            try
            {
                // --- Use GoalManager ---
                _dashboardPlayer.Goals = await GoalManager.GetPlayerGoalsAsync(_dashboardPlayer.Player?.PlayerName ?? string.Empty, Logger); // Pass Logger
                                                                                                                                             // -----------------------
                Logger.LogInformation($"Retrieved goals for {_dashboardPlayer.Player?.PlayerName ?? "unknown"}: DailyPrestigeGoal={_dashboardPlayer.Goals?.DailyPrestigeGoal}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error getting goals for player {_dashboardPlayer.Player?.PlayerName}");
                _dashboardPlayer.Goals = null; // Ensure it's null if fetching fails
            }

            // Load player history for the chart
            await LoadPlayerHistory();

            // Update goal data using MajPlayerRankingManager
            try
            {
                Logger.LogInformation("Fetching surrounding player data for {PlayerName}", _dashboardPlayer.Player?.PlayerName ?? "unknown");
                // --- Use MajPlayerRankingManager ---
                var sePlayers = _dashboardPlayer.Player != null ? await MajPlayerRankingManager.GetSurroundingSEPlayersAsync(_dashboardPlayer.Player.PlayerName, _dashboardPlayer.Player.SoulEggs, Logger) : null; // Pass Logger
                var ebPlayers = _dashboardPlayer.Player != null ? await MajPlayerRankingManager.GetSurroundingEBPlayersAsync(_dashboardPlayer.Player.PlayerName, _dashboardPlayer.Player.EarningsBonusPercentage, Logger) : null; // Pass Logger
                var merPlayers = _dashboardPlayer.Player != null ? await MajPlayerRankingManager.GetSurroundingMERPlayersAsync(_dashboardPlayer.Player.PlayerName, (decimal)(_dashboardPlayer.Player.MER), Logger) : null; // Handle null
                var jerPlayers = _dashboardPlayer.Player != null ? await MajPlayerRankingManager.GetSurroundingJERPlayersAsync(_dashboardPlayer.Player.PlayerName, (decimal)(_dashboardPlayer.Player.JER), Logger) : null; // Handle null
                                                                                                                                                                                                                           // ---------------------------------

                // Set the goal data from the API responses (with null checks)
                // Commented out Ranking properties as they might not exist on the DTO
                _dashboardPlayer.SEGoalBegin = sePlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower SE", SEString = "0s" /*, Ranking = (dashboardPlayer.Stats?.SERanking ?? 0) + 1 */ };
                _dashboardPlayer.SEGoalEnd = sePlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper SE", SEString = _dashboardPlayer.Player?.SoulEggs ?? "0s" /*, Ranking = (dashboardPlayer.Stats?.SERanking ?? 1) - 1 */ };
                _dashboardPlayer.EBGoalBegin = ebPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" /*, Ranking = (dashboardPlayer.Stats?.EBRanking ?? 0) + 1 */ };
                _dashboardPlayer.EBGoalEnd = ebPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper EB", EBString = _dashboardPlayer.Player?.EarningsBonusPercentage ?? "0%" /*, Ranking = (dashboardPlayer.Stats?.EBRanking ?? 1) - 1 */ };

                // Corrected MER/JER fallback initialization - Use ternary operator
                _dashboardPlayer.MERGoalBegin = merPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m /*, Ranking = (dashboardPlayer.Stats?.MERRanking ?? 0) + 1 */ };
                _dashboardPlayer.MERGoalEnd = merPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper MER", MER = _dashboardPlayer.Player != null ? (decimal)_dashboardPlayer.Player.MER : 0m /*, Ranking = (dashboardPlayer.Stats?.MERRanking ?? 1) - 1 */ }; // Ternary

                _dashboardPlayer.JERGoalBegin = jerPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m /*, Ranking = (dashboardPlayer.Stats?.JERRanking ?? 0) + 1 */ };
                _dashboardPlayer.JERGoalEnd = jerPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper JER", JER = _dashboardPlayer.Player != null ? (decimal)_dashboardPlayer.Player.JER : 0m /*, Ranking = (dashboardPlayer.Stats?.JERRanking ?? 1) - 1 */ }; // Ternary

                Logger.LogInformation("Surrounding player data fetched. SE: {SEBegin} -> {SEEnd}, EB: {EBBegin} -> {EBEnd}",
                   _dashboardPlayer.SEGoalBegin.IGN, _dashboardPlayer.SEGoalEnd.IGN, _dashboardPlayer.EBGoalBegin.IGN, _dashboardPlayer.EBGoalEnd.IGN);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting surrounding players data");
                // Initialize goals to empty DTOs if API fails
                _dashboardPlayer.SEGoalBegin = new MajPlayerRankingDto();
                _dashboardPlayer.SEGoalEnd = new MajPlayerRankingDto();
                _dashboardPlayer.EBGoalBegin = new MajPlayerRankingDto();
                _dashboardPlayer.EBGoalEnd = new MajPlayerRankingDto();
                _dashboardPlayer.MERGoalBegin = new MajPlayerRankingDto();
                _dashboardPlayer.MERGoalEnd = new MajPlayerRankingDto();
                _dashboardPlayer.JERGoalBegin = new MajPlayerRankingDto();
                _dashboardPlayer.JERGoalEnd = new MajPlayerRankingDto();
            }

            Logger.LogInformation("Player data loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error loading player data for EID {EID}");

            // Retry on exception
            if (retryCount < maxRetries)
            {
                Logger.LogInformation($"Retrying after error (Attempt {retryCount + 2})");
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

    private async Task CalculateSEThisWeek()
    {
        // Calculate SE This Week using the service (might be okay if refactored)
        if (_dashboardPlayer.Player != null)
        {
            _dashboardPlayer.SEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_dashboardPlayer.Player);
        }
        else
        {
            _dashboardPlayer.SEThisWeek = BigInteger.Zero; // Default if player is null
        }
    }

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/");
    }

    private void CalculateDaysToNextTitle()
    {
        try
        {
            // Use null-conditional operator for safety
            string? projectedDateString = _playerStats?.ProjectedTitleChange; // This property is DateTime, not string

            // Use the DateTime property directly
            if (_dashboardPlayer.Player != null && _dashboardPlayer.Player.ProjectedTitleChange != DateTime.MinValue)
            {
                var daysUntil = (_dashboardPlayer.Player.ProjectedTitleChange - DateTime.Now).Days;
                _dashboardPlayer.DaysToNextTitle = Math.Max(0, daysUntil); // Ensure it's not negative
                Logger.LogInformation($"Days to next title: {_dashboardPlayer.DaysToNextTitle}");
            }
            else
            {
                _dashboardPlayer.DaysToNextTitle = 0;
                Logger.LogWarning("Missing player data or projected date for title projection calculation");
            }
        }
        catch (Exception ex)
        {
            _dashboardPlayer.DaysToNextTitle = 0;
            Logger.LogError(ex, "Error calculating days to next title");
        }
    }

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

            if (historicalData?.Any() == true)
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
                    new ChartSeries { Name = "Soul Eggs", Data = _playerHistoryData }
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
