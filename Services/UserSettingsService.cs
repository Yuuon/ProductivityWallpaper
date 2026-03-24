using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Service for loading and saving user-specific widget settings.
    /// Settings are stored in user_config.json alongside AppConfig.
    /// Singleton service with internal caching.
    /// </summary>
    public class UserSettingsService
    {
        private const string SettingsFileName = "user_config.json";
        private readonly string _settingsPath;
        private readonly JsonSerializerOptions _jsonOptions;
        private UserWidgetSettings? _cachedSettings;

        public UserSettingsService()
        {
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProductivityWallpaper",
                SettingsFileName);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Loads user widget settings. Creates defaults if file doesn't exist.
        /// </summary>
        public async Task<UserWidgetSettings> LoadAsync()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    var config = JsonSerializer.Deserialize<AppConfigWithWidgets>(json, _jsonOptions);
                    if (config?.WidgetSettings != null)
                    {
                        _cachedSettings = config.WidgetSettings;
                        return _cachedSettings;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UserSettingsService] Error loading settings: {ex.Message}");
                }
            }

            // Create defaults
            _cachedSettings = CreateDefaultSettings();
            await SaveAsync(_cachedSettings);
            return _cachedSettings;
        }

        /// <summary>
        /// Saves user widget settings to disk.
        /// Preserves existing config properties when saving.
        /// </summary>
        public async Task SaveAsync(UserWidgetSettings settings)
        {
            _cachedSettings = settings;

            // Load existing config to preserve other settings
            AppConfigWithWidgets config;
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    config = JsonSerializer.Deserialize<AppConfigWithWidgets>(json, _jsonOptions)
                        ?? new AppConfigWithWidgets();
                }
                catch
                {
                    config = new AppConfigWithWidgets();
                }
            }
            else
            {
                config = new AppConfigWithWidgets();
            }

            config.WidgetSettings = settings;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_settingsPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var newJson = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, newJson);

            Debug.WriteLine("[UserSettingsService] Settings saved successfully");
        }

        /// <summary>
        /// Gets the cached settings without reloading from disk.
        /// </summary>
        public UserWidgetSettings? GetCachedSettings() => _cachedSettings;

        /// <summary>
        /// Invalidates the cache, forcing a reload on next access.
        /// </summary>
        public void InvalidateCache() => _cachedSettings = null;

        private static UserWidgetSettings CreateDefaultSettings()
        {
            return new UserWidgetSettings
            {
                ClockSettings = new ClockWidgetSettings(),
                PomodoroSettings = new PomodoroWidgetSettings(),
                AnniversarySettings = new AnniversaryWidgetSettings(),
                GlobalPomodoroSettings = new PomodoroGlobalSettings(),
                UsePerThemePomodoroSettings = false
            };
        }

        /// <summary>
        /// Internal wrapper class to store widget settings alongside existing AppConfig properties.
        /// </summary>
        private class AppConfigWithWidgets
        {
            public string? LastLibraryPath { get; set; }
            public string? MediaLibraryPath { get; set; }
            public UserWidgetSettings? WidgetSettings { get; set; }
        }
    }
}
