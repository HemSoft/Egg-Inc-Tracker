namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;
// using HemSoft.EggIncTracker.Data; // Removed Utils using
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public partial class PlayerDetail
{
    [Parameter]
    public string EID { get; set; } = string.Empty;

    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private PlayerDataService PlayerDataService { get; set; } = default!; // Inject the new service

    [Inject]
    private ILogger<PlayerDetail> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private PlayerDto? _player;
    private PlayerStatsDto? _playerStats;
    private MajPlayerRankingDto _SEGoalBegin = new();
    private MajPlayerRankingDto _SEGoalEnd = new();
    private MajPlayerRankingDto _EBGoalBegin = new();
    private MajPlayerRankingDto _EBGoalEnd = new();
    private MajPlayerRankingDto _MERGoalBegin = new();
    private MajPlayerRankingDto _MERGoalEnd = new();
    private MajPlayerRankingDto _JERGoalBegin = new();
    private MajPlayerRankingDto _JERGoalEnd = new();
    private PlayerGoalDto? _playerGoals; // Added field for player goals
    private string _playerSEThisWeek = string.Empty;
    private int _daysToNextTitle = 0;
    private bool isLoading = true;
    private const int NameCutOff = 10;

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
        ShowLabels = true, // Keep labels enabled
        ShowLegend = false,
        ShowLegendLabels = false,
        ShowToolTips = false // Disabled tooltips again
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
            StateHasChanged();
        }
    }

    private async Task ManualRefresh()
    {
        Logger.LogInformation("Manual refresh requested");
        await LoadPlayerData();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (EID != _player?.EID)
        {
            await LoadPlayerData();
        }
    }

    private async Task LoadPlayerData(int retryCount = 0)
    {
        const int maxRetries = 2; // Maximum number of retries
        isLoading = true;
        StateHasChanged();

        try
        {
            Logger.LogInformation($"Loading player data for EID: {EID} (Attempt {retryCount + 1})");

            // Get player by EID
            _player = await ApiService.GetPlayerByEIDAsync(EID);

            if (_player == null)
            {
                Logger.LogWarning($"Player with EID {EID} not found");

                // Retry logic for player data
                if (retryCount < maxRetries)
                {
                    Logger.LogInformation($"Retrying player data load (Attempt {retryCount + 2})");
                    isLoading = false;
                    StateHasChanged();
                    await Task.Delay(500); // Wait before retry
                    await LoadPlayerData(retryCount + 1);
                    return;
                }

                isLoading = false;
                StateHasChanged();
                return;
            }

            Logger.LogInformation($"Player found: {_player.PlayerName}");

            // Get player stats
            _playerStats = await ApiService.GetRankedPlayerAsync(_player.PlayerName, 1, 30);

            if (_playerStats == null && retryCount < maxRetries)
            {
                Logger.LogWarning("Player stats not found, retrying...");
                await Task.Delay(500); // Wait before retry
                await LoadPlayerData(retryCount + 1);
                return;
            }

            // Calculate SE This Week
            await CalculateSEThisWeek();

            // Calculate Prestiges Today/Week using the service
            if (_player != null)
            {
                var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_player);
                _player.PrestigesToday = prestigesToday;
                _player.PrestigesThisWeek = prestigesThisWeek;
            }


            // Calculate days to next title
            CalculateDaysToNextTitle();

            // Get player goals
            try
            {
                _playerGoals = await ApiService.GetPlayerGoalsAsync(_player?.PlayerName ?? string.Empty);
                Logger.LogInformation($"Retrieved goals for {_player?.PlayerName ?? "unknown"}: DailyPrestigeGoal={_playerGoals?.DailyPrestigeGoal}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error getting goals for player {_player?.PlayerName}");
                _playerGoals = null; // Ensure it's null if fetching fails
            }

            // Load player history for the chart
            await LoadPlayerHistory();

            // Update goal data using ApiService
            try
            {
                 Logger.LogInformation("Fetching surrounding player data for {PlayerName}", _player?.PlayerName ?? "unknown");
                var sePlayers = _player != null ? await ApiService.GetSurroundingSEPlayersAsync(_player.PlayerName, _player.SoulEggs) : null;
                var ebPlayers = _player != null ? await ApiService.GetSurroundingEBPlayersAsync(_player.PlayerName, _player.EarningsBonusPercentage) : null;
                var merPlayers = _player != null ? await ApiService.GetSurroundingMERPlayersAsync(_player.PlayerName, (decimal)_player.MER) : null;
                var jerPlayers = _player != null ? await ApiService.GetSurroundingJERPlayersAsync(_player.PlayerName, (decimal)_player.JER) : null;

                // Set the goal data from the API responses
                _SEGoalBegin = sePlayers?.LowerPlayer ?? new MajPlayerRankingDto(); // Use empty DTO as fallback
                _SEGoalEnd = sePlayers?.UpperPlayer ?? new MajPlayerRankingDto();
                _EBGoalBegin = ebPlayers?.LowerPlayer ?? new MajPlayerRankingDto();
                _EBGoalEnd = ebPlayers?.UpperPlayer ?? new MajPlayerRankingDto();
                _MERGoalBegin = merPlayers?.LowerPlayer ?? new MajPlayerRankingDto();
                _MERGoalEnd = merPlayers?.UpperPlayer ?? new MajPlayerRankingDto();
                _JERGoalBegin = jerPlayers?.LowerPlayer ?? new MajPlayerRankingDto();
                _JERGoalEnd = jerPlayers?.UpperPlayer ?? new MajPlayerRankingDto();
                 Logger.LogInformation("Surrounding player data fetched. SE: {SEBegin} -> {SEEnd}, EB: {EBBegin} -> {EBEnd}",
                    _SEGoalBegin.IGN, _SEGoalEnd.IGN, _EBGoalBegin.IGN, _EBGoalEnd.IGN);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting surrounding players data");
                // Continue even if this fails - it's not critical
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
                await LoadPlayerData(retryCount + 1);
                return;
            }
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task CalculateSEThisWeek()
    {
        // Calculate SE This Week using the service
        if (_player != null)
        {
            _playerSEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_player);
        }
        else
        {
            _playerSEThisWeek = "0"; // Default if player is null
        }
    }

    // Keep method signature, call service for implementation
    private double CalculateProgressPercentage(string? current, string? target, string? previous)
    {
        return PlayerDataService.CalculateProgressPercentage(current, target, previous);
    }

    // ParseBigNumber is now internal to PlayerDataService and not needed here

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/");
    }

    private void CalculateDaysToNextTitle()
    {
        try
        {
            if (_player != null && _playerStats != null && !string.IsNullOrEmpty(_playerStats.ProjectedTitleChange))
            {
                // Parse the projected title change date
                if (DateTime.TryParse(_playerStats.ProjectedTitleChange, out DateTime projectedDate))
                {
                    // Calculate days until that date
                    var daysUntil = (projectedDate - DateTime.Now).Days;
                    _daysToNextTitle = Math.Max(0, daysUntil); // Ensure it's not negative
                    Logger.LogInformation($"Days to next title: {_daysToNextTitle}");
                }
                else
                {
                    _daysToNextTitle = 0;
                    Logger.LogWarning($"Could not parse projected title change date: {_playerStats.ProjectedTitleChange}");
                }
            }
            else
            {
                _daysToNextTitle = 0;
                Logger.LogWarning("Missing player data for title projection calculation");
            }
        }
        catch (Exception ex)
        {
            _daysToNextTitle = 0;
            Logger.LogError(ex, "Error calculating days to next title");
        }
    }

    /// <summary>
    /// Get the CSS color string based on the progress percentage
    /// </summary>
    /// <param name="actual">The actual value</param>
    /// <param name="goal">The goal value</param>
    /// <returns>A CSS color string</returns>
    // Keep method signature, call service for implementation
    private string GetProgressColorStyle(int? actual, int goal)
    {
        return PlayerDataService.GetProgressColorStyle(actual, goal);
    }

    private async Task LoadPlayerHistory()
    {
        if (_player == null || string.IsNullOrEmpty(_player.PlayerName))
        {
            _playerHistorySeries = new();
            _playerHistoryLabels = Array.Empty<string>();
            return;
        }

        try
        {
            Logger.LogInformation("Loading player history for {PlayerName} for the last {Days} days", _player.PlayerName, _selectedHistoryDays);
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-_selectedHistoryDays);

            var historicalData = await ApiService.GetPlayerHistoryAsync(
                _player.PlayerName,
                startDate,
                endDate);

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
                 var parsedData = groupedByDay
                    .Select(p => PlayerDataService.ParseBigNumber(p.SoulEggsFull)) // Changed to use SoulEggsFull
                    .ToArray();

                // --- Reverted Log10 calculation ---
                _playerHistoryData = parsedData;
                // --- End Revert ---

                _playerHistorySeries = new List<ChartSeries>
                {
                    new ChartSeries { Name = "Soul Eggs", Data = _playerHistoryData } // Reverted series name
                };
                 Logger.LogInformation("Loaded {Count} historical data points for chart", _playerHistoryData.Length);
            }
            else
            {
                 Logger.LogWarning("No historical data found for {PlayerName}", _player.PlayerName);
                _playerHistorySeries = new();
                _playerHistoryLabels = Array.Empty<string>();
                _playerHistoryData = Array.Empty<double>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player history data for {PlayerName}", _player.PlayerName);
            _playerHistorySeries = new(); // Clear chart on error
            _playerHistoryLabels = Array.Empty<string>();
            _playerHistoryData = Array.Empty<double>();
        }
        StateHasChanged(); // Update UI after loading history
    }

     // Handle the slider value change for history days
    private async void OnHistoryDaysSliderChanged(int value)
    {
        _selectedHistoryDays = value;
        await LoadPlayerHistory(); // Reload history data with new range
    }
}
