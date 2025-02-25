namespace EggDash.Client.Services;

public class DashboardState
{
    public DateTime LastUpdated { get; private set; }
    public event Action? OnChange;

    public void SetLastUpdated(DateTime dateTime)
    {
        LastUpdated = dateTime;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}