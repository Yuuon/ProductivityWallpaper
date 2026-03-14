using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Anniversary configuration view.
    /// Manages anniversary events, display styles, and event operations.
    /// </summary>
    public partial class AnniversaryViewModel : ObservableObject
    {
        // --- Properties ---

        /// <summary>
        /// Gets or sets the name of the current anniversary scheme.
        /// </summary>
        [ObservableProperty]
        private string _schemeName = "Anniversary";

        /// <summary>
        /// Gets or sets a value indicating whether the scheme name is being edited.
        /// </summary>
        [ObservableProperty]
        private bool _isEditingName;

        /// <summary>
        /// Gets or sets the collection of display styles.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AnniversaryStyleModel> _displayStyles = new();

        /// <summary>
        /// Gets or sets the collection of anniversary events.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AnniversaryEventModel> _anniversaries = new();

        /// <summary>
        /// Gets or sets the currently active display style.
        /// </summary>
        [ObservableProperty]
        private AnniversaryStyleModel? _activeStyle;

        /// <summary>
        /// Gets or sets a value indicating whether this is the currently active scheme.
        /// </summary>
        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// Gets a value indicating whether there are any anniversaries.
        /// </summary>
        public bool HasAnniversaries => Anniversaries.Count > 0;

        // --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="AnniversaryViewModel"/> class.
        /// </summary>
        public AnniversaryViewModel()
        {
            InitializeDefaultStyles();
        }

        // --- Initialization ---

        /// <summary>
        /// Initializes the default display styles.
        /// </summary>
        private void InitializeDefaultStyles()
        {
            DisplayStyles.Add(new AnniversaryStyleModel
            {
                Id = "anniversary-card",
                Name = "Card Style",
                PreviewImagePath = ""
            });

            DisplayStyles.Add(new AnniversaryStyleModel
            {
                Id = "anniversary-minimal",
                Name = "Minimal",
                PreviewImagePath = ""
            });

            // Set first style as active by default
            if (DisplayStyles.Count > 0)
            {
                DisplayStyles[0].IsActive = true;
                ActiveStyle = DisplayStyles[0];
            }
        }

        // --- Commands ---

        /// <summary>
        /// Toggles the activation of a display style.
        /// </summary>
        /// <param name="style">The style to toggle.</param>
        [RelayCommand]
        private void ToggleStyleActivation(AnniversaryStyleModel? style)
        {
            if (style == null)
                return;

            // Deactivate all styles
            foreach (var s in DisplayStyles)
            {
                s.IsActive = false;
            }

            // Activate selected style
            style.IsActive = true;
            ActiveStyle = style;
        }

        /// <summary>
        /// Adds a new anniversary event.
        /// </summary>
        [RelayCommand]
        private void AddAnniversary()
        {
            var newAnniversary = new AnniversaryEventModel
            {
                Name = "New Event",
                TargetDate = DateTime.Today,
                RepeatMode = RepeatMode.NoRepeat,
                IsNameEditing = true
            };

            Anniversaries.Add(newAnniversary);
            OnPropertyChanged(nameof(HasAnniversaries));
        }

        /// <summary>
        /// Deletes an anniversary event.
        /// </summary>
        /// <param name="anniversary">The anniversary to delete.</param>
        [RelayCommand]
        private void DeleteAnniversary(AnniversaryEventModel? anniversary)
        {
            if (anniversary == null)
                return;

            Anniversaries.Remove(anniversary);
            OnPropertyChanged(nameof(HasAnniversaries));
        }

        /// <summary>
        /// Starts editing the name of an anniversary.
        /// </summary>
        /// <param name="anniversary">The anniversary to edit.</param>
        [RelayCommand]
        private void StartEditName(AnniversaryEventModel? anniversary)
        {
            if (anniversary == null)
                return;

            // End editing for all other anniversaries
            foreach (var a in Anniversaries)
            {
                a.IsNameEditing = false;
            }

            anniversary.IsNameEditing = true;
        }

        /// <summary>
        /// Finishes editing the name of an anniversary.
        /// </summary>
        /// <param name="anniversary">The anniversary to finish editing.</param>
        [RelayCommand]
        private void FinishEditName(AnniversaryEventModel? anniversary)
        {
            if (anniversary == null)
                return;

            anniversary.IsNameEditing = false;

            // Ensure name is not empty
            if (string.IsNullOrWhiteSpace(anniversary.Name))
            {
                anniversary.Name = "New Event";
            }
        }

        /// <summary>
        /// Sets the repeat mode for an anniversary.
        /// </summary>
        /// <param name="parameters">Tuple containing anniversary and repeat mode.</param>
        [RelayCommand]
        private void SetRepeatMode((AnniversaryEventModel? Anniversary, RepeatMode Mode) parameters)
        {
            var (anniversary, mode) = parameters;
            if (anniversary == null)
                return;

            anniversary.RepeatMode = mode;
        }

        /// <summary>
        /// Sets the date for an anniversary.
        /// </summary>
        /// <param name="parameters">Tuple containing anniversary and date.</param>
        [RelayCommand]
        private void SetDate((AnniversaryEventModel? Anniversary, DateTime Date) parameters)
        {
            var (anniversary, date) = parameters;
            if (anniversary == null)
                return;

            anniversary.TargetDate = date;
        }

        /// <summary>
        /// Sets the weekly day for an anniversary.
        /// </summary>
        /// <param name="parameters">Tuple containing anniversary and day of week.</param>
        [RelayCommand]
        private void SetWeeklyDay((AnniversaryEventModel? Anniversary, DayOfWeek Day) parameters)
        {
            var (anniversary, day) = parameters;
            if (anniversary == null)
                return;

            anniversary.WeeklyDay = day;
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
        private void FinishEditSchemeName()
        {
            IsEditingName = false;

            // Ensure name is not empty
            if (string.IsNullOrWhiteSpace(SchemeName))
            {
                SchemeName = "Anniversary";
            }
        }

        /// <summary>
        /// Activates this scheme as the current anniversary configuration.
        /// </summary>
        [RelayCommand]
        private void ActivateScheme()
        {
            IsActive = true;
        }
    }
}
