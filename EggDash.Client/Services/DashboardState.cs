namespace EggDash.Client.Services;

public class DashboardState
{
    // Add this event declaration at the top of the class
    public event Action? OnChange;

    private DateTime _lastUpdated = DateTime.Now;
    public DateTime LastUpdated => _lastUpdated;

    private DateTime _playerLastUpdated = DateTime.MinValue;
    public DateTime PlayerLastUpdated => _playerLastUpdated;

    public void SetLastUpdated(DateTime lastUpdated)
    {
        _lastUpdated = lastUpdated;
        OnChange?.Invoke();
    }

    public void SetPlayerLastUpdated(DateTime playerLastUpdated)
    {
        _playerLastUpdated = playerLastUpdated;
        OnChange?.Invoke();
    }
}