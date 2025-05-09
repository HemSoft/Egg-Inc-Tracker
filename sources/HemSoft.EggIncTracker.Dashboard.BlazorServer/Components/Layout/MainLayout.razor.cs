using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Layout
{
    public partial class MainLayout
    {
        [Inject]
        private DashboardState DashboardState { get; set; } = default!;

        [Inject]
        private StopwatchPersistenceService StopwatchPersistenceService { get; set; } = default!;

        private DateTime _minuteStartTime = DateTime.Now;
        private DateTime _25minStartTime = DateTime.Now;
        private int _legCount = 0;
        private List<double> _lapMarkers = new List<double>();

        private bool _drawerOpen = true;
        private bool _isDarkMode = true;
        private MudTheme? _theme = null;

        private bool _isRunning;
        private DateTime _startTime;
        private TimeSpan _elapsed = TimeSpan.Zero;
        private System.Timers.Timer? _timer;
        private System.Timers.Timer? _clockTimer;
        private DateTime _currentTime = DateTime.Now;

        protected override async Task OnInitializedAsync()
        {
            // Timer for stopwatch
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += async (sender, e) =>
            {
                if (_isRunning)
                {
                    _elapsed = DateTime.Now - _startTime;
                    await SaveStopwatchState();
                    await InvokeAsync(StateHasChanged);
                }
            };

            // Timer for clock update
            _clockTimer = new System.Timers.Timer(1000);
            _clockTimer.Elapsed += async (sender, e) =>
            {
                _currentTime = DateTime.Now;
                await InvokeAsync(StateHasChanged);
            };
            _clockTimer.AutoReset = true;
            _clockTimer.Start();

            _theme = new()
            {
                PaletteDark = _darkPalette,
                LayoutProperties = new LayoutProperties()
            };

            DashboardState.OnChange += StateHasChanged;

            // Restore stopwatch state
            await RestoreStopwatchState();
        }

        private double GetProgressValue(double currentSeconds, double maxSeconds, DateTime? startTime = null)
        {
            if (startTime.HasValue)
            {
                var elapsed = (DateTime.Now - startTime.Value).TotalSeconds;
                return Math.Min((elapsed / maxSeconds) * 100, 100);
            }
            return Math.Min((currentSeconds / maxSeconds) * 100, 100);
        }

        private string GetProgressColorStyle(double currentSeconds, double maxSeconds, DateTime? startTime = null)
        {
            var progress = startTime.HasValue
            ? (DateTime.Now - startTime.Value).TotalSeconds / maxSeconds
            : currentSeconds / maxSeconds;

            double percentage = Math.Clamp(progress * 100, 0, 100);
            int red = (int)(255 * (1 - percentage / 100.0));
            int green = (int)(255 * (percentage / 100.0));
            int blue = 0;

            return $"background-color: rgb({red}, {green}, {blue})";
        }

        private void ResetMinuteProgress()
        {
            _minuteStartTime = DateTime.Now;
            StateHasChanged();
        }

        private void Reset25MinProgress()
        {
            _25minStartTime = DateTime.Now;
            StateHasChanged();
        }

        private async Task SaveStopwatchState()
        {
            try
            {
                var state = new StopwatchState
                {
                    IsRunning = _isRunning,
                    ElapsedTicks = _elapsed.Ticks,
                    StartTimeTicks = _startTime.Ticks,
                    MinuteStartTimeTicks = _minuteStartTime.Ticks,
                    TwentyFiveMinStartTimeTicks = _25minStartTime.Ticks,
                    LegCount = _legCount,
                    LapMarkers = _lapMarkers
                };

                await Task.Run(() => StopwatchPersistenceService.SaveStopwatchState(state));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving stopwatch state: {ex.Message}");
            }
        }

        private async Task RestoreStopwatchState()
        {
            try
            {
                var state = await Task.Run(() => StopwatchPersistenceService.LoadStopwatchState());

                if (state != null)
                {
                    _isRunning = state.IsRunning;
                    _elapsed = TimeSpan.FromTicks(state.ElapsedTicks);
                    _startTime = new DateTime(state.StartTimeTicks);
                    _minuteStartTime = new DateTime(state.MinuteStartTimeTicks);
                    _25minStartTime = new DateTime(state.TwentyFiveMinStartTimeTicks);
                    _legCount = state.LegCount;
                    _lapMarkers = state.LapMarkers;

                    if (_isRunning)
                    {
                        _timer?.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring stopwatch state: {ex.Message}");
            }
        }

        private async Task ToggleStopwatch()
        {
            if (_isRunning)
            {
                _timer?.Stop();
                _elapsed = DateTime.Now - _startTime;
            }
            else
            {
                _startTime = DateTime.Now - _elapsed;
                _timer?.Start();

                if (_elapsed.TotalSeconds < 0.1 && _legCount == 0)
                {
                    _legCount = 1;
                }
            }
            _isRunning = !_isRunning;
            await SaveStopwatchState();
            StateHasChanged();
        }

        private async Task ResetStopwatch()
        {
            _timer?.Stop();
            _isRunning = false;
            _elapsed = TimeSpan.Zero;
            _startTime = DateTime.Now;
            _legCount = 0;
            _lapMarkers.Clear();

            _minuteStartTime = DateTime.Now;
            _25minStartTime = DateTime.Now;

            await SaveStopwatchState();
            StateHasChanged();
        }

        public void Dispose()
        {
            DashboardState.OnChange -= StateHasChanged;

            // Dispose stopwatch timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            // Dispose clock timer
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer.Dispose();
                _clockTimer = null;
            }
        }

        private void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }

        private void DarkModeToggle()
        {
            _isDarkMode = !_isDarkMode;
        }

        private string GetFormattedTime()
        {
            return $"{(int)_elapsed.TotalHours:D2}:{_elapsed.Minutes:D2}:{_elapsed.Seconds:D2}";
        }

        private async Task IncrementLegs()
        {
            if (_isRunning)
            {
                _legCount++;
                _minuteStartTime = DateTime.Now;

                var elapsedSince25MinStart = (DateTime.Now - _25minStartTime).TotalSeconds;
                double progressValue = Math.Min((elapsedSince25MinStart / 1510) * 100, 100);
                _lapMarkers.Add(progressValue);

                await SaveStopwatchState();
                StateHasChanged();
            }
        }

        private readonly PaletteLight _lightPalette = new()
        {
            Black = "#110e2d",
            AppbarText = "#424242",
            AppbarBackground = "rgba(255,255,255,0.8)",
            DrawerBackground = "#ffffff",
            GrayLight = "#e8e8e8",
            GrayLighter = "#f9f9f9",
        };

        private readonly PaletteDark _darkPalette = new()
        {
            Primary = "#7e6fff",
            Surface = "#1e1e2d",
            Background = "#1a1a27",
            AppbarText = "#92929f",
            AppbarBackground = "rgba(26,26,39,0.8)",
            DrawerBackground = "#1a1a27",
            ActionDefault = "#74718e",
            ActionDisabled = "#9999994d",
            ActionDisabledBackground = "#605f6d4d",
            TextPrimary = "#b2b0bf",
            TextSecondary = "#92929f",
            TextDisabled = "#ffffff33",
            DrawerIcon = "#92929f",
            DrawerText = "#92929f",
            GrayLight = "#2a2833",
            GrayLighter = "#1e1e2d",
            Info = "#4a86ff",
            Success = "#3dcb6c",
            Warning = "#ffb545",
            Error = "#ff3f5f",
            LinesDefault = "#33323e",
            TableLines = "#33323e",
            Divider = "#292838",
            OverlayLight = "#1e1e2d80",
        };

        public string DarkLightModeButtonIcon => _isDarkMode switch
        {
            true => Icons.Material.Rounded.AutoMode,
            false => Icons.Material.Outlined.DarkMode,
        };
    }
}
