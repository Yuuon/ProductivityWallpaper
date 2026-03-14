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

        [ObservableProperty]
        private bool _isMouseClickExpanded;

        [ObservableProperty]
        private bool _isShutdownExpanded;

        [ObservableProperty]
        private bool _isBootRestartExpanded;

        [ObservableProperty]
        private bool _isScreenWakeExpanded;

        // --- Selected Schemes for Each Feature ---
        [ObservableProperty]
        private SchemeModel? _selectedDesktopBackgroundScheme;

        [ObservableProperty]
        private SchemeModel? _selectedMouseClickScheme;

        [ObservableProperty]
        private SchemeModel? _selectedShutdownScheme;

        [ObservableProperty]
        private SchemeModel? _selectedBootRestartScheme;

        [ObservableProperty]
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
        [ObservableProperty]
        private bool _isThemePreviewSelected = true;

        [ObservableProperty]
        private bool _isDesktopBackgroundSelected;

        [ObservableProperty]
        private bool _isMouseClickSelected;

        [ObservableProperty]
        private bool _isShutdownSelected;

        [ObservableProperty]
        private bool _isBootRestartSelected;

        [ObservableProperty]
        private bool _isScreenWakeSelected;

        [ObservableProperty]
        private bool _isOpenAppSelected;

        [ObservableProperty]
        private bool _isDesktopClockSelected;

        [ObservableProperty]
        private bool _isPomodoroSelected;

        [ObservableProperty]
        private bool _isAnniversarySelected;

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
            // Reset all selections
            IsThemePreviewSelected = false;
            IsDesktopBackgroundSelected = false;
            IsMouseClickSelected = false;
            IsShutdownSelected = false;
            IsBootRestartSelected = false;
            IsScreenWakeSelected = false;
            IsOpenAppSelected = false;
            IsDesktopClockSelected = false;
            IsPomodoroSelected = false;
            IsAnniversarySelected = false;

            // Set selected feature
            switch (featureName)
            {
                case "ThemePreview":
                    IsThemePreviewSelected = true;
                    // Collapse all expanded menus when selecting Theme Preview
                    CollapseAllNavExcept();
                    break;
                case "DesktopBackground":
                    IsDesktopBackgroundSelected = true;
                    break;
                case "MouseClick":
                    IsMouseClickSelected = true;
                    break;
                case "Shutdown":
                    IsShutdownSelected = true;
                    break;
                case "BootRestart":
                    IsBootRestartSelected = true;
                    break;
                case "ScreenWake":
                    IsScreenWakeSelected = true;
                    break;
                case "OpenApp":
                    IsOpenAppSelected = true;
                    // Collapse all expanded menus when selecting simple feature
                    CollapseAllNavExcept();
                    break;
                case "DesktopClock":
                    IsDesktopClockSelected = true;
                    // Collapse all expanded menus when selecting simple feature
                    CollapseAllNavExcept();
                    break;
                case "Pomodoro":
                    IsPomodoroSelected = true;
                    // Collapse all expanded menus when selecting simple feature
                    CollapseAllNavExcept();
                    break;
                case "Anniversary":
                    IsAnniversarySelected = true;
                    // Collapse all expanded menus when selecting simple feature
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
                }
            }

            // Activate the selected scheme
            scheme.IsActive = true;

            // Update the selected scheme property
            switch (featureType)
            {
                case FeatureType.DesktopBackground:
                    SelectedDesktopBackgroundScheme = scheme;
                    break;
                case FeatureType.MouseClick:
                    SelectedMouseClickScheme = scheme;
                    break;
                case FeatureType.Shutdown:
                    SelectedShutdownScheme = scheme;
                    break;
                case FeatureType.BootRestart:
                    SelectedBootRestartScheme = scheme;
                    break;
                case FeatureType.ScreenWake:
                    SelectedScreenWakeScheme = scheme;
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
                    IsActive = true
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
            switch (featureName)
            {
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

                case "DesktopClock":
                    // Create and configure DesktopClockViewModel
                    var clockVm = _desktopClockVmFactory();
                    ConfigurationContent = clockVm;
                    HasPreviewContent = false;
                    break;

                case "Pomodoro":
                    // Create and configure PomodoroViewModel
                    var pomodoroVm = _pomodoroVmFactory();
                    ConfigurationContent = pomodoroVm;
                    HasPreviewContent = false;
                    break;

                case "Anniversary":
                    // Create and configure AnniversaryViewModel
                    var anniversaryVm = _anniversaryVmFactory();
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

                case "ThemePreview":
                    ConfigurationContent = null;
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
