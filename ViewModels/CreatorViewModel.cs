using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;

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
        private readonly Dictionary<FeatureType, ObservableCollection<SchemeModel>> _schemesByFeature;

        // --- Expansion States for Multi-Scheme Features ---
        // Guard flag to prevent recursive updates during batch collapse
        private bool _isUpdatingExpansion;

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

        partial void OnIsDesktopBackgroundExpandedChanged(bool value)
        {
            if (_isUpdatingExpansion) return;
            if (value) HandleFeatureExpanded(FeatureType.DesktopBackground);
        }

        partial void OnIsMouseClickExpandedChanged(bool value)
        {
            if (_isUpdatingExpansion) return;
            if (value) HandleFeatureExpanded(FeatureType.MouseClick);
        }

        partial void OnIsShutdownExpandedChanged(bool value)
        {
            if (_isUpdatingExpansion) return;
            if (value) HandleFeatureExpanded(FeatureType.Shutdown);
        }

        partial void OnIsBootRestartExpandedChanged(bool value)
        {
            if (_isUpdatingExpansion) return;
            if (value) HandleFeatureExpanded(FeatureType.BootRestart);
        }

        partial void OnIsScreenWakeExpandedChanged(bool value)
        {
            if (_isUpdatingExpansion) return;
            if (value) HandleFeatureExpanded(FeatureType.ScreenWake);
        }

        /// <summary>
        /// Central handler for feature expansion. Collapses others, sets state, loads content.
        /// </summary>
        private void HandleFeatureExpanded(FeatureType expandedFeature)
        {
            _isUpdatingExpansion = true;
            try
            {
                // Single-expand: collapse all others
                if (expandedFeature != FeatureType.DesktopBackground) IsDesktopBackgroundExpanded = false;
                if (expandedFeature != FeatureType.MouseClick) IsMouseClickExpanded = false;
                if (expandedFeature != FeatureType.Shutdown) IsShutdownExpanded = false;
                if (expandedFeature != FeatureType.BootRestart) IsBootRestartExpanded = false;
                if (expandedFeature != FeatureType.ScreenWake) IsScreenWakeExpanded = false;
            }
            finally
            {
                _isUpdatingExpansion = false;
            }

            // Auto-create default scheme if needed
            EnsureDefaultScheme(expandedFeature);

            // Set current state (this also clears stale selections via OnCurrentStateChanged)
            if (Enum.TryParse<CreatorViewState>(expandedFeature.ToString(), out var state))
            {
                CurrentState = state;
            }

            LoadFeatureContent(expandedFeature.ToString());
        }

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
        [NotifyPropertyChangedFor(nameof(IsThemePreviewActive))]
        [NotifyPropertyChangedFor(nameof(IsDesktopBackgroundActive))]
        [NotifyPropertyChangedFor(nameof(IsMouseClickActive))]
        [NotifyPropertyChangedFor(nameof(IsShutdownActive))]
        [NotifyPropertyChangedFor(nameof(IsBootRestartActive))]
        [NotifyPropertyChangedFor(nameof(IsScreenWakeActive))]
        [NotifyPropertyChangedFor(nameof(IsOpenAppActive))]
        [NotifyPropertyChangedFor(nameof(IsDesktopClockActive))]
        [NotifyPropertyChangedFor(nameof(IsPomodoroActive))]
        [NotifyPropertyChangedFor(nameof(IsAnniversaryActive))]
        [NotifyPropertyChangedFor(nameof(IsDesktopBackgroundHeaderHighlighted))]
        [NotifyPropertyChangedFor(nameof(IsMouseClickHeaderHighlighted))]
        [NotifyPropertyChangedFor(nameof(IsShutdownHeaderHighlighted))]
        [NotifyPropertyChangedFor(nameof(IsBootRestartHeaderHighlighted))]
        [NotifyPropertyChangedFor(nameof(IsScreenWakeHeaderHighlighted))]
        private CreatorViewState _currentState = CreatorViewState.ThemePreview;

        /// <summary>
        /// Called when CurrentState changes. Clears stale scheme selections from non-current features.
        /// This is the single source of truth for ensuring only one feature is highlighted at a time.
        /// </summary>
        partial void OnCurrentStateChanged(CreatorViewState value)
        {
            ClearStaleSchemeSelections(value);
        }

        public bool IsThemePreviewActive => CurrentState == CreatorViewState.ThemePreview;
        public bool IsDesktopBackgroundActive => CurrentState == CreatorViewState.DesktopBackground;
        public bool IsMouseClickActive => CurrentState == CreatorViewState.MouseClick;
        public bool IsShutdownActive => CurrentState == CreatorViewState.Shutdown;
        public bool IsBootRestartActive => CurrentState == CreatorViewState.BootRestart;
        public bool IsScreenWakeActive => CurrentState == CreatorViewState.ScreenWake;
        public bool IsOpenAppActive => CurrentState == CreatorViewState.OpenApp;
        public bool IsDesktopClockActive => CurrentState == CreatorViewState.DesktopClock;
        public bool IsPomodoroActive => CurrentState == CreatorViewState.Pomodoro;
        public bool IsAnniversaryActive => CurrentState == CreatorViewState.Anniversary;

        // --- Header Highlight Properties (for expandable features) ---
        // Simplified: only depends on CurrentState, so only one can be true at a time.
        public bool IsDesktopBackgroundHeaderHighlighted => CurrentState == CreatorViewState.DesktopBackground;
        public bool IsMouseClickHeaderHighlighted => CurrentState == CreatorViewState.MouseClick;
        public bool IsShutdownHeaderHighlighted => CurrentState == CreatorViewState.Shutdown;
        public bool IsBootRestartHeaderHighlighted => CurrentState == CreatorViewState.BootRestart;
        public bool IsScreenWakeHeaderHighlighted => CurrentState == CreatorViewState.ScreenWake;

        // --- Content Properties ---
        [ObservableProperty]
        private bool _hasPreviewContent;

        [ObservableProperty]
        private object? _previewContent;

        [ObservableProperty]
        private object? _configurationContent;

        public Dictionary<FeatureType, ObservableCollection<SchemeModel>> SchemesByFeature => _schemesByFeature;
        public ObservableCollection<SchemeModel> DesktopBackgroundSchemes => _schemesByFeature[FeatureType.DesktopBackground];
        public ObservableCollection<SchemeModel> MouseClickSchemes => _schemesByFeature[FeatureType.MouseClick];
        public ObservableCollection<SchemeModel> ShutdownSchemes => _schemesByFeature[FeatureType.Shutdown];
        public ObservableCollection<SchemeModel> BootRestartSchemes => _schemesByFeature[FeatureType.BootRestart];
        public ObservableCollection<SchemeModel> ScreenWakeSchemes => _schemesByFeature[FeatureType.ScreenWake];

        // --- Constructors ---
        public CreatorViewModel() : this(null, null, null, null, null, null, null, null)
        {
        }

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

            _schemesByFeature = new Dictionary<FeatureType, ObservableCollection<SchemeModel>>();
            foreach (var featureType in MultiSchemeFeatures)
            {
                _schemesByFeature[featureType] = new ObservableCollection<SchemeModel>();
            }
        }

        // --- Commands ---

        [RelayCommand]
        private void StartCreating()
        {
            CurrentThemeName = string.IsNullOrWhiteSpace(NewThemeName)
                ? "输入主题名字..."
                : NewThemeName.Trim();

            IsWelcomePage = false;
            IsCreatingPage = true;
            SelectFeature("ThemePreview");
        }

        [RelayCommand]
        private void BackToWelcome()
        {
            IsWelcomePage = true;
            IsCreatingPage = false;
            NewThemeName = string.Empty;
        }

        [RelayCommand]
        private void SelectFeature(string featureName)
        {
            Debug.WriteLine($"[Navigation] Selecting feature: {featureName}");

            try
            {
                if (!Enum.TryParse<CreatorViewState>(featureName, out var state))
                {
                    Debug.WriteLine($"[Navigation] ERROR: Unknown feature name: {featureName}");
                    return;
                }

                // For simple features, collapse all expandable menus
                switch (featureName)
                {
                    case "ThemePreview":
                    case "OpenApp":
                    case "DesktopClock":
                    case "Pomodoro":
                    case "Anniversary":
                        CollapseAllNav();
                        break;
                }

                // Setting CurrentState triggers OnCurrentStateChanged which clears stale selections
                CurrentState = state;

                LoadFeatureContent(featureName);

                Debug.WriteLine(ConfigurationContent != null
                    ? $"[Navigation] SUCCESS: {featureName} loaded, Content type: {ConfigurationContent.GetType().Name}"
                    : $"[Navigation] {featureName} loaded, ConfigurationContent is null (expected for ThemePreview/OpenApp)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Navigation] FAILED: {featureName} - {ex.Message}");
                NavigationMonitorService.LogNavigation(featureName, null, ex);
            }
        }

        [RelayCommand]
        private void ToggleEditThemeName()
        {
            IsEditingThemeName = true;
        }

        [RelayCommand]
        private void FinishEditThemeName()
        {
            IsEditingThemeName = false;
        }

        /// <summary>
        /// Collapses all expandable navigation menus.
        /// </summary>
        private void CollapseAllNav()
        {
            _isUpdatingExpansion = true;
            try
            {
                IsDesktopBackgroundExpanded = false;
                IsMouseClickExpanded = false;
                IsShutdownExpanded = false;
                IsBootRestartExpanded = false;
                IsScreenWakeExpanded = false;
            }
            finally
            {
                _isUpdatingExpansion = false;
            }
        }

        [RelayCommand]
        private void ToggleFeatureExpansion(FeatureType featureType)
        {
            bool isCurrentlyExpanded = featureType switch
            {
                FeatureType.DesktopBackground => IsDesktopBackgroundExpanded,
                FeatureType.MouseClick => IsMouseClickExpanded,
                FeatureType.Shutdown => IsShutdownExpanded,
                FeatureType.BootRestart => IsBootRestartExpanded,
                FeatureType.ScreenWake => IsScreenWakeExpanded,
                _ => false
            };

            // Toggle the expansion state (the partial OnChanged handler does the rest)
            switch (featureType)
            {
                case FeatureType.DesktopBackground:
                    IsDesktopBackgroundExpanded = !isCurrentlyExpanded;
                    break;
                case FeatureType.MouseClick:
                    IsMouseClickExpanded = !isCurrentlyExpanded;
                    break;
                case FeatureType.Shutdown:
                    IsShutdownExpanded = !isCurrentlyExpanded;
                    break;
                case FeatureType.BootRestart:
                    IsBootRestartExpanded = !isCurrentlyExpanded;
                    break;
                case FeatureType.ScreenWake:
                    IsScreenWakeExpanded = !isCurrentlyExpanded;
                    break;
            }
        }

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

            SelectScheme(newScheme);
        }

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

            // Activate and select the chosen scheme
            scheme.IsActive = true;
            scheme.IsSelected = true;

            // Set CurrentState (triggers OnCurrentStateChanged which clears other features' stale selections)
            if (Enum.TryParse<CreatorViewState>(featureType.ToString(), out var state))
            {
                CurrentState = state;
            }

            // Update the per-feature selected scheme reference and load content
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
        /// Clears IsSelected on all schemes for features that are NOT the current feature.
        /// This ensures only one feature's schemes can be selected at a time,
        /// preventing multiple expandable headers from being highlighted simultaneously.
        /// </summary>
        private void ClearStaleSchemeSelections(CreatorViewState currentState)
        {
            foreach (var kvp in _schemesByFeature)
            {
                // Skip the current feature — its selection is valid
                if (kvp.Key.ToString() == currentState.ToString())
                    continue;

                foreach (var scheme in kvp.Value)
                {
                    scheme.IsSelected = false;
                }
            }

            // Clear the selected scheme references for non-current features
            if (currentState != CreatorViewState.DesktopBackground && SelectedDesktopBackgroundScheme != null)
            {
                SelectedDesktopBackgroundScheme = null;
            }
            if (currentState != CreatorViewState.MouseClick && SelectedMouseClickScheme != null)
            {
                SelectedMouseClickScheme = null;
            }
            if (currentState != CreatorViewState.Shutdown && SelectedShutdownScheme != null)
            {
                SelectedShutdownScheme = null;
            }
            if (currentState != CreatorViewState.BootRestart && SelectedBootRestartScheme != null)
            {
                SelectedBootRestartScheme = null;
            }
            if (currentState != CreatorViewState.ScreenWake && SelectedScreenWakeScheme != null)
            {
                SelectedScreenWakeScheme = null;
            }
        }

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

        private void LoadFeatureContent(string featureName)
        {
            PreviewContent = null;
            ConfigurationContent = null;
            HasPreviewContent = false;

            switch (featureName)
            {
                case "ThemePreview":
                    HasPreviewContent = false;
                    ConfigurationContent = null;
                    NavigationMonitorService.LogNavigation("ThemePreview", null);
                    break;

                case "DesktopBackground":
                    try
                    {
                        var desktopBgVm = _desktopBackgroundVmFactory();
                        if (SelectedDesktopBackgroundScheme != null)
                            desktopBgVm.SchemeName = SelectedDesktopBackgroundScheme.Name;
                        ConfigurationContent = desktopBgVm;
                        NavigationMonitorService.LogNavigation("DesktopBackground", desktopBgVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create DesktopBackgroundViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("DesktopBackground", null, ex);
                    }
                    break;

                case "MouseClick":
                    try
                    {
                        var mouseClickVm = _mouseClickVmFactory();
                        if (SelectedMouseClickScheme != null)
                            mouseClickVm.SchemeName = SelectedMouseClickScheme.Name;
                        ConfigurationContent = mouseClickVm;
                        NavigationMonitorService.LogNavigation("MouseClick", mouseClickVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create MouseClickViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("MouseClick", null, ex);
                    }
                    break;

                case "DesktopClock":
                    try
                    {
                        var clockVm = _desktopClockVmFactory();
                        ConfigurationContent = clockVm;
                        NavigationMonitorService.LogNavigation("DesktopClock", clockVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create DesktopClockViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("DesktopClock", null, ex);
                    }
                    break;

                case "Pomodoro":
                    try
                    {
                        var pomodoroVm = _pomodoroVmFactory();
                        ConfigurationContent = pomodoroVm;
                        NavigationMonitorService.LogNavigation("Pomodoro", pomodoroVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create PomodoroViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("Pomodoro", null, ex);
                    }
                    break;

                case "Anniversary":
                    try
                    {
                        var anniversaryVm = _anniversaryVmFactory();
                        ConfigurationContent = anniversaryVm;
                        NavigationMonitorService.LogNavigation("Anniversary", anniversaryVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create AnniversaryViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("Anniversary", null, ex);
                    }
                    break;

                case "Shutdown":
                    try
                    {
                        var shutdownVm = _shutdownVmFactory();
                        if (SelectedShutdownScheme != null)
                            shutdownVm.SchemeName = SelectedShutdownScheme.Name;
                        ConfigurationContent = shutdownVm;
                        NavigationMonitorService.LogNavigation("Shutdown", shutdownVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create ShutdownViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("Shutdown", null, ex);
                    }
                    break;

                case "BootRestart":
                    try
                    {
                        var bootRestartVm = _bootRestartVmFactory();
                        if (SelectedBootRestartScheme != null)
                            bootRestartVm.SchemeName = SelectedBootRestartScheme.Name;
                        ConfigurationContent = bootRestartVm;
                        NavigationMonitorService.LogNavigation("BootRestart", bootRestartVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create BootRestartViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("BootRestart", null, ex);
                    }
                    break;

                case "ScreenWake":
                    try
                    {
                        var screenWakeVm = _screenWakeVmFactory();
                        if (SelectedScreenWakeScheme != null)
                            screenWakeVm.SchemeName = SelectedScreenWakeScheme.Name;
                        ConfigurationContent = screenWakeVm;
                        NavigationMonitorService.LogNavigation("ScreenWake", screenWakeVm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to create ScreenWakeViewModel: {ex.Message}");
                        NavigationMonitorService.LogNavigation("ScreenWake", null, ex);
                    }
                    break;

                case "OpenApp":
                    ConfigurationContent = null;
                    NavigationMonitorService.LogNavigation("OpenApp", null);
                    break;

                default:
                    ConfigurationContent = null;
                    break;
            }
        }
    }
}
