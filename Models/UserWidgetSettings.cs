using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// User-specific settings for all widgets.
    /// Stored separately from theme data in user_config.json.
    /// Theme = visual styles, User = active style, opacity, personal events.
    /// </summary>
    public partial class UserWidgetSettings : ObservableObject
    {
        /// <summary>
        /// Currently active theme ID.
        /// </summary>
        [ObservableProperty]
        private string _activeThemeId = string.Empty;

        /// <summary>
        /// Clock widget settings.
        /// </summary>
        [ObservableProperty]
        private ClockWidgetSettings _clockSettings = new();

        /// <summary>
        /// Pomodoro widget settings.
        /// </summary>
        [ObservableProperty]
        private PomodoroWidgetSettings _pomodoroSettings = new();

        /// <summary>
        /// Anniversary widget settings (contains user events).
        /// </summary>
        [ObservableProperty]
        private AnniversaryWidgetSettings _anniversarySettings = new();

        /// <summary>
        /// Global Pomodoro settings (used when per-theme is disabled).
        /// </summary>
        [ObservableProperty]
        private PomodoroGlobalSettings _globalPomodoroSettings = new();

        /// <summary>
        /// Whether to use per-theme Pomodoro settings instead of global.
        /// </summary>
        [ObservableProperty]
        private bool _usePerThemePomodoroSettings;

        /// <summary>
        /// Per-theme Pomodoro overrides (key: theme ID, value: settings).
        /// </summary>
        [ObservableProperty]
        private Dictionary<string, PomodoroPerThemeSettings> _perThemePomodoroSettings = new();
    }

    /// <summary>
    /// User settings for the desktop clock widget.
    /// </summary>
    public partial class ClockWidgetSettings : ObservableObject
    {
        /// <summary>
        /// ID of the currently active clock style (from theme).
        /// </summary>
        [ObservableProperty]
        private string _activeStyleId = string.Empty;

        /// <summary>
        /// Current opacity (0-100).
        /// </summary>
        [ObservableProperty]
        private int _opacity = 100;

        /// <summary>
        /// Whether to use 12-hour format.
        /// </summary>
        [ObservableProperty]
        private bool _use12HourFormat;

        /// <summary>
        /// Whether the clock is visible.
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;
    }

    /// <summary>
    /// Global Pomodoro settings (used across all themes).
    /// </summary>
    public partial class PomodoroGlobalSettings : ObservableObject
    {
        /// <summary>
        /// Work duration in minutes.
        /// </summary>
        [ObservableProperty]
        private int _workDurationMinutes = 25;

        /// <summary>
        /// Short break duration in minutes.
        /// </summary>
        [ObservableProperty]
        private int _shortBreakDurationMinutes = 5;

        /// <summary>
        /// Long break duration in minutes.
        /// </summary>
        [ObservableProperty]
        private int _longBreakDurationMinutes = 15;

        /// <summary>
        /// Whether Do Not Disturb is enabled during work sessions.
        /// </summary>
        [ObservableProperty]
        private bool _enableDoNotDisturb = true;

        /// <summary>
        /// Number of work sessions before a long break.
        /// </summary>
        [ObservableProperty]
        private int _sessionsBeforeLongBreak = 4;
    }

    /// <summary>
    /// Per-theme Pomodoro settings override.
    /// </summary>
    public partial class PomodoroPerThemeSettings : ObservableObject
    {
        [ObservableProperty]
        private int _workDurationMinutes = 25;

        [ObservableProperty]
        private int _shortBreakDurationMinutes = 5;

        [ObservableProperty]
        private int _longBreakDurationMinutes = 15;

        [ObservableProperty]
        private bool _enableDoNotDisturb = true;

        [ObservableProperty]
        private int _sessionsBeforeLongBreak = 4;
    }

    /// <summary>
    /// User settings for the Pomodoro widget.
    /// </summary>
    public partial class PomodoroWidgetSettings : ObservableObject
    {
        /// <summary>
        /// ID of the currently active Pomodoro style (from theme).
        /// </summary>
        [ObservableProperty]
        private string _activeStyleId = string.Empty;

        /// <summary>
        /// Whether the Pomodoro timer is visible.
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;

        /// <summary>
        /// Current opacity (0-100).
        /// </summary>
        [ObservableProperty]
        private int _opacity = 100;
    }

    /// <summary>
    /// User settings for the anniversary widget (contains personal events).
    /// </summary>
    public partial class AnniversaryWidgetSettings : ObservableObject
    {
        /// <summary>
        /// ID of the currently active anniversary style (from theme).
        /// </summary>
        [ObservableProperty]
        private string _activeStyleId = string.Empty;

        /// <summary>
        /// User's personal anniversary events.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AnniversaryEventModel> _events = new();

        /// <summary>
        /// Whether the anniversary widget is visible.
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;

        /// <summary>
        /// Current opacity (0-100).
        /// </summary>
        [ObservableProperty]
        private int _opacity = 100;
    }
}
