using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Desktop Clock configuration view.
    /// Manages clock style selection, format toggles, and opacity settings.
    /// </summary>
    public partial class DesktopClockViewModel : ObservableObject
    {
        // --- Observable Collections ---
        /// <summary>
        /// Gets the collection of available clock styles.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ClockStyleModel> _clockStyles = new();

        // --- Properties ---
        /// <summary>
        /// Gets the currently active clock style, or null if none is active.
        /// </summary>
        public ClockStyleModel? ActiveClock => ClockStyles.FirstOrDefault(c => c.IsActive);

        // --- Scheme Properties ---
        [ObservableProperty]
        private string _schemeName = "Desktop Clock";

        [ObservableProperty]
        private bool _isEditingName;

        [ObservableProperty]
        private bool _isActive;

        // --- Constructors ---
        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopClockViewModel"/> class.
        /// </summary>
        public DesktopClockViewModel()
        {
            InitializeDefaultStyles();
        }

        // --- Initialization ---
        /// <summary>
        /// Initializes the default clock styles collection.
        /// </summary>
        private void InitializeDefaultStyles()
        {
            ClockStyles.Add(new ClockStyleModel
            {
                Id = "digital-modern",
                Name = "Digital Modern",
                PreviewImagePath = "Resources/Img/Clocks/digital-modern.png",
                Format = ClockFormat.Hour24,
                Opacity = 1.0
            });

            ClockStyles.Add(new ClockStyleModel
            {
                Id = "analog-classic",
                Name = "Analog Classic",
                PreviewImagePath = "Resources/Img/Clocks/analog-classic.png",
                Format = ClockFormat.Hour12,
                Opacity = 1.0
            });

            ClockStyles.Add(new ClockStyleModel
            {
                Id = "minimalist",
                Name = "Minimalist",
                PreviewImagePath = "Resources/Img/Clocks/minimalist.png",
                Format = ClockFormat.Hour24,
                Opacity = 0.9
            });

            ClockStyles.Add(new ClockStyleModel
            {
                Id = "neon",
                Name = "Neon",
                PreviewImagePath = "Resources/Img/Clocks/neon.png",
                Format = ClockFormat.Hour12,
                Opacity = 1.0
            });
        }

        // --- Commands ---
        /// <summary>
        /// Toggles the activation state of a clock style.
        /// Only one style can be active at a time.
        /// </summary>
        /// <param name="clock">The clock style to toggle.</param>
        [RelayCommand]
        private void ToggleClockActivation(ClockStyleModel? clock)
        {
            if (clock == null)
                return;

            if (clock.IsActive)
            {
                // Deactivate if already active
                clock.IsActive = false;
                IsActive = false;
            }
            else
            {
                // Deactivate all other clocks first (single selection)
                foreach (var style in ClockStyles)
                {
                    style.IsActive = false;
                }

                // Activate the selected clock
                clock.IsActive = true;
                IsActive = true;
            }

            // Notify that ActiveClock may have changed
            OnPropertyChanged(nameof(ActiveClock));
        }

        /// <summary>
        /// Sets the time format for a clock style.
        /// </summary>
        /// <param name="clock">The clock style to update.</param>
        /// <param name="format">The format to set (12h or 24h).</param>
        [RelayCommand]
        private void SetClockFormat(ClockStyleModel? clock, ClockFormat format)
        {
            if (clock == null)
                return;

            clock.Format = format;
        }

        /// <summary>
        /// Sets the opacity for a clock style.
        /// </summary>
        /// <param name="clock">The clock style to update.</param>
        /// <param name="opacity">The opacity value (0.0 to 1.0).</param>
        [RelayCommand]
        private void SetClockOpacity(ClockStyleModel? clock, double opacity)
        {
            if (clock == null)
                return;

            clock.Opacity = opacity;
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
            // Activation is handled by selecting a clock style
            // This command can be used for additional activation logic
        }
    }
}
