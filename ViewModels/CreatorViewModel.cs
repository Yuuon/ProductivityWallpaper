using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Creator view, managing theme creation and scheme configuration.
    /// </summary>
    public partial class CreatorViewModel : ObservableObject
    {
        // --- DI Services ---
        private readonly Func<DesktopBackgroundViewModel> _desktopBackgroundVmFactory;
        private readonly Func<MouseClickViewModel> _mouseClickVmFactory;
        private readonly Func<DesktopClockViewModel> _desktopClockVmFactory;
        private readonly Func<PomodoroViewModel> _pomodoroVmFactory;
        private readonly Func<AnniversaryViewModel> _anniversaryVmFactory;
        private readonly Func<ShutdownViewModel> _shutdownVmFactory;
        private readonly Func<BootRestartViewModel> _bootRestartVmFactory;
        private readonly Func<ScreenWakeViewModel> _screenWakeVmFactory;
        // --- Feature Types Supporting Multi-Scheme ---
        private static readonly FeatureType[] MultiSchemeFeatures = new[]
        {
            FeatureType.DesktopBackground,
            FeatureType.MouseClick,
            FeatureType.Shutdown,
            FeatureType.BootRestart,
            FeatureType.ScreenWake
        };

        // --- Scheme Collections by Feature ---
        /// <summary>
        /// Dictionary mapping feature types to their scheme collections.
        /// </summary>
        private readonly Dictionary<FeatureType, ObservableCollection<SchemeModel>> _schemesByFeature;

        // --- Expansion States for Multi-Scheme Features ---
        [ObservableProperty]
        private bool _isDesktopBackgroundExpanded;

        partial void OnIsDesktopBackgroundExpandedChanged(bool value)
        {
            if (value)
            {
                // Single-expand: collapse others
                IsMouseClickExpanded = false;
                IsShutdownExpanded = false;
                IsBootRestartExpanded = false;
                IsScreenWakeExpanded = false;

                // Auto-create scheme if none exist
                EnsureDefaultScheme(FeatureType.DesktopBackground);

                // Select the feature and show content
                SelectedFeature = "DesktopBackground";
                LoadFeatureContent("DesktopBackground");
            }
            else
            {
                // Collapsed: clear highlight if this feature is not selected
                if (SelectedFeature != "DesktopBackground")
                {
                    // Deselect any selected scheme for this feature
                    if (SelectedDesktopBackgroundScheme != null)
                    {
                        SelectedDesktopBackgroundScheme.IsSelected = false;
                        SelectedDesktopBackgroundScheme = null;
                    }
                }
            }
        }

        [ObservableProperty]
        private bool _isMouseClickExpanded;

        partial void OnIsMouseClickExpandedChanged(bool value)
        {
            if (value)
            {
                // Single-expand: collapse others
                IsDesktopBackgroundExpanded = false;
                IsShutdownExpanded = false;
                IsBootRestartExpanded = false;
                IsScreenWakeExpanded = false;
                
                // Auto-create scheme if none exist
                EnsureDefaultScheme(FeatureType.MouseClick);
                
                // Select the feature and show content
                SelectedFeature = "MouseClick";
                LoadFeatureContent("MouseClick");
            }
        }

        [ObservableProperty]
        private bool _isShutdownExpanded;

        partial void OnIsShutdownExpandedChanged(bool value)
        {
            if (value)
            {
                // Single-expand: collapse others
                IsDesktopBackgroundExpanded = false;
                IsMouseClickExpanded = false;
                IsBootRestartExpanded = false;
                IsScreenWakeExpanded = false;
                
                // Auto-create scheme if none exist
                EnsureDefaultScheme(FeatureType.Shutdown);
                
                // Select the feature and show content
                SelectedFeature = "Shutdown";
                LoadFeatureContent("Shutdown");
            }
        }

        [ObservableProperty]
        private bool _isBootRestartExpanded;

        partial void OnIsBootRestartExpandedChanged(bool value)
        {
            if (value)
            {
                // Single-expand: collapse others
                IsDesktopBackgroundExpanded = false;
                IsMouseClickExpanded = false;
                IsShutdownExpanded = false;
                IsScreenWakeExpanded = false;
                
                // Auto-create scheme if none exist
                EnsureDefaultScheme(FeatureType.BootRestart);
                
                // Select the feature and show content
                SelectedFeature = "BootRestart";
                LoadFeatureContent("BootRestart");
            }
        }

        [ObservableProperty]
        private bool _isScreenWakeExpanded;

        partial void OnIsScreenWakeExpandedChanged(bool value)
        {
            if (value)
            {
                // Single-expand: collapse others
                IsDesktopBackgroundExpanded = false;
                IsMouseClickExpanded = false;
                IsShutdownExpanded = false;
                IsBootRestartExpanded = false;
                
                // Auto-create scheme if none exist
                EnsureDefaultScheme(FeatureType.ScreenWake);
                
                // Select the feature and show content
                SelectedFeature = "ScreenWake";
                LoadFeatureContent("ScreenWake");
            }
        }

        // --- Selected Schemes for Each Feature ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDesktopBackgroundHeaderHighlighted))]
        private SchemeModel? _selectedDesktopBackgroundScheme;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsMouseClickHeaderHighlighted))]
        private SchemeModel? _selectedMouseClickScheme;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShutdownHeaderHighlighted))]
        private SchemeModel? _selectedShutdownScheme;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBootRestartHeaderHighlighted))]
        private SchemeModel? _selectedBootRestartScheme;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsScreenWakeHeaderHighlighted))]
        private SchemeModel? _selectedScreenWakeScheme;

        // --- Page States ---
        [ObservableProperty]
        private bool _isWelcomePage = true;

        [ObservableProperty]
        private bool _isCreatingPage;

        [ObservableProperty]
        private string _newThemeName = string.Empty;

        [ObservableProperty]
        private string _currentThemeName = string.Empty;

        [ObservableProperty]
        private bool _isEditingThemeName;

        // --- Feature Selection States ---
        /// <summary>
        /// The currently selected feature name.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsThemePreviewSelected))]
        [NotifyPropertyChangedFor(nameof(IsDesktopBackgroundSelected))]
        [NotifyPropertyChangedFor(nameof(IsMouseClickSelected))]
        [NotifyPropertyChangedFor(nameof(IsShutdownSelected))]
        [NotifyPropertyChangedFor(nameof(IsBootRestartSelected))]
        [NotifyPropertyChangedFor(nameof(IsScreenWakeSelected))]
        [NotifyPropertyChangedFor(nameof(IsOpenAppSelected))]
        [NotifyPropertyChangedFor(nameof(IsDesktopClockSelected))]
        [NotifyPropertyChangedFor(nameof(IsPomodoroSelected))]
        [NotifyPropertyChangedFor(nameof(IsAnniversarySelected))]
        private string _selectedFeature = "ThemePreview";

        /// <summary>
        /// Gets whether Theme Preview is selected.
        /// </summary>
        public bool IsThemePreviewSelected => SelectedFeature == "ThemePreview";

        /// <summary>
        /// Gets whether Desktop Background is selected.
        /// </summary>
        public bool IsDesktopBackgroundSelected => SelectedFeature == "DesktopBackground";

        /// <summary>
        /// Gets whether Mouse Click is selected.
        /// </summary>
        public bool IsMouseClickSelected => SelectedFeature == "MouseClick";

        /// <summary>
        /// Gets whether Shutdown is selected.
        /// </summary>
        public bool IsShutdownSelected => SelectedFeature == "Shutdown";

        /// <summary>
        /// Gets whether Boot/Restart is selected.
        /// </summary>
        public bool IsBootRestartSelected => SelectedFeature == "BootRestart";

        /// <summary>
        /// Gets whether Screen Wake is selected.
        /// </summary>
        public bool IsScreenWakeSelected => SelectedFeature == "ScreenWake";

        /// <summary>
        /// Gets whether Open App is selected.
        /// </summary>
        public bool IsOpenAppSelected => SelectedFeature == "OpenApp";

        /// <summary>
        /// Gets whether Desktop Clock is selected.
        /// </summary>
        public bool IsDesktopClockSelected => SelectedFeature == "DesktopClock";

        /// <summary>
        /// Gets whether Pomodoro is selected.
        /// </summary>
        public bool IsPomodoroSelected => SelectedFeature == "Pomodoro";

        /// <summary>
        /// Gets whether Anniversary is selected.
        /// </summary>
        public bool IsAnniversarySelected => SelectedFeature == "Anniversary";

        // --- Header Highlight Properties (for expandable features) ---
        /// <summary>
        /// Gets whether Desktop Background header should be highlighted.
        /// True when directly selected or when any of its schemes is selected.
        /// </summary>
        public bool IsDesktopBackgroundHeaderHighlighted => 
            SelectedFeature == "DesktopBackground" || 
            (SelectedDesktopBackgroundScheme?.IsSelected == true);

        /// <summary>
        /// Gets whether Mouse Click header should be highlighted.
        /// True when directly selected or when any of its schemes is selected.
        /// </summary>
        public bool IsMouseClickHeaderHighlighted => 
            SelectedFeature == "MouseClick" || 
            (SelectedMouseClickScheme?.IsSelected == true);

        /// <summary>
        /// Gets whether Shutdown header should be highlighted.
        /// True when directly selected or when any of its schemes is selected.
        /// </summary>
        public bool IsShutdownHeaderHighlighted => 
            SelectedFeature == "Shutdown" || 
            (SelectedShutdownScheme?.IsSelected == true);

        /// <summary>
        /// Gets whether Boot/Restart header should be highlighted.
        /// True when directly selected or when any of its schemes is selected.
        /// </summary>
        public bool IsBootRestartHeaderHighlighted => 
            SelectedFeature == "BootRestart" || 
            (SelectedBootRestartScheme?.IsSelected == true);

        /// <summary>
        /// Gets whether Screen Wake header should be highlighted.
        /// True when directly selected or when any of its schemes is selected.
        /// </summary>
        public bool IsScreenWakeHeaderHighlighted => 
            SelectedFeature == "ScreenWake" || 
            (SelectedScreenWakeScheme?.IsSelected == true);

        // --- Content Properties ---
        [ObservableProperty]
        private bool _hasPreviewContent;

        [ObservableProperty]
        private object? _previewContent;

        [ObservableProperty]
        private object? _configurationContent;

        /// <summary>
        /// Gets the dictionary mapping feature types to their scheme collections.
        /// </summary>
        public Dictionary<FeatureType, ObservableCollection<SchemeModel>> SchemesByFeature => _schemesByFeature;

        /// <summary>
        /// Gets the schemes for Desktop Background feature.
        /// </summary>
        public ObservableCollection<SchemeModel> DesktopBackgroundSchemes => 
            _schemesByFeature[FeatureType.DesktopBackground];

        /// <summary>
        /// Gets the schemes for Mouse Click feature.
        /// </summary>
        public ObservableCollection<SchemeModel> MouseClickSchemes => 
            _schemesByFeature[FeatureType.MouseClick];

        /// <summary>
        /// Gets the schemes for Shutdown feature.
        /// </summary>
        public ObservableCollection<SchemeModel> ShutdownSchemes => 
            _schemesByFeature[FeatureType.Shutdown];

        /// <summary>
        /// Gets the schemes for Boot/Restart feature.
        /// </summary>
        public ObservableCollection<SchemeModel> BootRestartSchemes => 
            _schemesByFeature[FeatureType.BootRestart];

        /// <summary>
        /// Gets the schemes for Screen Wake feature.
        /// </summary>
        public ObservableCollection<SchemeModel> ScreenWakeSchemes => 
            _schemesByFeature[FeatureType.ScreenWake];

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatorViewModel"/> class.
        /// </summary>
        public CreatorViewModel() : this(null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatorViewModel"/> class with DI.
        /// </summary>
        /// <param name="desktopBackgroundVmFactory">Factory for creating DesktopBackgroundViewModel instances.</param>
        /// <param name="mouseClickVmFactory">Factory for creating MouseClickViewModel instances.</param>
        /// <param name="desktopClockVmFactory">Factory for creating DesktopClockViewModel instances.</param>
        /// <param name="pomodoroVmFactory">Factory for creating PomodoroViewModel instances.</param>
        /// <param name="anniversaryVmFactory">Factory for creating AnniversaryViewModel instances.</param>
        /// <param name="shutdownVmFactory">Factory for creating ShutdownViewModel instances.</param>
        /// <param name="bootRestartVmFactory">Factory for creating BootRestartViewModel instances.</param>
        /// <param name="screenWakeVmFactory">Factory for creating ScreenWakeViewModel instances.</param>
        public CreatorViewModel(
            Func<DesktopBackgroundViewModel> desktopBackgroundVmFactory,
            Func<MouseClickViewModel> mouseClickVmFactory,
            Func<DesktopClockViewModel> desktopClockVmFactory,
            Func<PomodoroViewModel> pomodoroVmFactory,
            Func<AnniversaryViewModel> anniversaryVmFactory,
            Func<ShutdownViewModel> shutdownVmFactory,
            Func<BootRestartViewModel> bootRestartVmFactory,
            Func<ScreenWakeViewModel> screenWakeVmFactory)
        {
            _desktopBackgroundVmFactory = desktopBackgroundVmFactory ?? (() => new DesktopBackgroundViewModel());
            _mouseClickVmFactory = mouseClickVmFactory ?? (() => new MouseClickViewModel());
            _desktopClockVmFactory = desktopClockVmFactory ?? (() => new DesktopClockViewModel());
            _pomodoroVmFactory = pomodoroVmFactory ?? (() => new PomodoroViewModel());
            _anniversaryVmFactory = anniversaryVmFactory ?? (() => new AnniversaryViewModel());
            _shutdownVmFactory = shutdownVmFactory ?? (() => new ShutdownViewModel());
            _bootRestartVmFactory = bootRestartVmFactory ?? (() => new BootRestartViewModel());
            _screenWakeVmFactory = screenWakeVmFactory ?? (() => new ScreenWakeViewModel());

            // Initialize scheme collections for all multi-scheme features
            _schemesByFeature = new Dictionary<FeatureType, ObservableCollection<SchemeModel>>();
            foreach (var featureType in MultiSchemeFeatures)
            {
                _schemesByFeature[featureType] = new ObservableCollection<SchemeModel>();
            }
        }

        // --- Commands ---

        /// <summary>
        /// Starts creating a new theme with the specified name.
        /// </summary>
        [RelayCommand]
        private void StartCreating()
        {
            // Use user input if provided, otherwise use default placeholder text
            CurrentThemeName = string.IsNullOrWhiteSpace(NewThemeName) 
                ? "输入主题名字..." 
                : NewThemeName.Trim();

            IsWelcomePage = false;
            IsCreatingPage = true;
            SelectFeature("ThemePreview");
        }

        /// <summary>
        /// Returns to the welcome page and resets the state.
        /// </summary>
        [RelayCommand]
        private void BackToWelcome()
        {
            IsWelcomePage = true;
            IsCreatingPage = false;
            NewThemeName = string.Empty;
        }

        /// <summary>
        /// Selects a feature and updates the UI state.
        /// Collapses all expanded menus when selecting a simple feature.
        /// </summary>
        /// <param name="featureName">The name of the feature to select.</param>
        [RelayCommand]
        private void SelectFeature(string featureName)
        {
            // Set selected feature - all IsXXXSelected properties are computed from this
            SelectedFeature = featureName;

            // Collapse submenus for simple features and Theme Preview
            switch (featureName)
            {
                case "ThemePreview":
                case "OpenApp":
                case "DesktopClock":
                case "Pomodoro":
                case "Anniversary":
                    CollapseAllNavExcept();
                    break;
            }

            // Load preview and configuration content
            LoadFeatureContent(featureName);
        }

        /// <summary>
        /// Toggles the editing state for the theme name.
        /// </summary>
        [RelayCommand]
        private void ToggleEditThemeName()
        {
            IsEditingThemeName = true;
        }

        /// <summary>
        /// Finishes editing the theme name.
        /// </summary>
        [RelayCommand]
        private void FinishEditThemeName()
        {
            IsEditingThemeName = false;
        }

        /// <summary>
        /// Collapses all navigation menus except the specified one.
        /// </summary>
        /// <param name="exceptFeature">The feature to keep expanded (if any).</param>
        private void CollapseAllNavExcept(FeatureType? exceptFeature = null)
        {
            if (exceptFeature != FeatureType.DesktopBackground)
                IsDesktopBackgroundExpanded = false;
            if (exceptFeature != FeatureType.MouseClick)
                IsMouseClickExpanded = false;
            if (exceptFeature != FeatureType.Shutdown)
                IsShutdownExpanded = false;
            if (exceptFeature != FeatureType.BootRestart)
                IsBootRestartExpanded = false;
            if (exceptFeature != FeatureType.ScreenWake)
                IsScreenWakeExpanded = false;
        }

        /// <summary>
        /// Toggles the expansion state of a feature's submenu.
        /// Auto-creates a default scheme if the feature has none.
        /// Collapses all other expanded menus when expanding.
        /// </summary>
        /// <param name="featureType">The feature type to toggle.</param>
        [RelayCommand]
        private void ToggleFeatureExpansion(FeatureType featureType)
        {
            // Check current state before toggling
            bool isCurrentlyExpanded = featureType switch
            {
                FeatureType.DesktopBackground => IsDesktopBackgroundExpanded,
                FeatureType.MouseClick => IsMouseClickExpanded,
                FeatureType.Shutdown => IsShutdownExpanded,
                FeatureType.BootRestart => IsBootRestartExpanded,
                FeatureType.ScreenWake => IsScreenWakeExpanded,
                _ => false
            };

            // If expanding (currently collapsed), collapse all others first
            if (!isCurrentlyExpanded)
            {
                CollapseAllNavExcept(featureType);
            }

            // Toggle the appropriate expansion property
            switch (featureType)
            {
                case FeatureType.DesktopBackground:
                    IsDesktopBackgroundExpanded = !IsDesktopBackgroundExpanded;
                    if (IsDesktopBackgroundExpanded) EnsureDefaultScheme(featureType);
                    break;
                case FeatureType.MouseClick:
                    IsMouseClickExpanded = !IsMouseClickExpanded;
                    if (IsMouseClickExpanded) EnsureDefaultScheme(featureType);
                    break;
                case FeatureType.Shutdown:
                    IsShutdownExpanded = !IsShutdownExpanded;
                    if (IsShutdownExpanded) EnsureDefaultScheme(featureType);
                    break;
                case FeatureType.BootRestart:
                    IsBootRestartExpanded = !IsBootRestartExpanded;
                    if (IsBootRestartExpanded) EnsureDefaultScheme(featureType);
                    break;
                case FeatureType.ScreenWake:
                    IsScreenWakeExpanded = !IsScreenWakeExpanded;
                    if (IsScreenWakeExpanded) EnsureDefaultScheme(featureType);
                    break;
            }
        }

        /// <summary>
        /// Creates a new scheme for the specified feature with auto-generated name.
        /// </summary>
        /// <param name="featureType">The feature type to create a scheme for.</param>
        [RelayCommand]
        private void CreateNewScheme(FeatureType featureType)
        {
            if (!_schemesByFeature.ContainsKey(featureType))
                return;

            var schemes = _schemesByFeature[featureType];
            var featureName = GetFeatureDisplayName(featureType);
            var schemeNumber = schemes.Count + 1;
            var schemeName = $"{featureName} {schemeNumber}";

            var newScheme = new SchemeModel(schemeName, featureType);
            schemes.Add(newScheme);

            // Auto-select the new scheme
            SelectScheme(newScheme);
        }

        /// <summary>
        /// Selects and activates a scheme for its feature.
        /// Only one scheme can be active per feature.
        /// </summary>
        /// <param name="scheme">The scheme to select.</param>
        [RelayCommand]
        private void SelectScheme(SchemeModel? scheme)
        {
            if (scheme == null)
                return;

            var featureType = scheme.FeatureType;

            // Deactivate all schemes for this feature
            if (_schemesByFeature.ContainsKey(featureType))
            {
                foreach (var existingScheme in _schemesByFeature[featureType])
                {
                    existingScheme.IsActive = false;
                    existingScheme.IsSelected = false;
                }
            }

            // Activate and select the selected scheme
            scheme.IsActive = true;
            scheme.IsSelected = true;

            // Set the parent feature as selected (for header highlighting)
            SelectedFeature = featureType.ToString();

            // Update the selected scheme property and load content
            switch (featureType)
            {
                case FeatureType.DesktopBackground:
                    SelectedDesktopBackgroundScheme = scheme;
                    LoadFeatureContent("DesktopBackground");
                    break;
                case FeatureType.MouseClick:
                    SelectedMouseClickScheme = scheme;
                    LoadFeatureContent("MouseClick");
                    break;
                case FeatureType.Shutdown:
                    SelectedShutdownScheme = scheme;
                    LoadFeatureContent("Shutdown");
                    break;
                case FeatureType.BootRestart:
                    SelectedBootRestartScheme = scheme;
                    LoadFeatureContent("BootRestart");
                    break;
                case FeatureType.ScreenWake:
                    SelectedScreenWakeScheme = scheme;
                    LoadFeatureContent("ScreenWake");
                    break;
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Ensures a default scheme exists for the specified feature.
        /// Creates one if the feature has no schemes.
        /// </summary>
        /// <param name="featureType">The feature type to check.</param>
        private void EnsureDefaultScheme(FeatureType featureType)
        {
            if (!_schemesByFeature.ContainsKey(featureType))
                return;

            var schemes = _schemesByFeature[featureType];
            if (schemes.Count == 0)
            {
                var featureName = GetFeatureDisplayName(featureType);
                var defaultScheme = new SchemeModel($"{featureName} 1", featureType)
                {
                    IsActive = true,
                    IsSelected = true
                };
                schemes.Add(defaultScheme);

                // Update selected scheme property
                switch (featureType)
                {
                    case FeatureType.DesktopBackground:
                        SelectedDesktopBackgroundScheme = defaultScheme;
                        break;
                    case FeatureType.MouseClick:
                        SelectedMouseClickScheme = defaultScheme;
                        break;
                    case FeatureType.Shutdown:
                        SelectedShutdownScheme = defaultScheme;
                        break;
                    case FeatureType.BootRestart:
                        SelectedBootRestartScheme = defaultScheme;
                        break;
                    case FeatureType.ScreenWake:
                        SelectedScreenWakeScheme = defaultScheme;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the display name for a feature type.
        /// </summary>
        /// <param name="featureType">The feature type.</param>
        /// <returns>The display name of the feature.</returns>
        private static string GetFeatureDisplayName(FeatureType featureType)
        {
            return featureType switch
            {
                FeatureType.DesktopBackground => "Desktop Background",
                FeatureType.MouseClick => "Mouse Click",
                FeatureType.Shutdown => "Shutdown",
                FeatureType.BootRestart => "Boot/Restart",
                FeatureType.ScreenWake => "Screen Wake",
                FeatureType.OpenApp => "Open App",
                FeatureType.DesktopClock => "Desktop Clock",
                FeatureType.Pomodoro => "Pomodoro",
                FeatureType.Anniversary => "Anniversary",
                _ => featureType.ToString()
            };
        }

        /// <summary>
        /// Loads the preview and configuration content for a feature.
        /// </summary>
        /// <param name="featureName">The name of the feature.</param>
        private void LoadFeatureContent(string featureName)
        {
            // Reset content first
            PreviewContent = null;
            ConfigurationContent = null;
            HasPreviewContent = false;

            switch (featureName)
            {
                case "ThemePreview":
                    // Theme Preview shows the split layout with preview area
                    HasPreviewContent = false; // No actual preview content yet
                    ConfigurationContent = null;
                    break;

                case "DesktopBackground":
                    // Create and configure DesktopBackgroundViewModel
                    var desktopBgVm = _desktopBackgroundVmFactory();
                    if (SelectedDesktopBackgroundScheme != null)
                    {
                        desktopBgVm.SchemeName = SelectedDesktopBackgroundScheme.Name;
                    }
                    ConfigurationContent = desktopBgVm;
                    HasPreviewContent = false;
                    break;

                case "MouseClick":
                    // Create and configure MouseClickViewModel
                    var mouseClickVm = _mouseClickVmFactory();
                    if (SelectedMouseClickScheme != null)
                    {
                        mouseClickVm.SchemeName = SelectedMouseClickScheme.Name;
                    }
                    ConfigurationContent = mouseClickVm;
                    HasPreviewContent = false;
                    break;

                case "DesktopClock":
                    // Create and configure DesktopClockViewModel
                    ConfigurationContent = null; // Force refresh
                    var clockVm = _desktopClockVmFactory();
                    System.Diagnostics.Debug.WriteLine($"DesktopClockViewModel created: {clockVm != null}");
                    ConfigurationContent = clockVm;
                    HasPreviewContent = false;
                    break;

                case "Pomodoro":
                    // Create and configure PomodoroViewModel
                    ConfigurationContent = null; // Force refresh
                    var pomodoroVm = _pomodoroVmFactory();
                    System.Diagnostics.Debug.WriteLine($"PomodoroViewModel created: {pomodoroVm != null}");
                    ConfigurationContent = pomodoroVm;
                    HasPreviewContent = false;
                    break;

                case "Anniversary":
                    // Create and configure AnniversaryViewModel
                    ConfigurationContent = null; // Force refresh
                    var anniversaryVm = _anniversaryVmFactory();
                    System.Diagnostics.Debug.WriteLine($"AnniversaryViewModel created: {anniversaryVm != null}");
                    ConfigurationContent = anniversaryVm;
                    HasPreviewContent = false;
                    break;

                case "Shutdown":
                    // Create and configure ShutdownViewModel
                    var shutdownVm = _shutdownVmFactory();
                    if (SelectedShutdownScheme != null)
                    {
                        shutdownVm.SchemeName = SelectedShutdownScheme.Name;
                    }
                    ConfigurationContent = shutdownVm;
                    HasPreviewContent = false;
                    break;

                case "BootRestart":
                    // Create and configure BootRestartViewModel
                    var bootRestartVm = _bootRestartVmFactory();
                    if (SelectedBootRestartScheme != null)
                    {
                        bootRestartVm.SchemeName = SelectedBootRestartScheme.Name;
                    }
                    ConfigurationContent = bootRestartVm;
                    HasPreviewContent = false;
                    break;

                case "ScreenWake":
                    // Create and configure ScreenWakeViewModel
                    var screenWakeVm = _screenWakeVmFactory();
                    if (SelectedScreenWakeScheme != null)
                    {
                        screenWakeVm.SchemeName = SelectedScreenWakeScheme.Name;
                    }
                    ConfigurationContent = screenWakeVm;
                    HasPreviewContent = false;
                    break;

                case "OpenApp":
                    // Open App feature - full width configuration
                    ConfigurationContent = null; // TODO: Create OpenAppViewModel
                    HasPreviewContent = false;
                    break;

                default:
                    // For other features, clear content for now
                    ConfigurationContent = null;
                    HasPreviewContent = false;
                    break;
            }
        }
    }
}
