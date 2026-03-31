using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Service interface for theme package lifecycle operations.
    /// Supports two-phase workflow: local editing (absolute path references) and export (file copy with relative paths).
    /// </summary>
    public interface IThemeService
    {
        // ==================== Theme Lifecycle ====================

        /// <summary>
        /// Loads a theme from the app data themes folder by name.
        /// </summary>
        /// <param name="themeName">Name of the theme to load.</param>
        /// <returns>The loaded theme manifest, or null if not found.</returns>
        Task<ThemeManifest?> LoadThemeAsync(string themeName);

        /// <summary>
        /// Saves a theme manifest to its folder in app data.
        /// </summary>
        /// <param name="theme">The theme manifest to save.</param>
        /// <param name="isBackup">If true, saves as .backup file (auto-save).</param>
        Task SaveThemeAsync(ThemeManifest theme, bool isBackup = false);

        /// <summary>
        /// Creates a new empty theme with the given name and saves it.
        /// </summary>
        /// <param name="themeName">Display name for the new theme.</param>
        /// <returns>The newly created theme manifest.</returns>
        Task<ThemeManifest> CreateThemeAsync(string themeName);

        /// <summary>
        /// Checks if a theme with the given name already exists.
        /// </summary>
        bool ThemeExists(string themeName);

        /// <summary>
        /// Lists all available theme names in the app data folder.
        /// </summary>
        string[] GetThemeNames();

        // ==================== Import (Reference Mode) ====================

        /// <summary>
        /// Imports a resource by reference (no file copy). Creates a ResourceEntry with absolute SourcePath.
        /// </summary>
        /// <param name="filePath">Absolute path to the source file.</param>
        /// <returns>The created ResourceEntry, or null if duplicate found.</returns>
        Task<ResourceEntry?> ImportResourceAsync(string filePath);

        /// <summary>
        /// Checks if a file is a duplicate by computing its hash and checking the library.
        /// </summary>
        /// <param name="filePath">Path to the file to check.</param>
        /// <param name="existing">The existing entry if duplicate found.</param>
        /// <returns>True if duplicate, false if new.</returns>
        bool IsDuplicate(string filePath, out ResourceEntry? existing);

        /// <summary>
        /// Computes a CRC32 hash for the given file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>Hash string in format "crc32:XXXXXXXX".</returns>
        string ComputeHash(string filePath);

        // ==================== Export (Bundle Mode) ====================

        /// <summary>
        /// Exports a theme as a complete portable package by copying all referenced files.
        /// </summary>
        /// <param name="theme">The theme to export.</param>
        /// <param name="exportLocation">Parent folder for the export.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The full path to the exported theme folder.</returns>
        Task<string> ExportThemeAsync(ThemeManifest theme, string exportLocation,
            IProgress<string>? progress = null, CancellationToken ct = default);

        // ==================== Validation ====================

        /// <summary>
        /// Validates all resource SourcePaths in the theme and updates their Status.
        /// </summary>
        void ValidateTheme(ThemeManifest theme);

        // ==================== Utility ====================

        /// <summary>
        /// Gets the folder path for a theme in app data.
        /// </summary>
        string GetThemeFolderPath(string themeName);

        /// <summary>
        /// Gets the backup file path for a theme.
        /// </summary>
        string GetBackupPath(string themeName);

        /// <summary>
        /// Gets or sets the current theme being edited.
        /// </summary>
        ThemeManifest? CurrentTheme { get; set; }
    }
}
