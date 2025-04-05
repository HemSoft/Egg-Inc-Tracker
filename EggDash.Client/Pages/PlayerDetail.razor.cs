namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;

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
    private string _playerSEThisWeek = string.Empty;
    private bool isLoading = true;
    private const int NameCutOff = 10;

    protected override async Task OnInitializedAsync()
    {
        await LoadPlayerData();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (EID != _player?.EID)
        {
            await LoadPlayerData();
        }
    }

    private async Task LoadPlayerData()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            Logger.LogInformation($"Loading player data for EID: {EID}");

            // Get player by EID
            _player = await ApiService.GetPlayerByEIDAsync(EID);

            if (_player == null)
            {
                Logger.LogWarning($"Player with EID {EID} not found");
                isLoading = false;
                StateHasChanged();
                return;
            }

            Logger.LogInformation($"Player found: {_player.PlayerName}");

            // Get player stats
            _playerStats = await ApiService.GetRankedPlayerAsync(_player.PlayerName, 1, 30);

            // Calculate SE This Week
            await CalculateSEThisWeek();

            // Update goal data from MajPlayerRankingManager
            (_SEGoalBegin, _SEGoalEnd) = await MajPlayerRankingManager.GetSurroundingSEPlayers(_player.PlayerName, _player.SoulEggs);
            (_EBGoalBegin, _EBGoalEnd) = await MajPlayerRankingManager.GetSurroundingEBPlayers(_player.PlayerName, _player.EarningsBonusPercentage);
            (_MERGoalBegin, _MERGoalEnd) = await MajPlayerRankingManager.GetSurroundingMERPlayers(_player.PlayerName, (decimal)_player.MER);
            (_JERGoalBegin, _JERGoalEnd) = await MajPlayerRankingManager.GetSurroundingJERPlayers(_player.PlayerName, (decimal)_player.JER);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error loading player data for EID {EID}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task CalculateSEThisWeek()
    {
        try
        {
            // Get the current time in UTC
            var utcNow = DateTime.UtcNow;
            var utcToday = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var todayUtcStart = utcToday;

            // Calculate the start of the week (Monday) in UTC
            // Get the days since last Monday (DayOfWeek.Monday = 1)
            int daysSinceMonday = ((int)utcToday.DayOfWeek - 1 + 7) % 7; // Ensure positive result
            var startOfWeekUtc = utcToday.AddDays(-daysSinceMonday);

            Logger.LogInformation($"Today (UTC): {utcToday}, Start of Week (Monday) (UTC): {startOfWeekUtc}");
            Logger.LogInformation($"Days since Monday: {daysSinceMonday}");

            // Get player history for this week
            var playerHistoryWeek = await ApiService.GetPlayerHistoryAsync(
                _player!.PlayerName,
                startOfWeekUtc,
                DateTime.UtcNow);

            // Get player history for today
            var playerHistoryToday = await ApiService.GetPlayerHistoryAsync(
                _player.PlayerName,
                todayUtcStart,
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
            Logger.LogInformation($"Player history today count: {playerHistoryToday?.Count() ?? 0}");
            Logger.LogInformation($"Player history week count: {playerHistoryWeek?.Count() ?? 0}");
            Logger.LogInformation($"First today: {firstToday?.Updated}, SE: {firstToday?.SoulEggs}");
            Logger.LogInformation($"First this week: {firstThisWeek?.Updated}, SE: {firstThisWeek?.SoulEggs}");
            Logger.LogInformation($"Current SE: {_player.SoulEggs}, Full: {_player.SoulEggsFull}");

            // Calculate SE gain this week
            if (playerHistoryWeek != null && playerHistoryWeek.Any())
            {
                var latestSE = _player.SoulEggsFull;
                var earliestSE = firstThisWeek?.SoulEggsFull ?? latestSE;

                Logger.LogInformation($"Using latest SE: {latestSE}");
                Logger.LogInformation($"Using earliest SE: {earliestSE}");

                try
                {
                    // Use BigNumberCalculator to calculate the difference
                    _playerSEThisWeek = HemSoft.EggIncTracker.Domain.BigNumberCalculator.CalculateDifference(earliestSE, latestSE);
                    Logger.LogInformation($"Calculated SE gain this week: {_playerSEThisWeek}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error calculating SE gain this week");
                    _playerSEThisWeek = "0";
                }
            }
            else
            {
                Logger.LogWarning("No player history for this week, setting SE gain to 0");
                _playerSEThisWeek = "0";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating SE this week");
            _playerSEThisWeek = "0";
        }
    }

    private double CalculateProgressPercentage(string? current, string? target, string? previous)
    {
        if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(previous))
        {
            return 0;
        }

        try
        {
            // Parse the values
            var currentValue = ParseBigNumber(current);
            var targetValue = ParseBigNumber(target);
            var previousValue = ParseBigNumber(previous);

            // Calculate the percentage
            if (targetValue <= previousValue)
            {
                return 100; // Already at or beyond the target
            }

            var range = targetValue - previousValue;
            var progress = currentValue - previousValue;

            if (range <= 0)
            {
                return 100; // Avoid division by zero
            }

            var percentage = (progress / range) * 100;
            return Math.Min(Math.Max(percentage, 0), 100); // Clamp between 0 and 100
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calculating progress percentage for {current}, {target}, {previous}");
            return 0;
        }
    }

    private double ParseBigNumber(string value)
    {
        // Remove any non-numeric characters except for decimal points and scientific notation
        var cleanValue = value.Trim();

        // Handle scientific notation (e.g., 1.23e45)
        if (cleanValue.Contains('e') || cleanValue.Contains('E'))
        {
            return double.Parse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        // Handle suffixes like Q, q, S, s, etc.
        var suffixes = new Dictionary<string, double>
        {
            {"K", 1e3}, {"M", 1e6}, {"B", 1e9}, {"T", 1e12},
            {"q", 1e15}, {"Q", 1e18}, {"s", 1e21}, {"S", 1e24},
            {"O", 1e27}, {"N", 1e30}, {"d", 1e33}, {"U", 1e36},
            {"D", 1e39}, {"Td", 1e42}, {"Qd", 1e45}, {"sd", 1e48},
            {"Sd", 1e51}, {"Od", 1e54}, {"Nd", 1e57}, {"V", 1e60},
            {"uV", 1e63}, {"dV", 1e66}, {"tV", 1e69}, {"qV", 1e72},
            {"QV", 1e75}, {"sV", 1e78}, {"SV", 1e81}, {"OV", 1e84},
            {"NV", 1e87}, {"tT", 1e90}
        };

        foreach (var suffix in suffixes)
        {
            if (cleanValue.EndsWith(suffix.Key))
            {
                var numericPart = cleanValue.Substring(0, cleanValue.Length - suffix.Key.Length);
                if (double.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return number * suffix.Value;
                }
            }
        }

        // If no suffix is found, try to parse as a regular number
        if (double.TryParse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        // If all else fails, return 0
        return 0;
    }

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/");
    }
}
