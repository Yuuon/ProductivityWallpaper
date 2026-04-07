using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using ProductivityWallpaper.Views;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Creator view, managing theme creation, scheme configuration,
    /// dirty tracking, save/export operations, and auto-save backup.
    /// </summary>
    public partial class CreatorViewModel : ObservableObject, IDisposable
    {
        // --- DI Services ---
        private readonly IFeatureViewModelFactory _featureVmFactory;
        private readonly IThemeService _themeService;

        // Cache of ViewModels per scheme to preserve state when switching pages
        private readonly Dictionary<string, ObservableObject> _schemeViewModelCache = new();

        // --- Auto-Save Timer ---
        private System.Timers.Timer? _autoSaveTimer;
        private const int AutoSaveIntervalMinutes = 5;

        // --- Dirty State Tracking ---

        /// <summary>
        /// Whether the current theme has unsaved changes.
        /// </summary>
        [ObservableProperty]
        private bool _isDirty;

        /// <summary>
        /// Whether a save operation is currently in progress.
        /// </summary>
        [ObservableProperty]
        private bool _isSaving;

        /// <summary>
        /// Whether an export operation is currently in progress.
        /// </summary>
        [ObservableProperty]
        private bool _isExporting;

        /// <summary>
        /// The current theme manifest being edited.
        /// </summary>
        [ObservableProperty]
        private ThemeManifest? _currentTheme;

        /// <summary>
        /// Status message for save/export operations.
        /// </summary>
        [ObservableProperty]
        private string _saveStatusMessage = string.Empty;

        /// <summary>
        /// Name of the loaded theme for save operations.
        /// </summary>
        private string? _loadedThemeName;

        partial void OnIsDirtyChanged(bool value)
        {
            SaveStatusMessage = value ? "Unsaved changes" : "All changes saved";
            SaveThemeCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsSavingChanged(bool value)
        {
            SaveThemeCommand.NotifyCanExecuteChanged();
        }

        partial void OnCurrentThemeChanged(ThemeManifest? value)
        {
            SaveThemeCommand.NotifyCanExecuteChanged();
            ExportThemeCommand.NotifyCanExecuteChanged();
        }

        partial void OnCurrentThemeNameChanged(string value)
        {
            if (CurrentTheme != null && !string.IsNullOrWhiteSpace(value) && CurrentTheme.Name != value)
            {
                CurrentTheme.Name = value;
            }
            MarkDirty();
        }

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
        /// Central handler for feature expansion. Collapses others and ensures default scheme exists.
        /// Does NOT load content — content is only loaded when a scheme is explicitly selected.
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
        public CreatorViewModel() : this(null, null)
        {
        }

        public CreatorViewModel(
            IFeatureViewModelFactory? featureVmFactory = null,
            IThemeService? themeService = null)
        {
            _featureVmFactory = featureVmFactory ?? CreateDefaultFactory();
            _themeService = themeService ?? new ThemeService();

            _schemesByFeature = new Dictionary<FeatureType, ObservableCollection<SchemeModel>>();
            foreach (var featureType in MultiSchemeFeatures)
            {
                _schemesByFeature[featureType] = new ObservableCollection<SchemeModel>();
            }

            // Initialize auto-save timer (5-minute interval, backup only) — not started until theme is loaded
            InitializeAutoSaveTimer();
        }

        /// <summary>
        /// Creates a default factory for design-time or fallback usage.
        /// </summary>
        private static FeatureViewModelFactory CreateDefaultFactory()
        {
            return new FeatureViewModelFactory(new Dictionary<FeatureType, Func<ObservableObject>>
            {
                [FeatureType.DesktopBackground] = () => new DesktopBackgroundViewModel(),
                [FeatureType.MouseClick] = () => new MouseClickViewModel(),
                [FeatureType.DesktopClock] = () => new DesktopClockViewModel(),
                [FeatureType.Pomodoro] = () => new PomodoroViewModel(),
                [FeatureType.Anniversary] = () => new AnniversaryViewModel(),
                [FeatureType.Shutdown] = () => new ShutdownViewModel(),
                [FeatureType.BootRestart] = () => new BootRestartViewModel(),
                [FeatureType.ScreenWake] = () => new ScreenWakeViewModel(),
            });
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

            // Initialize new theme
            InitializeNewTheme(CurrentThemeName);
            SelectFeature("ThemePreview");
        }

        [RelayCommand]
        private async Task BackToWelcome()
        {
            if (IsDirty)
            {
                var dialog = new SaveChangesDialog();
                if (dialog.ShowDialog() == true)
                {
                    switch (dialog.Result)
                    {
                        case SaveChangesResult.Save:
                            await SaveThemeAsync();
                            PerformBackNavigation();
                            break;
                        case SaveChangesResult.DontSave:
                            PerformBackNavigation();
                            break;
                        case SaveChangesResult.Cancel:
                            return;
                    }
                }
                else
                {
                    // Dialog was closed (cancelled)
                    return;
                }
            }
            else
            {
                PerformBackNavigation();
            }
        }

        /// <summary>
        /// Performs the actual back navigation, cleaning up state.
        /// </summary>
        private void PerformBackNavigation()
        {
            IsWelcomePage = true;
            IsCreatingPage = false;
            NewThemeName = string.Empty;
            CurrentTheme = null;
            _loadedThemeName = null;
            _schemeViewModelCache.Clear();
            IsDirty = false;

            // Stop auto-save timer while not editing
            _autoSaveTimer?.Stop();
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
            MarkDirty();
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

            // ThemePreview and OpenApp have no feature VM
            if (featureName == "ThemePreview")
            {
                NavigationMonitorService.LogNavigation("ThemePreview", null);
                return;
            }
            if (featureName == "OpenApp")
            {
                NavigationMonitorService.LogNavigation("OpenApp", null);
                return;
            }

            // Parse the feature name to a FeatureType enum
            if (!Enum.TryParse<FeatureType>(featureName, out var featureType))
            {
                Debug.WriteLine($"[Navigation] ERROR: Unknown feature name: {featureName}");
                return;
            }

            // Determine cache key: multi-scheme features use scheme ID, single-scheme use feature name
            var cacheKey = GetCacheKey(featureType);

            try
            {
                // Check cache first
                if (!_schemeViewModelCache.TryGetValue(cacheKey, out var cachedVm))
                {
                    // Create new VM via factory
                    cachedVm = _featureVmFactory.Create(featureType);

                    // For multi-scheme features, sync scheme name from selected scheme
                    var selectedScheme = GetSelectedScheme(featureType);
                    if (selectedScheme != null && cachedVm is IFeatureViewModel featureVm)
                    {
                        featureVm.SchemeName = selectedScheme.Name;
                    }

                    // Subscribe to property changes for dirty tracking
                    cachedVm.PropertyChanged += OnChildViewModelPropertyChanged;

                    // For media VMs, subscribe to collection changes
                    if (cachedVm is MediaConfigurationViewModel mediaVm)
                    {
                        mediaVm.ImageVideoItems.CollectionChanged += (_, _) => MarkDirty();
                        mediaVm.AudioItems.CollectionChanged += (_, _) => MarkDirty();
                    }

                    // For mouse click VMs, subscribe to region collection changes
                    if (cachedVm is MouseClickViewModel mouseVm)
                    {
                        mouseVm.Regions.CollectionChanged += (_, _) => MarkDirty();
                    }

                    _schemeViewModelCache[cacheKey] = cachedVm;
                }

                ConfigurationContent = cachedVm;
                NavigationMonitorService.LogNavigation(featureName, cachedVm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to create ViewModel for {featureName}: {ex.Message}");
                NavigationMonitorService.LogNavigation(featureName, null, ex);
            }
        }

        /// <summary>
        /// Determines the cache key for a feature. Multi-scheme features are keyed by scheme ID;
        /// single-scheme features use the feature type name as a stable key.
        /// </summary>
        private string GetCacheKey(FeatureType featureType)
        {
            var selectedScheme = GetSelectedScheme(featureType);
            if (selectedScheme != null)
                return selectedScheme.Id;

            // Single-scheme features or no scheme selected — use feature name as key
            return featureType.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Gets the currently selected scheme for a given feature type, or null for single-scheme features.
        /// </summary>
        private SchemeModel? GetSelectedScheme(FeatureType featureType)
        {
            return featureType switch
            {
                FeatureType.DesktopBackground => SelectedDesktopBackgroundScheme,
                FeatureType.MouseClick => SelectedMouseClickScheme,
                FeatureType.Shutdown => SelectedShutdownScheme,
                FeatureType.BootRestart => SelectedBootRestartScheme,
                FeatureType.ScreenWake => SelectedScreenWakeScheme,
                _ => null
            };
        }

        // ==================== Save / Export / Auto-Save ====================

        /// <summary>
        /// Saves the current theme to disk. Resets IsDirty on success.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveTheme))]
        private async Task SaveThemeAsync()
        {
            if (CurrentTheme == null || string.IsNullOrEmpty(_loadedThemeName))
                return;

            IsSaving = true;
            SaveStatusMessage = "Saving...";

            try
            {
                SyncToTheme();
                await _themeService.SaveThemeAsync(CurrentTheme, isBackup: false);
                IsDirty = false;
                SaveStatusMessage = "Saved";
                Debug.WriteLine($"[CreatorViewModel] Theme saved: {_loadedThemeName}");
            }
            catch (Exception ex)
            {
                SaveStatusMessage = $"Save failed: {ex.Message}";
                Debug.WriteLine($"[CreatorViewModel] Save failed: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private bool CanSaveTheme() => IsDirty && !IsSaving && CurrentTheme != null;

        /// <summary>
        /// Exports the current theme as a complete portable package.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExportTheme))]
        private async Task ExportThemeAsync()
        {
            if (CurrentTheme == null)
                return;

            // Use WinForms FolderBrowserDialog for folder selection
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select export location for theme package",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            IsExporting = true;
            SaveStatusMessage = "Exporting...";

            try
            {
                SyncToTheme();
                var progress = new Progress<string>(msg =>
                    SaveStatusMessage = $"Exporting: {msg}");

                var exportPath = await _themeService.ExportThemeAsync(
                    CurrentTheme, dialog.SelectedPath, progress);

                SaveStatusMessage = $"Exported to: {exportPath}";
                Debug.WriteLine($"[CreatorViewModel] Theme exported to: {exportPath}");
            }
            catch (Exception ex)
            {
                SaveStatusMessage = $"Export failed: {ex.Message}";
                Debug.WriteLine($"[CreatorViewModel] Export failed: {ex.Message}");
            }
            finally
            {
                IsExporting = false;
            }
        }

        private bool CanExportTheme() => !IsExporting && CurrentTheme != null;

        // ==================== Auto-Save Timer ====================

        /// <summary>
        /// Initializes the 5-minute auto-save timer for backup saves.
        /// </summary>
        private void InitializeAutoSaveTimer()
        {
            _autoSaveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(AutoSaveIntervalMinutes).TotalMilliseconds);
            _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
            _autoSaveTimer.AutoReset = true;
        }

        private async void OnAutoSaveTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!IsDirty || CurrentTheme == null || IsSaving || _loadedThemeName == null)
                return;

            try
            {
                SyncToTheme();
                // Silent backup save — does NOT reset IsDirty
                await _themeService.SaveThemeAsync(CurrentTheme, isBackup: true);
                Debug.WriteLine("[AutoSave] Backup saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutoSave] Failed: {ex.Message}");
            }
        }

        // ==================== Theme Initialization ====================

        /// <summary>
        /// Initializes a new theme for editing.
        /// </summary>
        private void InitializeNewTheme(string themeName)
        {
            CurrentTheme = new ThemeManifest { Name = themeName };
            _loadedThemeName = themeName;
            _themeService.CurrentTheme = CurrentTheme;
            IsDirty = false;

            // Start auto-save timer
            _autoSaveTimer?.Start();
        }

        /// <summary>
        /// Loads an existing saved theme into the creator for editing.
        /// Populates scheme collections and caches from the theme manifest.
        /// </summary>
        public async Task LoadExistingThemeAsync(string themeName)
        {
            var theme = await _themeService.LoadThemeAsync(themeName);
            if (theme == null)
            {
                Debug.WriteLine($"[CreatorViewModel] Failed to load theme: {themeName}");
                return;
            }

            // Clear existing state
            _schemeViewModelCache.Clear();
            foreach (var featureType in MultiSchemeFeatures)
            {
                _schemesByFeature[featureType].Clear();
            }

            // Set the current theme
            CurrentTheme = theme;
            _loadedThemeName = themeName;
            _themeService.CurrentTheme = theme;
            CurrentThemeName = theme.Name;

            // Restore scheme collections from the loaded theme
            RestoreSchemes(FeatureType.DesktopBackground, theme.DesktopBackgroundSchemes);
            RestoreSchemes(FeatureType.MouseClick, theme.MouseClickSchemes);
            RestoreSchemes(FeatureType.Shutdown, theme.ShutdownSchemes);
            RestoreSchemes(FeatureType.BootRestart, theme.BootRestartSchemes);
            RestoreSchemes(FeatureType.ScreenWake, theme.ScreenWakeSchemes);

            // Navigate to creating page
            IsWelcomePage = false;
            IsCreatingPage = true;
            IsDirty = false;
            SelectFeature("ThemePreview");

            // Start auto-save timer
            _autoSaveTimer?.Start();

            Debug.WriteLine($"[CreatorViewModel] Loaded existing theme: {themeName}");
        }

        /// <summary>
        /// Restores scheme collection from loaded theme data. For each scheme,
        /// pre-populates the ViewModel cache with media items from ResourceLibrary.
        /// </summary>
        private void RestoreSchemes(FeatureType featureType, ObservableCollection<SchemeModel> schemes)
        {
            if (!_schemesByFeature.ContainsKey(featureType)) return;

            foreach (var scheme in schemes)
            {
                scheme.FeatureType = featureType;
                _schemesByFeature[featureType].Add(scheme);

                // Pre-populate ViewModel cache from scheme data
                try
                {
                    var vm = _featureVmFactory.Create(featureType);

                    if (vm is IFeatureViewModel featureVm)
                    {
                        featureVm.SchemeName = scheme.Name;
                    }

                    // Restore media items from ResourceLibrary references
                    if (vm is MediaConfigurationViewModel mediaVm && CurrentTheme != null)
                    {
                        foreach (var resourceId in scheme.DesktopBackgroundMedia.MediaIds)
                        {
                            var mediaItem = ResolveMediaItem(resourceId);
                            if (mediaItem != null)
                                mediaVm.ImageVideoItems.Add(mediaItem);
                        }
                        mediaVm.SelectedPlaybackMode = scheme.DesktopBackgroundMedia.PlaybackMode;

                        foreach (var resourceId in scheme.EventMedia.MediaIds)
                        {
                            var mediaItem = ResolveMediaItem(resourceId);
                            if (mediaItem != null)
                                mediaVm.AudioItems.Add(mediaItem);
                        }
                        mediaVm.SelectedAudioPlaybackMode = scheme.EventMedia.PlaybackMode;
                    }

                    // Restore mouse click regions
                    if (vm is MouseClickViewModel mouseVm)
                    {
                        foreach (var region in scheme.ClickRegions)
                        {
                            mouseVm.Regions.Add(region);
                        }
                    }

                    // Subscribe to changes
                    vm.PropertyChanged += OnChildViewModelPropertyChanged;
                    if (vm is MediaConfigurationViewModel mvm)
                    {
                        mvm.ImageVideoItems.CollectionChanged += (_, _) => MarkDirty();
                        mvm.AudioItems.CollectionChanged += (_, _) => MarkDirty();
                    }
                    if (vm is MouseClickViewModel mcvm)
                    {
                        mcvm.Regions.CollectionChanged += (_, _) => MarkDirty();
                    }

                    _schemeViewModelCache[scheme.Id] = vm;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CreatorViewModel] Failed to restore scheme {scheme.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resolves a resource ID to a MediaItemModel by looking up the ResourceLibrary.
        /// </summary>
        private MediaItemModel? ResolveMediaItem(string resourceId)
        {
            if (CurrentTheme == null) return null;

            var resource = CurrentTheme.ResourceLibrary.GetById(resourceId);
            if (resource == null)
            {
                Debug.WriteLine($"[CreatorViewModel] Resource not found: {resourceId}");
                return null;
            }

            var item = new MediaItemModel(resource.SourcePath)
            {
                Type = resource.Type switch
                {
                    MediaType.Image => MediaFileType.Image,
                    MediaType.Video => MediaFileType.Video,
                    MediaType.Audio => MediaFileType.Audio,
                    _ => MediaFileType.Image
                },
                FileSize = resource.FileSize,
                Duration = resource.Duration,
                DisplayMode = DisplayMode.Fill
            };

            // Set thumbnail for images
            if (resource.Type == MediaType.Image && !string.IsNullOrEmpty(resource.SourcePath))
            {
                item.ThumbnailPath = resource.SourcePath;
            }

            return item;
        }

        // ==================== Sync to Theme ====================

        /// <summary>
        /// Synchronizes all in-memory scheme data and cached ViewModel data
        /// back into CurrentTheme so it can be correctly serialized.
        /// Registers media files as ResourceEntry objects in ResourceLibrary and
        /// stores ResourceEntry IDs (not raw file paths) in scheme MediaReferenceList.
        /// </summary>
        private void SyncToTheme()
        {
            if (CurrentTheme == null) return;

            // 1. Sync scheme collections from _schemesByFeature to CurrentTheme
            CurrentTheme.DesktopBackgroundSchemes = new ObservableCollection<SchemeModel>(
                _schemesByFeature[FeatureType.DesktopBackground]);
            CurrentTheme.MouseClickSchemes = new ObservableCollection<SchemeModel>(
                _schemesByFeature[FeatureType.MouseClick]);
            CurrentTheme.ShutdownSchemes = new ObservableCollection<SchemeModel>(
                _schemesByFeature[FeatureType.Shutdown]);
            CurrentTheme.BootRestartSchemes = new ObservableCollection<SchemeModel>(
                _schemesByFeature[FeatureType.BootRestart]);
            CurrentTheme.ScreenWakeSchemes = new ObservableCollection<SchemeModel>(
                _schemesByFeature[FeatureType.ScreenWake]);

            // 2. Sync cached ViewModel data back to their respective schemes
            foreach (var kvp in _schemeViewModelCache)
            {
                var cacheKey = kvp.Key;
                var vm = kvp.Value;

                // Find the corresponding scheme by ID
                SchemeModel? scheme = null;
                foreach (var featureSchemes in _schemesByFeature.Values)
                {
                    foreach (var s in featureSchemes)
                    {
                        if (s.Id == cacheKey)
                        {
                            scheme = s;
                            break;
                        }
                    }
                    if (scheme != null) break;
                }

                if (scheme == null) continue;

                // Sync MediaConfigurationViewModel data to SchemeModel
                if (vm is MediaConfigurationViewModel mediaVm)
                {
                    scheme.DesktopBackgroundMedia.MediaIds.Clear();
                    foreach (var item in mediaVm.ImageVideoItems)
                    {
                        var resourceId = RegisterOrFindResource(item);
                        scheme.DesktopBackgroundMedia.MediaIds.Add(resourceId);
                    }
                    scheme.DesktopBackgroundMedia.PlaybackMode = mediaVm.SelectedPlaybackMode;

                    scheme.EventMedia.MediaIds.Clear();
                    foreach (var item in mediaVm.AudioItems)
                    {
                        var resourceId = RegisterOrFindResource(item);
                        scheme.EventMedia.MediaIds.Add(resourceId);
                    }
                    scheme.EventMedia.PlaybackMode = mediaVm.SelectedAudioPlaybackMode;

                    scheme.Name = mediaVm.SchemeName;
                }

                // Sync MouseClickViewModel data to SchemeModel
                if (vm is MouseClickViewModel mouseVm)
                {
                    scheme.ClickRegions = new ObservableCollection<ClickRegionModel>(mouseVm.Regions);
                    scheme.Name = mouseVm.SchemeName;
                }
            }

            Debug.WriteLine($"[CreatorViewModel] Theme data synced. ResourceLibrary: {CurrentTheme.ResourceLibrary.Count} entries");
        }

        /// <summary>
        /// Registers a media item as a ResourceEntry in the theme ResourceLibrary.
        /// If a resource with the same SourcePath already exists, returns its ID.
        /// </summary>
        private string RegisterOrFindResource(MediaItemModel item)
        {
            if (CurrentTheme == null) return item.FilePath;

            // Check if resource already exists by SourcePath
            foreach (var existing in CurrentTheme.ResourceLibrary.GetAll())
            {
                if (existing.SourcePath == item.FilePath)
                    return existing.Id;
            }

            // Create new ResourceEntry
            var entry = new ResourceEntry
            {
                SourcePath = item.FilePath,
                OriginalName = item.FileName,
                FileName = item.FileName,
                Format = item.Format,
                FileSize = item.FileSize,
                Duration = item.Duration,
                Type = item.Type switch
                {
                    MediaFileType.Image => MediaType.Image,
                    MediaFileType.Video => MediaType.Video,
                    MediaFileType.Audio => MediaType.Audio,
                    _ => MediaType.Image
                }
            };

            CurrentTheme.ResourceLibrary.Add(entry);
            return entry.Id;
        }

        // ==================== Dirty Tracking ====================

        /// <summary>
        /// Marks the current theme as having unsaved changes.
        /// </summary>
        private void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

        /// <summary>
        /// Handles property changes on child ViewModels to propagate dirty state.
        /// </summary>
        private void OnChildViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Track meaningful property changes that indicate user edits
            if (e.PropertyName is "SchemeName" or "SelectedPlaybackMode" or "SelectedAudioPlaybackMode"
                or "IsActive" or "BackgroundMedia" or "SelectedRegion")
            {
                MarkDirty();
            }
        }

        /// <summary>
        /// Cleans up timer resources.
        /// </summary>
        public void Dispose()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();

            // Unsubscribe from child VM events
            foreach (var vm in _schemeViewModelCache.Values)
            {
                vm.PropertyChanged -= OnChildViewModelPropertyChanged;
            }

            _schemeViewModelCache.Clear();
        }
    }
}
