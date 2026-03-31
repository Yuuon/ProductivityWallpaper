using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Root structure for theme.json — the manifest file for a theme package.
    /// Contains metadata, resource library, scheme collections, and widget styles.
    /// Supports two-phase workflow: local editing (absolute paths) and export (relative paths).
    /// </summary>
    public partial class ThemeManifest : ObservableObject
    {
        // ==================== Metadata ====================

        /// <summary>
        /// Unique identifier for this theme.
        /// </summary>
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Display name of the theme.
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Author of the theme.
        /// </summary>
        [ObservableProperty]
        private string _author = string.Empty;

        /// <summary>
        /// Description of the theme.
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Theme version (semver format, e.g., "1.0.0").
        /// </summary>
        [ObservableProperty]
        private string _version = "1.0.0";

        /// <summary>
        /// Minimum app version required to use this theme.
        /// </summary>
        [ObservableProperty]
        private string _targetAppVersion = "1.0.0";

        /// <summary>
        /// When this theme was created.
        /// </summary>
        [ObservableProperty]
        private DateTime _createdAt = DateTime.UtcNow;

        /// <summary>
        /// When this theme was last modified.
        /// </summary>
        [ObservableProperty]
        private DateTime _updatedAt = DateTime.UtcNow;

        /// <summary>
        /// Base path for resolving relative ExportPaths. Set during export.
        /// Null during local editing mode.
        /// </summary>
        [ObservableProperty]
        private string? _exportBasePath;

        // ==================== Resources ====================

        /// <summary>
        /// Library of all media resources in this theme.
        /// </summary>
        [ObservableProperty]
        private ThemeResourceLibrary _resourceLibrary = new();

        // ==================== Schemes by Feature Type ====================

        /// <summary>
        /// Desktop background schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _desktopBackgroundSchemes = new();

        /// <summary>
        /// Mouse click interaction schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _mouseClickSchemes = new();

        /// <summary>
        /// Shutdown event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _shutdownSchemes = new();

        /// <summary>
        /// Boot/restart event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _bootRestartSchemes = new();

        /// <summary>
        /// Screen wake event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _screenWakeSchemes = new();

        // New system event schemes

        /// <summary>
        /// Session lock event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _sessionLockSchemes = new();

        /// <summary>
        /// Session unlock event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _sessionUnlockSchemes = new();

        /// <summary>
        /// Network disconnect event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _networkDisconnectSchemes = new();

        /// <summary>
        /// Network reconnect event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _networkReconnectSchemes = new();

        /// <summary>
        /// Power low event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _powerLowSchemes = new();

        /// <summary>
        /// Power charging event schemes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SchemeModel> _powerChargingSchemes = new();

        // ==================== Widget Styles ====================

        /// <summary>
        /// Clock widget styles provided by this theme.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ClockStyleModel> _clockStyles = new();

        /// <summary>
        /// Pomodoro widget styles provided by this theme.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PomodoroStyleModel> _pomodoroStyles = new();

        /// <summary>
        /// Anniversary widget styles provided by this theme.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AnniversaryStyleModel> _anniversaryStyles = new();

        // ==================== Methods ====================

        /// <summary>
        /// Resolves the actual file path for a resource.
        /// In local editing mode, returns the absolute SourcePath.
        /// In export mode, combines ExportBasePath with the resource's ExportPath.
        /// </summary>
        public string GetResolvedPath(ResourceEntry resource)
        {
            // Export mode: use relative path from ExportBasePath
            if (!string.IsNullOrEmpty(resource.ExportPath) && !string.IsNullOrEmpty(ExportBasePath))
            {
                return Path.Combine(ExportBasePath, resource.ExportPath);
            }

            // Local mode: use absolute SourcePath
            return resource.SourcePath;
        }

        /// <summary>
        /// Updates the ModifiedAt timestamp to the current UTC time.
        /// </summary>
        public void TouchModified()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
