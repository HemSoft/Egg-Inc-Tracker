namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components;

using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class RocketCard : IDisposable
{
    [Parameter] public DashboardPlayer? DashboardPlayer { get; set; }
    [Parameter] public DateTime? LastUpdatedTimestamp { get; set; }
    [Parameter] public bool IsLoading { get; set; }

    private List<JsonPlayerExtendedMissionInfo> Missions => DashboardPlayer?.Missions ?? new List<JsonPlayerExtendedMissionInfo>();
    private JsonPlayerExtendedMissionInfo? StandbyMission => DashboardPlayer?.StandbyMission;

    [Inject] private ILogger<RocketCard> Logger { get; set; } = default!;
    [Inject] private DashboardState DashboardState { get; set; } = default!;

    protected override void OnInitialized()
    {
        // Subscribe to state changes
        DashboardState.OnChange += HandleStateChange;
        base.OnInitialized();
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
            return string.Empty; // Don't display if no valid time
        }

        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime.Value, TimeZoneInfo.Local);

        return localTime.Date == DateTime.Today
            ? $"Today {localTime:HH:mm}"
            : localTime.Date == DateTime.Today.AddDays(-1)
                ? $"Yesterday {localTime:HH:mm}"
                : localTime.ToString("MM/dd HH:mm");
    }

    // Format time remaining in a human-readable format
    private string FormatTimeRemaining(double seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalDays >= 1)
        {
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
    }

    // Calculate mission progress percentage
    private double GetMissionProgressPercentage(JsonPlayerExtendedMissionInfo mission)
    {
        if (mission.DurationSeconds <= 0)
        {
            return 0;
        }

        double elapsed = mission.DurationSeconds - mission.SecondsRemaining;
        double percentage = (elapsed / mission.DurationSeconds) * 100;
        return Math.Clamp(percentage, 0, 100);
    }

    // Calculate fuel percentage
    private double GetFuelPercentage(JsonPlayerFuel fuel)
    {
        // In a real implementation, we would need to know the capacity
        // For now, we'll assume the amount is a percentage
        return Math.Min(fuel.Amount * 100, 100);
    }

    // Get return time
    private string GetReturnTime(double startTimeDerived, float durationSeconds)
    {
        DateTime startTime = DateTimeOffset.FromUnixTimeSeconds((long)startTimeDerived).DateTime;
        DateTime returnTime = startTime.AddSeconds(durationSeconds);

        // Convert to local time
        DateTime localReturnTime = TimeZoneInfo.ConvertTimeFromUtc(returnTime, TimeZoneInfo.Local);

        return localReturnTime.ToString("MM/dd HH:mm");
    }

    // Get ship name based on ship type
    private string GetShipName(int shipType)
    {
        return shipType switch
        {
            1 => "Chicken One",
            2 => "Chicken Heavy",
            3 => "BCR",
            4 => "Quintillion Chicken",
            5 => "Corellihen Corvette",
            6 => "Galeggtica",
            7 => "Henerprise",
            _ => $"Unknown Ship ({shipType})"
        };
    }

    // Get ship icon based on ship type
    private string GetShipIcon(int shipType)
    {
        return shipType switch
        {
            1 => Icons.Material.Filled.RocketLaunch,
            2 => Icons.Material.Filled.RocketLaunch,
            3 => Icons.Material.Filled.RocketLaunch,
            4 => Icons.Material.Filled.RocketLaunch,
            5 => Icons.Material.Filled.RocketLaunch,
            6 => Icons.Material.Filled.RocketLaunch,
            7 => Icons.Material.Filled.RocketLaunch,
            _ => Icons.Material.Filled.QuestionMark
        };
    }

    // Get status name based on status code
    private string GetStatusName(int statusCode)
    {
        return statusCode switch
        {
            1 => "Fueling",
            2 => "Traveling",
            3 => "Returned",
            _ => $"Unknown ({statusCode})"
        };
    }

    // Get status color based on status code
    private Color GetStatusColor(int statusCode)
    {
        return statusCode switch
        {
            1 => Color.Warning,  // Fueling
            2 => Color.Info,     // Traveling
            3 => Color.Success,  // Returned
            _ => Color.Default
        };
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
        // Unsubscribe from events
        DashboardState.OnChange -= HandleStateChange;

        // Suppress finalization
        GC.SuppressFinalize(this);
    }
}
