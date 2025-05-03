namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Utilities;

using System.Collections.Generic;

/// <summary>
/// Utility class to map ship IDs to their names
/// </summary>
public static class ShipNameMapper
{
    private static readonly Dictionary<int, string> ShipNames = new()
    {
        { 0, "Chicken One" },
        { 1, "Chicken Heavy" },
        { 2, "BCR" },
        { 3, "Quintillion Chicken" },
        { 4, "Corellihen Corvette" },
        { 5, "Galeggtica" },
        { 6, "Chickfiant" },
        { 7, "Voyegger" },
        { 8, "Henerprise" },
        { 9, "Defihent" },
        { 10, "Cornish-Ex Hen" }
    };

    /// <summary>
    /// Get the name of a ship by its ID
    /// </summary>
    /// <param name="shipId">The ship ID</param>
    /// <returns>The ship name, or "Unknown Ship" if the ID is not found</returns>
    public static string GetShipName(int shipId)
    {
        return ShipNames.TryGetValue(shipId, out var name) ? name : $"Unknown Ship ({shipId})";
    }

    /// <summary>
    /// Get the status description for a mission status code
    /// </summary>
    /// <param name="statusCode">The status code</param>
    /// <returns>The status description</returns>
    public static string GetStatusDescription(int statusCode)
    {
        return statusCode switch
        {
            0 => "Unknown",
            1 => "Fueling",
            2 => "In Progress",
            3 => "Returned",
            4 => "Complete",
            5 => "Archived",
            10 => "In Progress",
            25 => "Archived",
            _ => $"Unknown ({statusCode})"
        };
    }
}
