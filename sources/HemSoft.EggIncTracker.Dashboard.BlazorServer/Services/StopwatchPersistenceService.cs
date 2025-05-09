namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for persisting stopwatch state between application restarts
/// </summary>
public class StopwatchPersistenceService
{
    private readonly ILogger<StopwatchPersistenceService> _logger;
    private readonly string _stopwatchStorageFilePath;
    
    // Reusable JsonSerializerOptions
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public StopwatchPersistenceService(ILogger<StopwatchPersistenceService> logger)
    {
        _logger = logger;
        
        // Store the stopwatch state in the application's data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDataDir = Path.Combine(appDataPath, "EggIncTracker");
        
        // Create the directory if it doesn't exist
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }
        
        _stopwatchStorageFilePath = Path.Combine(appDataDir, "stopwatch_state.json");
        _logger.LogInformation("Stopwatch persistence storage file: {FilePath}", _stopwatchStorageFilePath);
    }

    /// <summary>
    /// Save stopwatch state to persistent storage
    /// </summary>
    /// <param name="state">Stopwatch state to save</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SaveStopwatchState(StopwatchState state)
    {
        try
        {
            _logger.LogInformation("Saving stopwatch state");
            
            // Save the stopwatch state to the file
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(_stopwatchStorageFilePath, json);
            
            _logger.LogInformation("Saved stopwatch state successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stopwatch state");
            return false;
        }
    }
    
    /// <summary>
    /// Load stopwatch state from persistent storage
    /// </summary>
    /// <returns>Stopwatch state if found, null otherwise</returns>
    public StopwatchState? LoadStopwatchState()
    {
        try
        {
            if (!File.Exists(_stopwatchStorageFilePath))
            {
                _logger.LogInformation("Stopwatch state file does not exist");
                return null;
            }
            
            var json = File.ReadAllText(_stopwatchStorageFilePath);
            var state = JsonSerializer.Deserialize<StopwatchState>(json);
            
            if (state != null)
            {
                _logger.LogInformation("Loaded stopwatch state successfully");
                return state;
            }
            
            _logger.LogWarning("Failed to deserialize stopwatch state");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stopwatch state");
            return null;
        }
    }
}

/// <summary>
/// Class to store stopwatch state information
/// </summary>
public class StopwatchState
{
    /// <summary>
    /// Whether the stopwatch is running
    /// </summary>
    public bool IsRunning { get; set; }
    
    /// <summary>
    /// Elapsed time in ticks
    /// </summary>
    public long ElapsedTicks { get; set; }
    
    /// <summary>
    /// Start time in ticks
    /// </summary>
    public long StartTimeTicks { get; set; }
    
    /// <summary>
    /// Minute progress start time in ticks
    /// </summary>
    public long MinuteStartTimeTicks { get; set; }
    
    /// <summary>
    /// 25-minute progress start time in ticks
    /// </summary>
    public long TwentyFiveMinStartTimeTicks { get; set; }
    
    /// <summary>
    /// Leg count
    /// </summary>
    public int LegCount { get; set; }
    
    /// <summary>
    /// Lap markers
    /// </summary>
    public List<double> LapMarkers { get; set; } = [];
}
