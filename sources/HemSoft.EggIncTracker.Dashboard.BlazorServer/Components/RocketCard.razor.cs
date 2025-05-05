namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components;

using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Utilities;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System;
using System.Collections.Generic;

public partial class RocketCard : IDisposable
{
    [Parameter] public DashboardPlayer? DashboardPlayer { get; set; }
    [Parameter] public DateTime? LastUpdatedTimestamp { get; set; }
    [Parameter] public bool IsLoading { get; set; }

    private List<JsonPlayerExtendedMissionInfo> Missions => DashboardPlayer?.Missions ?? new List<JsonPlayerExtendedMissionInfo>();
    private JsonPlayerExtendedMissionInfo? StandbyMission => DashboardPlayer?.StandbyMission;
    private System.Timers.Timer? _updateTimer;

    [Inject] private ILogger<RocketCard> Logger { get; set; } = default!;
    [Inject] private DashboardState DashboardState { get; set; } = default!;

    protected override void OnInitialized()
    {
        // Subscribe to state changes
        DashboardState.OnChange += HandleStateChange;

        // Set up timer to update the UI every second
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnUpdateTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Start();

        base.OnInitialized();
    }

    private async void OnUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            // Only update the time displays without triggering a full refresh
            // This is more efficient than calling StateHasChanged for the entire component
            if (Missions.Any())
            {
                // We only need to update the UI, not reload data
                await InvokeAsync(() =>
                {
                    // Use a more targeted StateHasChanged approach
                    StateHasChanged();
                });
            }
        }
        catch (ObjectDisposedException)
        {
            // Component is being disposed, stop the timer
            _updateTimer?.Stop();
            Logger.LogInformation("Update timer stopped due to component disposal");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating UI from timer");
        }
    }

    private void HandleStateChange()
    {
        // Force UI update when state changes
        InvokeAsync(StateHasChanged);
    }

    // Helper to format the timestamp for display
    private static string GetLocalTimeString(DateTime? utcTime)
    {
        if (!utcTime.HasValue || utcTime.Value == DateTime.MinValue)
        {
            return string.Empty;
        }

        TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var cstTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime.Value, cstZone);
        var cstToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, cstZone);

        return cstTime.Date == cstToday
            ? $"Today {cstTime:HH:mm}"
            : cstTime.Date == cstToday.AddDays(-1)
                ? $"Yesterday {cstTime:HH:mm}"
                : cstTime.ToString("MM/dd HH:mm");
    }

    // Format time remaining in a human-readable format
    private string FormatTimeRemaining(double seconds)
    {
        // Calculate the actual time remaining based on the return time
        var timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalDays >= 1)
        {
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }

        if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    /// <summary>
    /// Calculates the actual time remaining for a mission based on its return time.
    /// </summary>
    /// <param name="mission">An object containing mission details, including:
    /// - MissionLog: A string representing the actual return time (if available).
    /// - StartTimeDerived: The mission's start time in Unix timestamp format.
    /// - DurationSeconds: The mission's duration in seconds.</param>
    /// <returns>
    /// A string representing the time remaining in a human-readable format, or "Ready to collect" 
    /// if the mission has already returned. Returns "Time unknown" in case of an error.
    /// </returns>
    private string CalculateActualTimeRemaining(JsonPlayerExtendedMissionInfo mission)
    {
        try
        {
            // If we have the actual return time stored in MissionLog, use it
            DateTime returnTime;
            if (!string.IsNullOrEmpty(mission.MissionLog) && DateTime.TryParse(mission.MissionLog, out DateTime parsedTime))
            {
                returnTime = parsedTime;
            }
            else
            {
                // Fall back to calculating from start time and duration if MissionLog is not available
                var startTime = DateTimeOffset.FromUnixTimeSeconds((long)mission.StartTimeDerived).DateTime;
                returnTime = startTime.AddSeconds(mission.DurationSeconds);
            }

            // Convert to CST time
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var cstReturnTime = TimeZoneInfo.ConvertTimeFromUtc(returnTime, cstZone);
            var cstNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);

            // Calculate time remaining
            TimeSpan timeRemaining = cstReturnTime - cstNow;

            // If the time remaining is negative, the mission has already returned
            if (timeRemaining.TotalSeconds <= 0)
            {
                return "Ready to collect";
            }

            if (timeRemaining.TotalDays >= 1)
            {
                return $"{timeRemaining.Days}d {timeRemaining.Hours}h {timeRemaining.Minutes}m";
            }

            if (timeRemaining.TotalHours >= 1)
            {
                return $"{timeRemaining.Hours}h {timeRemaining.Minutes}m {timeRemaining.Seconds}s";
            }

            return $"{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent UI crashes
            System.Diagnostics.Debug.WriteLine($"Error calculating time remaining: {ex.Message}");
            return "Time unknown";
        }
    }

    // Calculate mission progress percentage
    private double GetMissionProgressPercentage(JsonPlayerExtendedMissionInfo mission)
    {
        if (mission.DurationSeconds <= 0)
        {
            return 0;
        }

        // Make sure we have a positive value for seconds remaining
        var secondsRemaining = Math.Max(0, mission.SecondsRemaining);

        // Calculate elapsed time and percentage
        var elapsed = mission.DurationSeconds - secondsRemaining;
        var percentage = (elapsed / mission.DurationSeconds) * 100;

        // Ensure we return a value between 0 and 100
        // For very small values, set a minimum of 5% to ensure the progress bar is visible
        var result = Math.Clamp(percentage, 5, 100);
        return result;
    }

    // Calculate fuel percentage
    private double GetFuelPercentage(JsonPlayerFuel fuel)
    {
        // In a real implementation, we would need to know the capacity
        // For now, we'll assume the amount is a percentage
        return Math.Min(fuel.Amount * 100, 100);
    }

    // Get start time
    private string GetStartTime(double startTimeDerived)
    {
        var startTime = DateTimeOffset.FromUnixTimeSeconds((long)startTimeDerived).DateTime;

        // Convert to CST time
        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var cstStartTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, cstZone);

        return cstStartTime.ToString("MM/dd HH:mm");
    }

    // Get return time
    private string GetReturnTime(JsonPlayerExtendedMissionInfo mission, bool includeSeconds = false)
    {
        try
        {
            // If we have the actual return time stored in MissionLog, use it
            DateTime returnTime;
            if (!string.IsNullOrEmpty(mission.MissionLog) && DateTime.TryParse(mission.MissionLog, out DateTime parsedTime))
            {
                returnTime = parsedTime;
            }
            else
            {
                // Fall back to calculating from start time and duration if MissionLog is not available
                var startTime = DateTimeOffset.FromUnixTimeSeconds((long)mission.StartTimeDerived).DateTime;
                returnTime = startTime.AddSeconds(mission.DurationSeconds);
            }

            // Convert to CST time
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var cstReturnTime = TimeZoneInfo.ConvertTimeFromUtc(returnTime, cstZone);
            var cstToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, cstZone);

            // Format based on when the mission returns
            if (cstReturnTime.Date == cstToday)
            {
                return includeSeconds
                    ? $"Today at {cstReturnTime:HH:mm:ss}"
                    : $"Today at {cstReturnTime:HH:mm}";
            }
            else if (cstReturnTime.Date == cstToday.AddDays(1))
            {
                return includeSeconds
                    ? $"Tomorrow at {cstReturnTime:HH:mm:ss}"
                    : $"Tomorrow at {cstReturnTime:HH:mm}";
            }
            else
            {
                return includeSeconds
                    ? cstReturnTime.ToString("MM/dd HH:mm:ss")
                    : cstReturnTime.ToString("MM/dd HH:mm");
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent UI crashes
            System.Diagnostics.Debug.WriteLine($"Error calculating return time: {ex.Message}");
            return "Time unknown";
        }
    }

    // Legacy method for backward compatibility
    private string GetReturnTime(double startTimeDerived, float durationSeconds, bool includeSeconds = false)
    {
        try
        {
            var startTime = DateTimeOffset.FromUnixTimeSeconds((long)startTimeDerived).DateTime;
            var returnTime = startTime.AddSeconds(durationSeconds);

            // Convert to CST time
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var cstReturnTime = TimeZoneInfo.ConvertTimeFromUtc(returnTime, cstZone);
            var cstToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, cstZone);

            // Format based on when the mission returns
            if (cstReturnTime.Date == cstToday)
            {
                return includeSeconds
                    ? $"Today at {cstReturnTime:HH:mm:ss}"
                    : $"Today at {cstReturnTime:HH:mm}";
            }
            else if (cstReturnTime.Date == cstToday.AddDays(1))
            {
                return includeSeconds
                    ? $"Tomorrow at {cstReturnTime:HH:mm:ss}"
                    : $"Tomorrow at {cstReturnTime:HH:mm}";
            }
            else
            {
                return includeSeconds
                    ? cstReturnTime.ToString("MM/dd HH:mm:ss")
                    : cstReturnTime.ToString("MM/dd HH:mm");
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent UI crashes
            System.Diagnostics.Debug.WriteLine($"Error calculating return time: {ex.Message}");
            return "Time unknown";
        }
    }

    // Format duration
    private string FormatDuration(float durationSeconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);

        if (duration.TotalDays >= 1)
        {
            return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m";
        }
        else if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours}h {duration.Minutes}m";
        }
        else
        {
            return $"{duration.Minutes}m {duration.Seconds}s";
        }
    }

    // Get return time style (color based on how soon the mission returns)
    private string GetReturnTimeStyle(double secondsRemaining)
    {
        if (secondsRemaining <= 3600) // Less than 1 hour
        {
            return "color: var(--mud-palette-success);";
        }
        else if (secondsRemaining <= 14400) // Less than 4 hours
        {
            return "color: var(--mud-palette-info);";
        }
        else
        {
            return string.Empty;
        }
    }

    // Get ship name based on ship type
    private string GetShipName(int shipType)
    {
        return ShipNameMapper.GetShipName(shipType);
    }

    // Get ship icon based on ship type - always return rocket icon
    private string GetShipIcon(int shipType)
    {
        // Return rocket icon for all ship types
        return Icons.Material.Filled.RocketLaunch;
    }

    // Get status name based on status code
    private string GetStatusName(int statusCode)
    {
        return ShipNameMapper.GetStatusDescription(statusCode);
    }

    // Get status color based on status code
    private Color GetStatusColor(int statusCode)
    {
        return statusCode switch
        {
            1 => Color.Warning,  // Fueling
            2 => Color.Info,     // Traveling
            3 => Color.Success,  // Returned
            10 => Color.Warning, // In Progress (was Complete)
            25 => Color.Dark,    // Archived
            _ => Color.Default
        };
    }

    // Get status color hex based on status code
    private string GetStatusColorHex(int statusCode)
    {
        return statusCode switch
        {
            1 => "#ff9800",  // Warning - Fueling
            2 => "#03a9f4",  // Info - Traveling
            3 => "#4caf50",  // Success - Returned
            10 => "#ff9800", // Warning - In Progress (was Complete)
            25 => "#424242", // Dark - Archived
            _ => "#9e9e9e"   // Default
        };
    }

    // Get duration type description
    private string GetDurationType(int durationType)
    {
        return durationType switch
        {
            0 => "Short",
            1 => "Medium",
            2 => "Long",
            3 => "Epic",
            _ => $"Unknown ({durationType})"
        };
    }

    // Get estimated duration based on ship type and level
    private string GetDurationByShipAndLevel(int shipType, int level)
    {
        // These are approximate durations based on ship type and level
        // The actual duration may vary slightly
        TimeSpan duration;

        switch (shipType)
        {
            case 10: // Henliner
                duration = level switch
                {
                    7 => TimeSpan.FromHours(38.4), // 138,240 seconds
                    6 => TimeSpan.FromHours(38.4), // 138,240 seconds
                    5 => TimeSpan.FromHours(4.8),  // 17,280 seconds
                    4 => TimeSpan.FromHours(9.6),  // 34,560 seconds
                    _ => TimeSpan.FromHours(38.4)  // Default to long mission
                };
                break;
            case 9: // Defihent
                duration = level switch
                {
                    6 => TimeSpan.FromHours(9.6),  // 34,560 seconds
                    5 => TimeSpan.FromHours(9.6),  // 34,560 seconds
                    _ => TimeSpan.FromHours(57.6)  // 207,360 seconds (default to long mission)
                };
                break;
            case 8: // Henerprise
                duration = TimeSpan.FromHours(7.2); // 25,920 seconds
                break;
            default:
                duration = TimeSpan.FromHours(24); // Default duration
                break;
        }

        return FormatDuration((float)duration.TotalSeconds);
    }

    // Get quality bonus text
    private string GetQualityBonusText(double qualityBump)
    {
        return qualityBump > 0 ? $"+{qualityBump:F2}x" : "None";
    }

    // Get egg name based on egg type
    private string GetEggName(int eggType)
    {
        return eggType switch
        {
            1 => "Regular",
            2 => "Superfood",
            3 => "Medical",
            4 => "Rocket Fuel",
            5 => "Super Material",
            6 => "Fusion",
            7 => "Quantum",
            8 => "Immortality",
            9 => "Tachyon",
            10 => "Graviton",
            11 => "Dilithium",
            12 => "Prodigy",
            13 => "Terraform",
            14 => "Antimatter",
            15 => "Dark Matter",
            16 => "AI",
            17 => "Nebula",
            18 => "Universe",
            19 => "Enlightenment",
            _ => $"Unknown Egg ({eggType})"
        };
    }

    public void Dispose()
    {
        try
        {
            // Unsubscribe from events
            DashboardState.OnChange -= HandleStateChange;

            // Clean up the timer
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Elapsed -= OnUpdateTimerElapsed;
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // Log disposal
            Logger.LogInformation("RocketCard component disposed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing RocketCard component");
        }

        // Suppress finalization
        GC.SuppressFinalize(this);
    }
}
