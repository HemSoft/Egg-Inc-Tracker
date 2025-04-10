using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.Extensions.Logging;

namespace EggDash.Client.Services
{
    public class EventService : IDisposable
    {
        private readonly IApiService _apiService;
        private readonly ILogger<EventService> _logger;
        private List<CurrentEventDto>? _events;
        private Timer? _refreshTimer;
        private Timer? _intensiveRefreshTimer;
        private bool _isIntensiveRefreshActive = false;
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private const int STANDARD_REFRESH_INTERVAL = 5 * 60 * 1000; // 5 minutes
        private const int INTENSIVE_REFRESH_INTERVAL = 1 * 60 * 1000; // 1 minute
        private const int INTENSIVE_REFRESH_DURATION = 60 * 60 * 1000; // 1 hour

        // Event that components can subscribe to
        public event EventHandler<List<CurrentEventDto>?>? EventsUpdated;

        public EventService(IApiService apiService, ILogger<EventService> logger)
        {
            _apiService = apiService;
            _logger = logger;
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            // Standard refresh timer (every 5 minutes)
            _refreshTimer = new Timer(STANDARD_REFRESH_INTERVAL);
            _refreshTimer.Elapsed += async (sender, e) => await RefreshEventsAsync();
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();

            // Check if we need to start intensive refresh mode
            CheckAndStartIntensiveRefreshIfNeeded();
        }

        private void CheckAndStartIntensiveRefreshIfNeeded()
        {
            // Get current time in CST
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var currentTimeCst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);

            // Check if we're within the intensive refresh window (10:45 AM - 11:45 AM CST)
            if (currentTimeCst.Hour == 10 && currentTimeCst.Minute >= 45 ||
                currentTimeCst.Hour == 11 && currentTimeCst.Minute <= 45)
            {
                StartIntensiveRefresh();
            }
        }

        private void StartIntensiveRefresh()
        {
            if (_isIntensiveRefreshActive)
                return;

            _logger.LogInformation("Starting intensive event refresh mode");
            _isIntensiveRefreshActive = true;

            // Create and start the intensive refresh timer
            _intensiveRefreshTimer = new Timer(INTENSIVE_REFRESH_INTERVAL);
            _intensiveRefreshTimer.Elapsed += async (sender, e) => await RefreshEventsAsync();
            _intensiveRefreshTimer.AutoReset = true;
            _intensiveRefreshTimer.Start();

            // Set a timer to stop intensive refresh after the duration
            var stopTimer = new Timer(INTENSIVE_REFRESH_DURATION);
            stopTimer.Elapsed += (sender, e) =>
            {
                StopIntensiveRefresh();
                stopTimer.Stop();
                stopTimer.Dispose();
            };
            stopTimer.AutoReset = false;
            stopTimer.Start();
        }

        private void StopIntensiveRefresh()
        {
            if (!_isIntensiveRefreshActive)
                return;

            _logger.LogInformation("Stopping intensive event refresh mode");
            _isIntensiveRefreshActive = false;

            if (_intensiveRefreshTimer != null)
            {
                _intensiveRefreshTimer.Stop();
                _intensiveRefreshTimer.Dispose();
                _intensiveRefreshTimer = null;
            }
        }

        public async Task<List<CurrentEventDto>?> GetEventsAsync()
        {
            // If we haven't loaded events yet or it's been more than 5 minutes, refresh them
            if (_events == null || (DateTime.Now - _lastRefreshTime).TotalMinutes > 5)
            {
                await RefreshEventsAsync();
            }
            return _events;
        }

        public async Task RefreshEventsAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing events");
                var events = await _apiService.GetActiveEventsAsync();

                // Check if events have changed
                bool eventsChanged = HasEventsChanged(_events, events);

                if (eventsChanged)
                {
                    _logger.LogInformation($"Events have changed. New count: {events?.Count ?? 0}");
                    _events = events;
                    // Notify subscribers
                    EventsUpdated?.Invoke(this, _events);
                }

                _lastRefreshTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing events");
            }
        }

        private bool HasEventsChanged(List<CurrentEventDto>? oldEvents, List<CurrentEventDto>? newEvents)
        {
            // If either is null, consider it a change
            if (oldEvents == null || newEvents == null)
                return true;

            // If counts differ, events have changed
            if (oldEvents.Count != newEvents.Count)
                return true;

            // Compare each event
            for (int i = 0; i < oldEvents.Count; i++)
            {
                var oldEvent = oldEvents[i];
                var newEvent = newEvents[i];

                // Compare relevant properties
                if (oldEvent.SubTitle != newEvent.SubTitle ||
                    oldEvent.EndTime != newEvent.EndTime)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();

            _intensiveRefreshTimer?.Stop();
            _intensiveRefreshTimer?.Dispose();
        }
    }
}
