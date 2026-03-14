using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Pomodoro Timer configuration view.
    /// Manages timer style selection, duration settings, and Do Not Disturb mode.
    /// </summary>
    public partial class PomodoroViewModel : ObservableObject
    {
        // --- Observable Collections ---
        /// <summary>
        /// Gets the collection of available Pomodoro timer styles.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PomodoroStyleModel> _pomodoroStyles = new();

        // --- Properties ---
        /// <summary>
        /// Gets the currently active Pomodoro style, or null if none is active.
        /// </summary>
        public PomodoroStyleModel? ActiveStyle => PomodoroStyles.FirstOrDefault(p => p.IsActive);

        /// <summary>
        /// Gets a value indicating whether Do Not Disturb is enabled for the active style.
        /// </summary>
        public bool IsDndEnabled => ActiveStyle?.IsDndEnabled ?? false;

        // --- Scheme Properties ---
        [ObservableProperty]
        private string _schemeName = "Pomodoro Timer";

        [ObservableProperty]
        private bool _isEditingName;

        [ObservableProperty]
        private bool _isActive;

        // --- Constructors ---
        /// <summary>
        /// Initializes a new instance of the <see cref="PomodoroViewModel"/> class.
        /// </summary>
        public PomodoroViewModel()
        {
            InitializeDefaultStyles();
        }

        // --- Initialization ---
        /// <summary>
        /// Initializes the default Pomodoro timer styles collection.
        /// </summary>
        private void InitializeDefaultStyles()
        {
            PomodoroStyles.Add(new PomodoroStyleModel
            {
                Id = "pomodoro-classic",
                Name = "Classic Timer",
                PreviewImagePath = "",
                Format = ClockFormat.Hour24,
                Opacity = 1.0,
                WorkDuration = 25,
                BreakDuration = 5,
                IsDndEnabled = false
            });

            PomodoroStyles.Add(new PomodoroStyleModel
            {
                Id = "pomodoro-minimal",
                Name = "Minimal Focus",
                PreviewImagePath = "",
                Format = ClockFormat.Hour24,
                Opacity = 0.9,
                WorkDuration = 25,
                BreakDuration = 5,
                IsDndEnabled = true
            });

            PomodoroStyles.Add(new PomodoroStyleModel
            {
                Id = "pomodoro-gradient",
                Name = "Gradient Flow",
                PreviewImagePath = "",
                Format = ClockFormat.Hour12,
                Opacity = 1.0,
                WorkDuration = 45,
                BreakDuration = 15,
                IsDndEnabled = false
            });

            PomodoroStyles.Add(new PomodoroStyleModel
            {
                Id = "pomodoro-retro",
                Name = "Retro Flip",
                PreviewImagePath = "",
                Format = ClockFormat.Hour12,
                Opacity = 1.0,
                WorkDuration = 30,
                BreakDuration = 10,
                IsDndEnabled = true
            });
        }

        // --- Commands ---
        /// <summary>
        /// Toggles the activation state of a Pomodoro style.
        /// Only one style can be active at a time.
        /// </summary>
        /// <param name="style">The Pomodoro style to toggle.</param>
        [RelayCommand]
        private void ToggleStyleActivation(PomodoroStyleModel? style)
        {
            if (style == null)
                return;

            if (style.IsActive)
            {
                // Deactivate if already active
                style.IsActive = false;
                IsActive = false;
            }
            else
            {
                // Deactivate all other styles first (single selection)
                foreach (var s in PomodoroStyles)
                {
                    s.IsActive = false;
                }

                // Activate the selected style
                style.IsActive = true;
                IsActive = true;
            }

            // Notify that computed properties may have changed
            OnPropertyChanged(nameof(ActiveStyle));
            OnPropertyChanged(nameof(IsDndEnabled));
        }

        /// <summary>
        /// Sets the work duration for the active Pomodoro style.
        /// </summary>
        /// <param name="minutes">The work duration in minutes.</param>
        [RelayCommand]
        private void SetWorkDuration(int minutes)
        {
            if (ActiveStyle != null)
            {
                ActiveStyle.WorkDuration = minutes > 0 ? minutes : 1;
            }
        }

        /// <summary>
        /// Sets the break duration for the active Pomodoro style.
        /// </summary>
        /// <param name="minutes">The break duration in minutes.</param>
        [RelayCommand]
        private void SetBreakDuration(int minutes)
        {
            if (ActiveStyle != null)
            {
                ActiveStyle.BreakDuration = minutes > 0 ? minutes : 1;
            }
        }

        /// <summary>
        /// Toggles the Do Not Disturb mode for the active Pomodoro style.
        /// </summary>
        [RelayCommand]
        private void ToggleDndMode()
        {
            if (ActiveStyle != null)
            {
                ActiveStyle.IsDndEnabled = !ActiveStyle.IsDndEnabled;
                OnPropertyChanged(nameof(IsDndEnabled));
            }
        }

        /// <summary>
        /// Toggles the editing state for the scheme name.
        /// </summary>
        [RelayCommand]
        private void ToggleEditName()
        {
            IsEditingName = !IsEditingName;
        }

        /// <summary>
        /// Finishes editing the scheme name.
        /// </summary>
        [RelayCommand]
        private void FinishEditName()
        {
            IsEditingName = false;
        }

        /// <summary>
        /// Activates the currently selected scheme.
        /// </summary>
        [RelayCommand]
        private void ActivateScheme()
        {
            // Activation is handled by selecting a Pomodoro style
            // This command can be used for additional activation logic
        }
    }
}
