using HemSoft.EggIncTracker.Domain;

namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
// Remove Domain reference and use only API service
// using HemSoft.EggIncTracker.Domain;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

using MudBlazor;

using ST = System.Timers;

public partial class Dashboard
{
    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!;

    [Inject]
    private EggDash.Client.Services.ApiService ApiService { get; set; } = default!;

    private const int NameCutOff = 12;

    private ST.Timer? _timer;
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";

    private double[] _kingFridayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingFridayTitleProgressLabels = new string[] { "", "" };

    private double[] _kingSaturdayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingSaturdayTitleProgressLabels = new string[] { "", "" };

    private double[] _kingSundayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingSundayTitleProgressLabels = new string[] { "", "" };

    private double[] _kingMondayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingMondayTitleProgressLabels = new string[] { "", "" };

    private PlayerDto? _kingFriday;
    private PlayerStatsDto? _kingFridayStats;
    private MajPlayerRankingDto _SEGoalBegin;
    private MajPlayerRankingDto _SEGoalEnd;
    private MajPlayerRankingDto _EBGoalBegin;
    private MajPlayerRankingDto _EBGoalEnd;
    private MajPlayerRankingDto _MERGoalBegin;
    private MajPlayerRankingDto _MERGoalEnd;
    private MajPlayerRankingDto _JERGoalBegin;
    private MajPlayerRankingDto _JERGoalEnd;

    private PlayerDto? _kingSaturday;
    private PlayerStatsDto? _kingSaturdayStats;

    private PlayerDto? _kingSunday;
    private PlayerStatsDto? _kingSundayStats;

    private PlayerDto? _kingMonday;
    private PlayerStatsDto? _kingMondayStats;

    private readonly ChartOptions _options = new()
    {
        ChartPalette = new[] { "#4CAF50", "#666666" }
    };

    private List<ChartSeries> _kingFridaySEHistorySeries = new List<ChartSeries>(Array.Empty<ChartSeries>());
    private string[] _kingFridaySEHistoryLabels = Array.Empty<string>();
    private double[] _kingFridaySEHistoryData = Array.Empty<double>();

    // New properties for the multi-series line chart
    private List<ChartSeries> _kingFridayMultiSeriesData = new();
    private string[] _kingFridayMultiSeriesLabels = Array.Empty<string>();
    private int _selectedDaysToDisplay = 7;
    private Dictionary<string, List<double>> _metricHistoryData = new();

    // Chart options with a custom palette for the Soul Eggs chart
    private readonly ChartOptions _multiSeriesChartOptions = new()
    {
        ChartPalette = new[] { "#4CAF50" }, // Green color for Soul Eggs
        InterpolationOption = InterpolationOption.Straight, // Use straight lines between points
        YAxisFormat = "0.##", // Format for Y-axis values
        XAxisLines = true, // Show X-axis grid lines
        YAxisLines = true, // Show Y-axis grid lines
        YAxisTicks = 1, // Increase number of ticks on Y-axis to show more granular values
        ShowLabels = true,
        ShowLegend = true,
        ShowLegendLabels = true,
        ShowToolTips = true
    };

    protected override async Task OnInitializedAsync()
    {
        // Register ApiService and Logger if they weren't registered globally
        // This ensures backward compatibility if the global registration fails
        if (ServiceLocator.GetService<IApiService>() == null)
        {
            ServiceLocator.RegisterService<IApiService>(ApiService);
        }

        if (ServiceLocator.GetService<ILogger>() == null)
        {
            ServiceLocator.RegisterService<ILogger>(Logger);
        }

        await RefreshData();

        _timer = new ST.Timer(30000); // 30 seconds
        _timer.Elapsed += async (sender, e) =>
        {
            try
            {
                // Ensure we're on the UI thread
                await InvokeAsync(async () =>
                {
                    await RefreshData();
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Timer refresh error");
            }
        };
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private async Task RefreshData()
    {
        try
        {
            // Fetch new data
            await UpdateKingFriday();
            await UpdateKingSaturday();
            await UpdateKingSunday();
            await UpdateKingMonday();
            await LoadKingFridaySEHistory();

            // Call the chart update method which now handles the API call directly
            await UpdateMultiSeriesChart();

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            // Update the shared state instead of local _lastUpdated
            DashboardState.SetLastUpdated(DateTime.Now);

            // StateHasChanged is now called on the UI thread
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing data");
        }
    }

    private async Task UpdateKingFriday()
    {
        // Get the UTC date boundaries that correspond to "today" and "start of week" in CST
        TimeZoneInfo cstZone;
        try
        {
            cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            cstZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }
        catch (InvalidTimeZoneException)
        {
            cstZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }

        // Get current date in CST
        var cstDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);
        var cstToday = cstDateTime.Date;

        // Convert CST today back to UTC for database comparison
        var todayUtcStart = TimeZoneInfo.ConvertTimeToUtc(cstToday, cstZone);

        // Calculate start of week in CST, then convert to UTC
        var cstStartOfWeek = cstToday.AddDays(-(int)cstToday.DayOfWeek + (int)DayOfWeek.Monday);
        if (cstToday.DayOfWeek == DayOfWeek.Sunday) // If today is Sunday, use last Monday
        {
            cstStartOfWeek = cstToday.AddDays(-6); // Go back 6 days to previous Monday
        }
        var startOfWeekUtc = TimeZoneInfo.ConvertTimeToUtc(cstStartOfWeek, cstZone);

        // Log for debugging purposes
        Logger.LogInformation($"Today (CST): {cstToday}, Start of Week (CST): {cstStartOfWeek}");
        Logger.LogInformation($"Today (UTC): {todayUtcStart}, Start of Week (UTC): {startOfWeekUtc}");

        _kingFriday = await ApiService.GetLatestPlayerAsync("King Friday!");
        // Replace direct call to PlayerManager with API service call
        _kingFridayStats = await ApiService.GetRankedPlayerAsync("King Friday!", 1, 30);

        if (_kingFriday != null)
        {
            // Update the player's last update time in DashboardState
            DashboardState.SetPlayerLastUpdated(_kingFriday.Updated);

            // Get player history for today
            var playerHistoryToday = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                todayUtcStart,
                DateTime.UtcNow);

            // Get player history for this week
            var playerHistoryWeek = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                startOfWeekUtc,
                DateTime.UtcNow);

            // If weekly data is empty or null but we have daily data, use daily data for weekly calculation
            if ((playerHistoryWeek == null || !playerHistoryWeek.Any()) && playerHistoryToday != null && playerHistoryToday.Any())
            {
                Logger.LogInformation("No weekly history found, using daily history for weekly calculation");
                playerHistoryWeek = playerHistoryToday;
            }

            var firstToday = playerHistoryToday?.OrderBy(x => x.Updated).FirstOrDefault();
            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

            // Log the data we're working with
            Logger.LogInformation($"Current prestiges: {_kingFriday.Prestiges}");
            Logger.LogInformation($"First today prestiges: {firstToday?.Prestiges}, timestamp: {firstToday?.Updated}");
            Logger.LogInformation($"First this week prestiges: {firstThisWeek?.Prestiges}, timestamp: {firstThisWeek?.Updated}");

            _kingFriday.PrestigesToday = _kingFriday.Prestiges - (firstToday?.Prestiges ?? _kingFriday.Prestiges);
            _kingFriday.PrestigesThisWeek = _kingFriday.Prestiges - (firstThisWeek?.Prestiges ?? _kingFriday.Prestiges);

            // Log calculated values
            Logger.LogInformation($"Calculated prestiges today: {_kingFriday.PrestigesToday}");
            Logger.LogInformation($"Calculated prestiges this week: {_kingFriday.PrestigesThisWeek}");

            // Ensure PrestigesThisWeek is at least equal to PrestigesToday and handle null values
            if (_kingFriday.PrestigesToday.HasValue)
            {
                // If PrestigesThisWeek is null or less than PrestigesToday, set it to PrestigesToday
                if (!_kingFriday.PrestigesThisWeek.HasValue || _kingFriday.PrestigesThisWeek.Value < _kingFriday.PrestigesToday.Value)
                {
                    _kingFriday.PrestigesThisWeek = _kingFriday.PrestigesToday;
                    Logger.LogInformation($"Applied fix for weekly prestiges: updated to {_kingFriday.PrestigesThisWeek}");
                }
            }

            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Friday!");
            if (titleInfo != null)
            {
                _kingFridayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingFridayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingFridayTitleProgressData = new double[] { _kingFriday.TitleProgress, 100 - _kingFriday.TitleProgress };
                _kingFridayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingFridayStats?.ProjectedTitleChange)),
                    _kingFriday.NextTitle
                };
            }

            // Update goal data from MajPlayerRankingManager
            (_SEGoalBegin, _SEGoalEnd) = await MajPlayerRankingManager.GetSurroundingSEPlayers("King Friday!", _kingFriday.SoulEggs);
            (_EBGoalBegin, _EBGoalEnd) = await MajPlayerRankingManager.GetSurroundingEBPlayers("King Friday!", _kingFriday.EarningsBonusPercentage);
            (_MERGoalBegin, _MERGoalEnd) = await MajPlayerRankingManager.GetSurroundingMERPlayers("King Friday!", (decimal)_kingFriday.MER);
            (_JERGoalBegin, _JERGoalEnd) = await MajPlayerRankingManager.GetSurroundingJERPlayers("King Friday!", (decimal)_kingFriday.JER);
        }
    }

    private async Task UpdateKingSaturday()
    {
        _kingSaturday = await ApiService.GetLatestPlayerAsync("King Saturday!");
        // Replace direct call to PlayerManager with API service call
        _kingSaturdayStats = await ApiService.GetRankedPlayerAsync("King Saturday!", 1, 30);

        if (_kingSaturday != null)
        {
            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Saturday!");
            if (titleInfo != null)
            {
                _kingSaturdayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingSaturdayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingSaturdayTitleProgressData = new double[] { _kingSaturday.TitleProgress, 100 - _kingSaturday.TitleProgress };
                _kingSaturdayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingSaturdayStats?.ProjectedTitleChange)),
                    _kingSaturday.NextTitle
                };
            }
        }
    }

    private async Task UpdateKingSunday()
    {
        _kingSunday = await ApiService.GetLatestPlayerAsync("King Sunday!");
        // Replace direct call to PlayerManager with API service call
        _kingSundayStats = await ApiService.GetRankedPlayerAsync("King Sunday!", 1, 30);

        if (_kingSunday != null)
        {
            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Sunday!");
            if (titleInfo != null)
            {
                _kingSundayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingSundayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingSundayTitleProgressData = new double[] { _kingSunday.TitleProgress, 100 - _kingSunday.TitleProgress };
                _kingSundayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingSundayStats?.ProjectedTitleChange)),
                    _kingSunday.NextTitle
                };
            }
        }
    }

    private async Task UpdateKingMonday()
    {
        _kingMonday = await ApiService.GetLatestPlayerAsync("King Monday!");
        // Replace direct call to PlayerManager with API service call
        _kingMondayStats = await ApiService.GetRankedPlayerAsync("King Monday!", 1, 30);

        if (_kingMonday != null)
        {
            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Monday!");
            if (titleInfo != null)
            {
                _kingMondayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingMondayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingMondayTitleProgressData = new double[] { _kingMonday.TitleProgress, 100 - _kingMonday.TitleProgress };
                _kingMondayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingMondayStats?.ProjectedTitleChange)),
                    _kingMonday.NextTitle
                };
            }
        }
    }

    private async Task LoadKingFridaySEHistory()
    {
        try
        {
            // Calculate date range (14 days ago until now)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-14);

            // Get historical player data for King Friday
            var historicalData = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                startDate,
                endDate);

            if (historicalData?.Any() == true)
            {
                // Format the data for the chart
                _kingFridaySEHistoryLabels = historicalData
                    .Select(p => p.Updated.ToString("MM/dd"))
                    .ToArray();

                // Convert SE values to numeric values for the chart
                _kingFridaySEHistoryData = historicalData
                    .Select(p => ParseSoulEggs(p.SoulEggs))
                    .ToArray();

                // Create the chart series
                _kingFridaySEHistorySeries =
                [
                    new ChartSeries
                    {
                        Name = "Soul Eggs",
                        Data = _kingFridaySEHistoryData
                    }
                ];
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading King Friday SE history data");
        }
    }

    private double ParseSoulEggs(string? soulEggs)
    {
        if (string.IsNullOrEmpty(soulEggs))
            return 0;

        // Remove any suffix like 'Q' for quintillion, etc.
        var cleanedValue = soulEggs;
        var suffixes = new[] { 'Q', 'q', 's', 'S', 'T', 't', 'B', 'b', 'M', 'm' };
        foreach (var suffix in suffixes)
        {
            cleanedValue = cleanedValue.TrimEnd(suffix);
        }

        // Try to parse using the existing number parsing method
        if (TryParseAnyNumber(cleanedValue, out decimal result))
        {
            return (double)result;
        }

        // Fallback to simpler parsing
        if (double.TryParse(cleanedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double simpleResult))
        {
            return simpleResult;
        }

        return 0;
    }

    private string GetTimeSinceLastUpdate()
    {
        var timeSince = DateTime.Now - _lastUpdated;

        if (timeSince.TotalSeconds < 60)
        {
            return $"{timeSince.Seconds:D} seconds ago";
        }

        if (timeSince.TotalMinutes < 60)
        {
            return $"{timeSince.Minutes:D} minutes, {timeSince.Seconds:D} seconds ago";
        }

        if (timeSince.TotalHours < 24)
        {
            return $"{timeSince.Hours:D} hours, {timeSince.Minutes:D} minutes, {timeSince.Seconds:D} seconds ago";
        }

        return $"{timeSince.Days:D} days, {timeSince.Hours:D} hours, {timeSince.Minutes:D} minutes ago";
    }

    private string FormatTitleChangeLabel(DateTime projectedDate)
    {
        var timeSpan = projectedDate - DateTime.Now;

        if (timeSpan.Days < 0)
            return "Title change overdue";

        if (timeSpan.Days == 0)
            return "Title change today";

        if (timeSpan.Days == 1)
            return "Next title tomorrow";

        return $"Next title in {timeSpan.Days} days";
    }

    private double CalculateProgressPercentage(string? current, string? goal, string? baseline = null)
    {
        try
        {
            // Handle null or empty inputs
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(goal))
            {
                return 0;
            }

            // Remove the 's' suffix, '%' character, and trim whitespace
            current = current.TrimEnd('d', 's', '%').Trim();
            goal = goal.TrimEnd('d', 's', '%').Trim();
            baseline = baseline?.TrimEnd('d', 's', '%').Trim();

            decimal currentValue, goalValue, baselineValue = 0;

            // Parse all values
            if (!TryParseAnyNumber(current, out currentValue) ||
                !TryParseAnyNumber(goal, out goalValue))
            {
                return 0;
            }

            // If baseline is provided, use it to adjust the calculation
            if (!string.IsNullOrEmpty(baseline) && TryParseAnyNumber(baseline, out baselineValue))
            {
                // Adjust current and goal values relative to baseline
                currentValue = Math.Max(0, currentValue - baselineValue);
                goalValue = Math.Max(0, goalValue - baselineValue);
            }

            // Avoid division by zero
            if (goalValue == 0)
            {
                return 0;
            }

            // Calculate percentage and ensure it doesn't exceed 100
            return Math.Min(100, (double)((currentValue / goalValue) * 100));
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private bool TryParseAnyNumber(string input, out decimal result)
    {
        result = 0;

        // Try parsing as regular decimal first
        if (decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        // If that fails, try scientific notation
        return TryParseScientificNotation(input, out result);
    }

    private bool TryParseScientificNotation(string input, out decimal result)
    {
        result = 0;

        try
        {
            if (string.IsNullOrEmpty(input)) return false;

            // Split number and exponent
            string[] parts = input.Split(new[] { 'e' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2) return false;

            decimal baseNumber = decimal.Parse(parts[0], CultureInfo.InvariantCulture);
            int exponent = int.Parse(parts[1]);

            // Calculate the result using power of 10
            result = baseNumber * (decimal)Math.Pow(10, exponent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetLocalTimeString(DateTime utcTime)
    {
        // Convert UTC time to local time
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);

        // Format for display with date if not today
        if (localTime.Date == DateTime.Today)
        {
            return $"Today {localTime.ToString("HH:mm")}";
        }
        else if (localTime.Date == DateTime.Today.AddDays(-1))
        {
            return $"Yesterday {localTime.ToString("HH:mm")}";
        }
        else
        {
            return localTime.ToString("MM/dd HH:mm");
        }
    }

    // Generate test data for the chart - only used as a fallback when real data isn't available
    private void InitializeTestData()
    {
        Logger.LogInformation("Using test data for Soul Eggs chart");

        // Create mock data for the last 7 days
        var today = DateTime.Now;
        var dates = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-i))
            .Reverse()
            .ToArray();

        _kingFridayMultiSeriesLabels = dates.Select(d => d.ToString("MM/dd")).ToArray();

        // Initialize the metrics dictionary with random test data for Soul Eggs only
        _metricHistoryData = new Dictionary<string, List<double>>
        {
            // Soul Eggs (increasing trend)
            ["Soul Eggs"] = new List<double> { 100, 110, 125, 145, 160, 185, 210 }
        };

        // Create chart series directly
        _kingFridayMultiSeriesData = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = "Soul Eggs",
                Data = _metricHistoryData["Soul Eggs"].ToArray()
            }
        };

        // Update labels
        _kingFridayMultiSeriesLabels = dates.Select(d => d.ToString("MM/dd")).ToArray();
    }

    // Update the chart based on the selected number of days
    private async Task UpdateMultiSeriesChart()
    {
        try
        {
            // Calculate date range (30 days ago until now)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30); // Get data for up to 30 days

            // Get historical player data for King Friday directly from the API
            var historicalData = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                startDate,
                endDate);

            if (historicalData?.Any() == true)
            {
                // Process actual historical data
                // First, ensure the data is sorted by date
                var sortedData = historicalData
                    .OrderBy(p => p.Updated)
                    .ToList();

                // Group data by day and take only the last entry for each day
                var groupedByDay = sortedData
                    .GroupBy(p => p.Updated.Date)
                    .Select(g => g.OrderByDescending(p => p.Updated).First())
                    .OrderBy(p => p.Updated)
                    .ToList();

                Logger.LogInformation("Reduced data points from {OriginalCount} to {GroupedCount} (one per day)",
                    sortedData.Count, groupedByDay.Count);

                // Extract the dates for the x-axis
                _kingFridayMultiSeriesLabels = groupedByDay
                    .Select(p => p.Updated.ToString("MM/dd"))
                    .ToArray();

                // Process Soul Eggs data
                var soulEggsValues = new List<double>();

                foreach (var dataPoint in groupedByDay)
                {
                    soulEggsValues.Add(ParseSoulEggs(dataPoint.SoulEggs));
                }

                // Update the metrics dictionary with Soul Eggs data only
                _metricHistoryData = new Dictionary<string, List<double>>
                {
                    ["Soul Eggs"] = soulEggsValues
                };

                Logger.LogInformation("Successfully loaded Soul Eggs data with {Count} data points", groupedByDay.Count);
            }
            else
            {
                Logger.LogWarning("No historical data found for King Friday");
                // No real data available, initialize with test data
                InitializeTestData();
                return;
            }

            // Format the chart data based on the selected number of days
            // Limit the labels and data to the selected number of days
            var daysToShow = Math.Min(_selectedDaysToDisplay, _kingFridayMultiSeriesLabels.Length);
            var labels = _kingFridayMultiSeriesLabels.TakeLast(daysToShow).ToArray();

            // Create single series for Soul Eggs
            _kingFridayMultiSeriesData = new List<ChartSeries>();

            var soulEggData = _metricHistoryData["Soul Eggs"].TakeLast(daysToShow).ToArray();
            _kingFridayMultiSeriesData.Add(new ChartSeries
            {
                Name = "Soul Eggs",
                Data = soulEggData
            });

            // Update the labels
            _kingFridayMultiSeriesLabels = labels;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading King Friday Soul Eggs data");
            // If there's an error, fall back to test data
            InitializeTestData();
        }
    }

    // Handle the slider value change
    private async void OnDaysSliderChanged(int value)
    {
        _selectedDaysToDisplay = value;
        await UpdateMultiSeriesChart();
        StateHasChanged();
    }
}
