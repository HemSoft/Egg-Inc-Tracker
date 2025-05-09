namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System.Text.Json;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for persisting timer states between application restarts
/// </summary>
public class TimerPersistenceService
{
    private readonly ILogger<TimerPersistenceService> _logger;
    private readonly string _missionStorageFilePath;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public TimerPersistenceService(ILogger<TimerPersistenceService> logger)
    {
        _logger = logger;

        // Store the timer states in the application's data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDataDir = Path.Combine(appDataPath, "EggIncTracker");

        // Create the directory if it doesn't exist
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        _missionStorageFilePath = Path.Combine(appDataDir, "mission_timer_states.json");

        _logger.LogInformation("Mission timer persistence storage file: {FilePath}", _missionStorageFilePath);
    }

    // Reusable JsonSerializerOptions
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Save mission timer states to persistent storage
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="missions">List of missions</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SaveMissionTimerStates(string playerName, List<JsonPlayerExtendedMissionInfo> missions)
    {
        try
        {
            _logger.LogInformation("Saving mission timer states for player {PlayerName}", playerName);

            // Create a dictionary to store the timer states
            var timerStates = LoadAllMissionTimerStates();

            // Update the timer states for the player
            timerStates[playerName] = missions.Select(m => new MissionTimerState
            {
                Ship = m.Ship,
                Status = m.Status,
                DurationType = m.DurationType,
                Level = m.Level,
                DurationSeconds = m.DurationSeconds,
                ReturnTime = !string.IsNullOrEmpty(m.MissionLog) && DateTime.TryParse(m.MissionLog, out DateTime returnTime)
                    ? returnTime
                    : DateTime.UtcNow.AddSeconds(m.SecondsRemaining),
                StartTime = DateTimeOffset.FromUnixTimeSeconds((long)m.StartTimeDerived).UtcDateTime
            }).ToList();

            // Save the timer states to the file
            var json = JsonSerializer.Serialize(timerStates, _jsonOptions);
            File.WriteAllText(_missionStorageFilePath, json);

            _logger.LogInformation("Saved {Count} mission timer states for player {PlayerName}",
                timerStates[playerName].Count, playerName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving mission timer states for player {PlayerName}", playerName);
            return false;
        }
    }

    /// <summary>
    /// Load mission timer states from persistent storage
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <returns>List of mission timer states</returns>
    public List<MissionTimerState> LoadMissionTimerStates(string playerName)
    {
        try
        {
            _logger.LogInformation("Loading mission timer states for player {PlayerName}", playerName);

            var timerStates = LoadAllMissionTimerStates();

            if (timerStates.TryGetValue(playerName, out var missions))
            {
                _logger.LogInformation("Loaded {Count} mission timer states for player {PlayerName}",
                    missions.Count, playerName);
                return missions;
            }

            _logger.LogInformation("No mission timer states found for player {PlayerName}", playerName);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading mission timer states for player {PlayerName}", playerName);
            return [];
        }
    }

    /// <summary>
    /// Load all mission timer states from persistent storage
    /// </summary>
    /// <returns>Dictionary of player names to mission timer states</returns>
    private Dictionary<string, List<MissionTimerState>> LoadAllMissionTimerStates()
    {
        try
        {
            if (!File.Exists(_missionStorageFilePath))
            {
                _logger.LogInformation("Mission timer states file does not exist, creating new one");
                return [];
            }

            var json = File.ReadAllText(_missionStorageFilePath);
            var timerStates = JsonSerializer.Deserialize<Dictionary<string, List<MissionTimerState>>>(json);

            return timerStates ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all mission timer states");
            return [];
        }
    }



}

/// <summary>
/// Class to store mission timer state information
/// </summary>
public class MissionTimerState
{
    /// <summary>
    /// Ship type
    /// </summary>
    public int Ship { get; set; }

    /// <summary>
    /// Mission status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Duration type (short, medium, long)
    /// </summary>
    public int DurationType { get; set; }

    /// <summary>
    /// Ship level
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public float DurationSeconds { get; set; }

    /// <summary>
    /// When the mission will return
    /// </summary>
    public DateTime ReturnTime { get; set; }

    /// <summary>
    /// When the mission was launched
    /// </summary>
    public DateTime StartTime { get; set; }
}


