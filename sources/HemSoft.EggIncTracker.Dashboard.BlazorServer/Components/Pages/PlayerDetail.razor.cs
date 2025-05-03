namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System.Globalization;
using System.Numerics;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Data.Dtos; // Ensure DTOs using is present
using HemSoft.EggIncTracker.Domain; // Add Domain using for static managers

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging; // Added missing using for ILogger

using MudBlazor;

// Ensure the partial class matches the file name and namespace
public partial class PlayerDetail : ComponentBase, IDisposable
{

    public void Dispose()
    {
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

    // PlayerDataService is now used through the ConvertToScientificNotation method

    [Inject]
    private PlayerCardService PlayerCardService { get; set; } = default!;

    [Inject]
    private ILogger<PlayerDetail> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected DashboardPlayer _dashboardPlayer = new();
    protected bool isLoading = true;
    protected PlayerDto? Player => _dashboardPlayer.Player;

    // Fields for the history chart
    protected List<ChartSeries> _playerHistorySeries = new();
    protected string[] _playerHistoryLabels = Array.Empty<string>();
    protected double[] _playerHistoryData = Array.Empty<double>();
    protected ChartOptions _chartOptions = new()
    {
        ChartPalette = new[] { "#4CAF50" },
        InterpolationOption = InterpolationOption.Straight,
        YAxisFormat = "N0",
        XAxisLines = true,
        YAxisLines = true,
        YAxisTicks = 7
    };
    protected int _selectedHistoryDays = 14;

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

    protected async Task ManualRefresh()
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

    protected void NavigateBack()
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

                // Convert SoulEggsFull values directly to scientific notation with suffix (e.g., 53.488s)
                var soulEggsData = groupedByDay
                    .Select(p => ConvertToScientificNotation(p.SoulEggsFull))
                    .ToArray();

                double[] dataDouble; // Use double array directly

                if (soulEggsData.Length == 1)
                {
                    // Handle single data point case
                    dataDouble = new double[] { soulEggsData[0], soulEggsData[0] };
                    // Duplicate label as well
                    _playerHistoryLabels = new string[] { _playerHistoryLabels[0], _playerHistoryLabels[0] };
                }
                else
                {
                    dataDouble = soulEggsData.ToArray();

                    // Check for negligible range
                    if (dataDouble.Length > 1)
                    {
                        double min = dataDouble.Min();
                        double max = dataDouble.Max();
                        // Use a small threshold for negligible range check
                        if (Math.Abs(max - min) < 0.0001)
                        {
                            // Adjust the last value slightly
                            double adjustment = min == 0 ? 1 : min * 0.01;
                            dataDouble[dataDouble.Length - 1] = dataDouble[dataDouble.Length - 1] + adjustment;
                        }
                    }
                }

                // Assign directly to _playerHistoryData
                _playerHistoryData = dataDouble;
                Logger.LogInformation("Converted soul eggs data to scientific notation format for chart");


                _playerHistorySeries = new List<ChartSeries>
                {
                    new() { Name = "Soul Eggs", Data = _playerHistoryData } // Assign the converted double array
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
    protected async void OnHistoryDaysSliderChanged(int value)
    {
        _selectedHistoryDays = value;
        await LoadPlayerHistory(); // Reload history data with new range
    }

    /// <summary>
    /// Converts a large number string to a scientific notation with suffix
    /// Example: "53488830507470026702848" -> 53.488 (representing 53.488s)
    /// </summary>
    /// <param name="bigNumberString">The large number as a string</param>
    /// <returns>A double value representing the number in scientific notation</returns>
    private double ConvertToScientificNotation(string bigNumberString)
    {
        if (string.IsNullOrEmpty(bigNumberString))
        {
            return 0;
        }

        try
        {
            // Define the suffixes and their corresponding powers of 10
            var suffixes = new Dictionary<string, int>
            {
                { "", 0 },       // No suffix
                { "K", 3 },      // Thousand
                { "M", 6 },      // Million
                { "B", 9 },      // Billion
                { "T", 12 },     // Trillion
                { "q", 15 },     // Quadrillion
                { "Q", 18 },     // Quintillion
                { "s", 21 },     // Sextillion
                { "S", 24 },     // Septillion
                { "o", 27 },     // Octillion
                { "N", 30 },     // Nonillion
                { "d", 33 },     // Decillion
                { "U", 36 },     // Undecillion
                { "D", 39 },     // Duodecillion
                { "Td", 42 },    // Tredecillion
                { "qd", 45 },    // Quattuordecillion
                { "Qd", 48 },    // Quindecillion
                { "sd", 51 },    // Sexdecillion
                { "Sd", 54 },    // Septendecillion
                { "Od", 57 },    // Octodecillion
                { "Nd", 60 },    // Novemdecillion
                { "V", 63 }      // Vigintillion
            };

            // Clean the input string
            var cleanValue = bigNumberString.Trim();

            // Try to parse as BigInteger
            if (BigInteger.TryParse(cleanValue, out BigInteger bigInt))
            {
                // Convert to string to count digits
                string bigIntString = bigInt.ToString();
                int digitCount = bigIntString.Length;

                // Find the appropriate suffix
                string suffix = "";
                int power = 0;

                foreach (var kvp in suffixes.OrderByDescending(s => s.Value))
                {
                    if (digitCount > kvp.Value)
                    {
                        suffix = kvp.Key;
                        power = kvp.Value;
                        break;
                    }
                }

                // Calculate the scientific notation value
                if (power > 0 && digitCount > power)
                {
                    // Extract the significant digits
                    string significantDigits = bigIntString.Substring(0, Math.Min(4, digitCount));
                    int decimalPosition = Math.Min(3, digitCount - 1);

                    // Insert decimal point
                    if (decimalPosition > 0 && decimalPosition < significantDigits.Length)
                    {
                        significantDigits = significantDigits.Insert(decimalPosition, ".");
                    }

                    // Parse as double
                    if (double.TryParse(significantDigits, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                    {
                        return result;
                    }
                }

                // Fallback for small numbers
                return (double)bigInt;
            }

            // If BigInteger parsing fails, try decimal
            if (decimal.TryParse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
            {
                return (double)decimalValue;
            }

            // Last resort: try double directly
            if (double.TryParse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                return doubleValue;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error converting {BigNumber} to scientific notation", bigNumberString);
        }

        return 0;
    }
}
