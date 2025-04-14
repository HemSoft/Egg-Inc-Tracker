using System.Numerics; // Add for BigInteger

namespace HemSoft.EggIncTracker.Dashboard.BlazorClient.Services;

public class DashboardState
{
    // Add this event declaration at the top of the class
    public event Action? OnChange;

    private DateTime _lastUpdated = DateTime.Now;
    public DateTime LastUpdated => _lastUpdated;

    // Store last updated timestamps for each player
    private Dictionary<string, DateTime> _playerLastUpdated = new Dictionary<string, DateTime>()
    {
        { "King Friday!", DateTime.MinValue },
        { "King Saturday!", DateTime.MinValue },
        { "King Sunday!", DateTime.MinValue },
        { "King Monday!", DateTime.MinValue }
    };

    // Legacy property for backward compatibility
    public DateTime PlayerLastUpdated => _playerLastUpdated.GetValueOrDefault("King Friday!", DateTime.MinValue);

    // New method to get last updated timestamp for a specific player
    public DateTime GetPlayerLastUpdated(string playerName)
    {
        return _playerLastUpdated.GetValueOrDefault(playerName, DateTime.MinValue);
    }

    public void SetLastUpdated(DateTime lastUpdated)
    {
        _lastUpdated = lastUpdated;
        OnChange?.Invoke();
    }

    // Update to accept player name
    public void SetPlayerLastUpdated(string playerName, DateTime playerLastUpdated)
    {
        if (_playerLastUpdated.ContainsKey(playerName))
        {
            _playerLastUpdated[playerName] = playerLastUpdated;
        }
        else
        {
            _playerLastUpdated.Add(playerName, playerLastUpdated);
        }
        OnChange?.Invoke();
    }

    // Legacy method for backward compatibility
    public void SetPlayerLastUpdated(DateTime playerLastUpdated)
    {
        SetPlayerLastUpdated("King Friday!", playerLastUpdated);
    }

    // Store SE This Week for each player
    private Dictionary<string, BigInteger?> _playerSEThisWeek = new Dictionary<string, BigInteger?>();

    // Method to get SE This Week for a specific player
    public BigInteger? GetPlayerSEThisWeek(string playerName)
    {
        return _playerSEThisWeek.GetValueOrDefault(playerName, null);
    }
}
