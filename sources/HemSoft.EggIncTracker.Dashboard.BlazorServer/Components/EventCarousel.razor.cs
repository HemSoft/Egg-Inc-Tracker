using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Extensions;
using HemSoft.EggIncTracker.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components
{
    public partial class EventCarousel : IDisposable
    {
        [Inject] private ILogger<EventCarousel> Logger { get; set; } = default!;
        [Parameter] public bool IsDarkMode { get; set; }

        // Class to represent carousel items (both events and custom items like Egg Day countdown)
        private class CarouselItem
        {
            public string Title { get; set; } = string.Empty;
            public string SubTitle { get; set; } = string.Empty;
            public string Icon { get; set; } = Icons.Material.Filled.Event;
            public Color IconColor { get; set; } = Color.Primary;
            public string TimeText { get; set; } = string.Empty;
            public bool IsCustom { get; set; } = false;
            public CurrentEventDto? EventData { get; set; }

            // Factory method to create from CurrentEventDto
            public static CarouselItem FromEvent(CurrentEventDto eventDto, Func<CurrentEventDto, string> getTimeRemaining,
                Func<string?, string> getEventIcon, Func<string?, Color> getEventColor)
            {
                return new CarouselItem
                {
                    Title = eventDto.Title,
                    SubTitle = eventDto.SubTitle,
                    Icon = getEventIcon(eventDto.EventType),
                    IconColor = getEventColor(eventDto.EventType),
                    TimeText = getTimeRemaining(eventDto),
                    EventData = eventDto
                };
            }

            // Factory method to create Egg Day countdown
            public static CarouselItem CreateEggDayCountdown()
            {
                // Calculate days until next Egg Day (July 14th)
                var today = DateTime.Today;
                var currentYear = today.Year;
                var eggDay = new DateTime(currentYear, 7, 14);

                // If Egg Day has already passed this year, use next year's date
                if (today > eggDay)
                {
                    eggDay = new DateTime(currentYear + 1, 7, 14);
                }

                var daysLeft = (eggDay - today).Days;

                return new CarouselItem
                {
                    Title = "Egg Day Countdown",
                    SubTitle = "Days Left Until Egg Day",
                    Icon = Icons.Material.Filled.Egg,
                    IconColor = Color.Warning,
                    TimeText = $"{daysLeft} days until July 14th",
                    IsCustom = true
                };
            }
        }

        private List<CurrentEventDto>? _events;
        private List<CarouselItem> _carouselItems = new();
        private CarouselItem? _currentItem;
        private int _currentIndex = 0;
        private System.Timers.Timer? _rotationTimer;
        private System.Timers.Timer? _refreshTimer;
        private System.Timers.Timer? _expiredEventRefreshTimer;
        private bool _isDarkMode;
        private bool _isLoading = true;
        private string? _errorMessage;
        private bool _expiredEventRefreshScheduled = false;
        private bool _isTransitioning = false;
        private string _transitionClass = "";

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger.LogInformation("EventCarousel: Initializing");
                // Initialize dark mode
                _isDarkMode = IsDarkMode;

                await LoadEventsAsync();

                // Set up rotation timer (10 seconds)
                _rotationTimer = new System.Timers.Timer(10000);
                _rotationTimer.Elapsed += OnRotationTimerElapsed;
                _rotationTimer.AutoReset = true;
                _rotationTimer.Start();

                // Set up refresh timer (5 minutes)
                _refreshTimer = new System.Timers.Timer(300000); // 5 minutes
                _refreshTimer.Elapsed += OnRefreshTimerElapsed;
                _refreshTimer.AutoReset = true;
                _refreshTimer.Start();

                Logger.LogInformation("EventCarousel: Initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing EventCarousel");
                _errorMessage = "Error loading events. Please try again later.";
            }
            finally
            {
                _isLoading = false;
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            // Check if dark mode has changed
            if (_isDarkMode != IsDarkMode)
            {
                Logger.LogInformation($"EventCarousel: Dark mode changed to {IsDarkMode}");
                _isDarkMode = IsDarkMode;
            }
        }

        private async Task LoadEventsAsync()
        {
            try
            {
                Logger.LogInformation("EventCarousel: Loading events");
                _isLoading = true;
                _errorMessage = null;

                // Clear existing carousel items
                _carouselItems.Clear();

                // Add Egg Day countdown as the first item
                _carouselItems.Add(CarouselItem.CreateEggDayCountdown());

                var events = await EventManager.GetActiveEventsAsync(Logger);
                if (!events.Any())
                {
                    Logger.LogInformation("EventCarousel: No active events found");
                    _events = new List<CurrentEventDto>();
                }
                else
                {
                    _events = events;

                    // Add event items to carousel
                    foreach (var eventDto in _events)
                    {
                        _carouselItems.Add(CarouselItem.FromEvent(
                            eventDto,
                            GetTimeRemaining,
                            GetEventIcon,
                            GetEventColor));
                    }

                    Logger.LogInformation($"EventCarousel: Loaded {_events.Count} events");

                    // Check if any events are expired
                    if (_events.Any(e => e.EndTime.HasValue && e.EndTime.Value < DateTime.Now))
                    {
                        Logger.LogInformation("EventCarousel: Found expired events during initialization");
                        // If all events are expired, schedule a refresh
                        if (_events.All(e => e.EndTime.HasValue && e.EndTime.Value < DateTime.Now))
                        {
                            ScheduleRefreshForExpiredEvent();
                        }
                    }
                }

                // Set current item to the first item
                _currentIndex = 0;
                _currentItem = _carouselItems.FirstOrDefault();

                Logger.LogInformation($"EventCarousel: Total carousel items: {_carouselItems.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading events");
                _errorMessage = "Error loading events. Please try again later.";
                _events = new List<CurrentEventDto>();
                _carouselItems.Clear();
                _currentItem = null;
            }
            finally
            {
                _isLoading = false;
            }
        }

        // Common method for transitioning between carousel items
        private async Task TransitionToItemAsync(int newIndex)
        {
            if (_isTransitioning || _carouselItems.Count <= 1 || newIndex < 0 || newIndex >= _carouselItems.Count || newIndex == _currentIndex)
                return;

            _isTransitioning = true;

            try
            {
                // Start fade-out transition
                _transitionClass = "fade-out";
                StateHasChanged(); // Force UI update to apply the fade-out class

                // Wait for fade-out animation to complete
                await Task.Delay(300);

                // Update the current index and item
                _currentIndex = newIndex;
                _currentItem = _carouselItems[_currentIndex];

                // Check if this is an event item and if it's expired
                if (_currentItem?.EventData?.EndTime.HasValue == true &&
                    _currentItem.EventData.EndTime.Value < DateTime.Now)
                {
                    // If all events are expired, schedule a refresh
                    if (_events?.All(e => e.EndTime.HasValue && e.EndTime.Value < DateTime.Now) == true)
                    {
                        ScheduleRefreshForExpiredEvent();
                    }
                }

                // Start fade-in transition
                _transitionClass = "fade-in";
                StateHasChanged(); // Force UI update to apply the fade-in class

                // Wait for fade-in animation to complete
                await Task.Delay(300);

                // Reset transition class
                _transitionClass = "";
            }
            finally
            {
                _isTransitioning = false;
                StateHasChanged(); // Final UI update
            }
        }

        private void OnRotationTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // Skip rotation if we're already transitioning or if there are no carousel items
                if (_isTransitioning || _carouselItems.Count == 0)
                {
                    return;
                }

                // If we only have one item, no need to rotate
                if (_carouselItems.Count == 1)
                {
                    // Check if the single item is an event and is expired
                    if (_currentItem?.EventData?.EndTime.HasValue == true &&
                        _currentItem.EventData.EndTime.Value < DateTime.Now)
                    {
                        ScheduleRefreshForExpiredEvent();
                    }
                    return;
                }

                // Calculate the next index
                int nextIndex = (_currentIndex + 1) % _carouselItems.Count;

                // Use InvokeAsync to ensure we're on the UI thread
                InvokeAsync(async () =>
                {
                    try
                    {
                        // Use the common transition method
                        await TransitionToItemAsync(nextIndex);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error during carousel rotation transition");
                        _isTransitioning = false; // Ensure flag is reset on error
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in rotation timer");
                _isTransitioning = false; // Ensure flag is reset on error
            }
        }

        // Method to manually navigate to the next item
        private async Task NextItemAsync()
        {
            if (_isTransitioning || _carouselItems.Count <= 1)
                return;

            // Calculate the next index
            int nextIndex = (_currentIndex + 1) % _carouselItems.Count;

            // Use the common transition method
            await TransitionToItemAsync(nextIndex);
        }

        // Method to manually navigate to the previous item
        private async Task PreviousItemAsync()
        {
            if (_isTransitioning || _carouselItems.Count <= 1)
                return;

            // Calculate the previous index
            int prevIndex = (_currentIndex - 1 + _carouselItems.Count) % _carouselItems.Count;

            // Use the common transition method
            await TransitionToItemAsync(prevIndex);
        }

        // Method to navigate to a specific item by index
        private async Task GoToItemAsync(int index)
        {
            // Use the common transition method
            await TransitionToItemAsync(index);
        }

        private void OnRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // Use InvokeAsync to ensure we're on the UI thread
                InvokeAsync(async () =>
                {
                    try
                    {
                        await LoadEventsAsync();
                        // Trigger a UI refresh
                        StateHasChanged();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error loading events during refresh");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in refresh timer");
            }
        }

        private void ScheduleRefreshForExpiredEvent()
        {
            // Only schedule a refresh if one isn't already scheduled
            if (!_expiredEventRefreshScheduled)
            {
                Logger.LogInformation("EventCarousel: Scheduling refresh for expired event");
                _expiredEventRefreshScheduled = true;

                // Dispose of any existing timer
                if (_expiredEventRefreshTimer != null)
                {
                    _expiredEventRefreshTimer.Stop();
                    _expiredEventRefreshTimer.Elapsed -= OnExpiredEventRefreshTimerElapsed;
                    _expiredEventRefreshTimer.Dispose();
                }

                // Create a new timer that will trigger after 5 seconds
                _expiredEventRefreshTimer = new System.Timers.Timer(5000); // 5 seconds
                _expiredEventRefreshTimer.Elapsed += OnExpiredEventRefreshTimerElapsed;
                _expiredEventRefreshTimer.AutoReset = false; // Only trigger once
                _expiredEventRefreshTimer.Start();
            }
        }

        private void OnExpiredEventRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Logger.LogInformation("EventCarousel: Refreshing events due to expired event");

                // Use InvokeAsync to ensure we're on the UI thread
                InvokeAsync(async () =>
                {
                    try
                    {
                        await LoadEventsAsync();
                        _expiredEventRefreshScheduled = false;

                        // Trigger a UI refresh
                        StateHasChanged();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error refreshing events for expired event");
                        _expiredEventRefreshScheduled = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in expired event refresh timer");
                _expiredEventRefreshScheduled = false;
            }
        }

        public void Dispose()
        {
            if (_rotationTimer != null)
            {
                _rotationTimer.Stop();
                _rotationTimer.Elapsed -= OnRotationTimerElapsed;
                _rotationTimer.Dispose();
            }

            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Elapsed -= OnRefreshTimerElapsed;
                _refreshTimer.Dispose();
            }

            if (_expiredEventRefreshTimer != null)
            {
                _expiredEventRefreshTimer.Stop();
                _expiredEventRefreshTimer.Elapsed -= OnExpiredEventRefreshTimerElapsed;
                _expiredEventRefreshTimer.Dispose();
            }
        }

        private string GetEventIcon(string? eventType)
        {
            return eventType switch
            {
                "hab-sale" => Icons.Material.Filled.Home,
                "piggy-boost" => Icons.Material.Filled.Savings,
                "mission-fuel" => Icons.Material.Filled.Rocket,
                "boost-duration" => Icons.Material.Filled.Speed,
                "prestige-boost" => Icons.Material.Filled.EmojiEvents,
                _ => Icons.Material.Filled.Event
            };
        }

        private Color GetEventColor(string? eventType)
        {
            return eventType switch
            {
                "hab-sale" => Color.Primary,
                "piggy-boost" => Color.Success,
                "mission-fuel" => Color.Warning,
                "boost-duration" => Color.Info,
                "prestige-boost" => Color.Secondary,
                _ => Color.Default
            };
        }

        private string GetTimeRemaining(CurrentEventDto eventDto)
        {
            if (eventDto.EndTime.HasValue)
            {
                var timeRemaining = eventDto.EndTime.Value - DateTime.Now;
                if (timeRemaining.TotalSeconds <= 0)
                {
                    // If we detect an expired event, schedule a refresh
                    ScheduleRefreshForExpiredEvent();
                    return "Expired"; // Handle expired events
                }
                if (timeRemaining.TotalDays >= 1)
                {
                    return $"{Math.Floor(timeRemaining.TotalDays)}d {timeRemaining.Hours}h remaining";
                }
                else if (timeRemaining.TotalHours >= 1)
                {
                    return $"{Math.Floor(timeRemaining.TotalHours)}h {timeRemaining.Minutes}m remaining";
                }
                else
                {
                    return $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s remaining";
                }
            }

            return "Time remaining unknown";
        }
    }
}
