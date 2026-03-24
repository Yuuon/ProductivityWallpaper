using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the clock time format (12-hour or 24-hour).
    /// </summary>
    public enum ClockFormat
    {
        /// <summary>
        /// 12-hour format with AM/PM (e.g., 2:30 PM).
        /// </summary>
        Hour12,

        /// <summary>
        /// 24-hour format (e.g., 14:30).
        /// </summary>
        Hour24
    }

    /// <summary>
    /// Represents the type of widget a style applies to.
    /// </summary>
    public enum WidgetType
    {
        /// <summary>
        /// Desktop clock widget.
        /// </summary>
        Clock,

        /// <summary>
        /// Pomodoro timer widget.
        /// </summary>
        Pomodoro,

        /// <summary>
        /// Anniversary countdown widget.
        /// </summary>
        Anniversary
    }

    /// <summary>
    /// Represents a clock style configuration with display settings.
    /// </summary>
    public partial class ClockStyleModel : ObservableObject
    {
        // --- Fields ---
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _previewImagePath = string.Empty;
        private bool _isActive;
        private ClockFormat _format = ClockFormat.Hour24;
        private double _opacity = 1.0;
        private WidgetType _widgetType = WidgetType.Clock;
        private string? _previewImageId;

        // --- Properties ---
        /// <summary>
        /// Gets or sets the unique identifier for this clock style.
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the display name of the clock style.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the path to the preview image for this style.
        /// </summary>
        public string PreviewImagePath
        {
            get => _previewImagePath;
            set => SetProperty(ref _previewImagePath, value);
        }

        /// <summary>
        /// Gets or sets the description of this style.
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Gets or sets the type of widget this style applies to.
        /// </summary>
        public WidgetType WidgetType
        {
            get => _widgetType;
            set => SetProperty(ref _widgetType, value);
        }

        /// <summary>
        /// Gets or sets the preview image resource ID (in theme library).
        /// </summary>
        public string? PreviewImageId
        {
            get => _previewImageId;
            set => SetProperty(ref _previewImageId, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this style is currently active.
        /// Only one clock style can be active at a time.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Gets or sets the time format (12h or 24h).
        /// </summary>
        public ClockFormat Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        /// <summary>
        /// Gets or sets the opacity of the clock display (0.0 to 1.0).
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set
            {
                // Clamp opacity between 0.0 and 1.0
                var clampedValue = value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
                SetProperty(ref _opacity, clampedValue);
            }
        }

        // --- Constructors ---
        /// <summary>
        /// Initializes a new instance of the <see cref="ClockStyleModel"/> class.
        /// </summary>
        public ClockStyleModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockStyleModel"/> class with the specified parameters.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="name">The display name.</param>
        /// <param name="previewImagePath">The path to the preview image.</param>
        public ClockStyleModel(string id, string name, string previewImagePath)
        {
            _id = id;
            _name = name;
            _previewImagePath = previewImagePath;
        }

        /// <summary>
        /// Creates a new ClockStyleModel with a generated UUID.
        /// Use this instead of the default constructor when creating new styles.
        /// </summary>
        public static ClockStyleModel CreateNew(string name, string previewImagePath = "")
        {
            return new ClockStyleModel(Guid.NewGuid().ToString(), name, previewImagePath);
        }
    }

    /// <summary>
    /// Represents a Pomodoro timer style configuration with timer settings.
    /// Inherits from ClockStyleModel for base styling capabilities.
    /// </summary>
    public partial class PomodoroStyleModel : ClockStyleModel
    {
        // --- Fields ---
        private bool _isDndEnabled;
        private int _workDuration = 25;  // Default: 25 minutes
        private int _breakDuration = 5;  // Default: 5 minutes

        // --- Properties ---
        /// <summary>
        /// Gets or sets a value indicating whether Do Not Disturb mode is enabled during work sessions.
        /// </summary>
        public bool IsDndEnabled
        {
            get => _isDndEnabled;
            set => SetProperty(ref _isDndEnabled, value);
        }

        /// <summary>
        /// Gets or sets the work session duration in minutes.
        /// Must be greater than 0.
        /// </summary>
        public int WorkDuration
        {
            get => _workDuration;
            set
            {
                // Ensure positive value
                var validValue = value < 1 ? 1 : value;
                SetProperty(ref _workDuration, validValue);
            }
        }

        /// <summary>
        /// Gets or sets the break duration in minutes.
        /// Must be greater than 0.
        /// </summary>
        public int BreakDuration
        {
            get => _breakDuration;
            set
            {
                // Ensure positive value
                var validValue = value < 1 ? 1 : value;
                SetProperty(ref _breakDuration, validValue);
            }
        }

        // --- Constructors ---
        /// <summary>
        /// Initializes a new instance of the <see cref="PomodoroStyleModel"/> class.
        /// </summary>
        public PomodoroStyleModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PomodoroStyleModel"/> class with the specified parameters.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="name">The display name.</param>
        /// <param name="previewImagePath">The path to the preview image.</param>
        public PomodoroStyleModel(string id, string name, string previewImagePath)
            : base(id, name, previewImagePath)
        {
        }
    }
}
