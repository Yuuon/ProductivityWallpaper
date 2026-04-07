using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Service for theme package lifecycle operations.
    /// Supports two-phase workflow:
    /// - Local editing: resources referenced by absolute path, no file copying
    /// - Export: creates complete portable package with copied files and relative paths
    /// Theme data stored in %AppData%/ProductivityWallpaper/Themes/{ThemeName}/
    /// </summary>
    public class ThemeService : IThemeService
    {
        private const string ManifestFileName = "theme.json";
        private const string BackupSuffix = ".backup";
        private const string AppFolderName = "ProductivityWallpaper";
        private const string ThemesFolderName = "Themes";

        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _themesRootPath;

        /// <summary>
        /// Gets or sets the current theme being edited.
        /// </summary>
        public ThemeManifest? CurrentTheme { get; set; }

        public ThemeService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            _themesRootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName,
                ThemesFolderName);
        }

        // ==================== Theme Lifecycle ====================

        /// <inheritdoc/>
        public async Task<ThemeManifest?> LoadThemeAsync(string themeName)
        {
            var themeFolderPath = GetThemeFolderPath(themeName);
            var manifestPath = Path.Combine(themeFolderPath, ManifestFileName);

            if (!File.Exists(manifestPath))
            {
                Debug.WriteLine($"[ThemeService] Manifest not found: {manifestPath}");
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ThemeManifest>(json, _jsonOptions);

                if (manifest == null)
                {
                    Debug.WriteLine($"[ThemeService] Failed to deserialize manifest: {manifestPath}");
                    return null;
                }

                // Validate all resource paths
                manifest.ResourceLibrary.ValidateAll();

                CurrentTheme = manifest;
                Debug.WriteLine($"[ThemeService] Loaded theme: {manifest.Name} v{manifest.Version}");
                return manifest;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] Error loading theme: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SaveThemeAsync(ThemeManifest theme, bool isBackup = false)
        {
            var themeFolderPath = GetThemeFolderPath(theme.Name);
            Directory.CreateDirectory(themeFolderPath);

            // Update timestamp
            theme.TouchModified();

            var fileName = isBackup
                ? ManifestFileName + BackupSuffix
                : ManifestFileName;

            var manifestPath = Path.Combine(themeFolderPath, fileName);
            var json = JsonSerializer.Serialize(theme, _jsonOptions);
            await File.WriteAllTextAsync(manifestPath, json);

            Debug.WriteLine($"[ThemeService] {(isBackup ? "Backup saved" : "Saved")} theme: {theme.Name} to {manifestPath}");
        }

        /// <inheritdoc/>
        public async Task<ThemeManifest> CreateThemeAsync(string themeName)
        {
            var manifest = new ThemeManifest
            {
                Name = themeName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await SaveThemeAsync(manifest);
            CurrentTheme = manifest;

            Debug.WriteLine($"[ThemeService] Created new theme: {themeName}");
            return manifest;
        }

        /// <inheritdoc/>
        public bool ThemeExists(string themeName)
        {
            var manifestPath = Path.Combine(GetThemeFolderPath(themeName), ManifestFileName);
            return File.Exists(manifestPath);
        }

        /// <inheritdoc/>
        public string[] GetThemeNames()
        {
            if (!Directory.Exists(_themesRootPath))
                return Array.Empty<string>();

            var dirs = Directory.GetDirectories(_themesRootPath);
            var names = new List<string>();
            foreach (var dir in dirs)
            {
                var manifestPath = Path.Combine(dir, ManifestFileName);
                if (File.Exists(manifestPath))
                {
                    names.Add(Path.GetFileName(dir));
                }
            }
            return names.ToArray();
        }

        // ==================== Import (Reference Mode) ====================

        /// <inheritdoc/>
        public Task<ResourceEntry?> ImportResourceAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[ThemeService] Import failed: file not found: {filePath}");
                return Task.FromResult<ResourceEntry?>(null);
            }

            // Check for duplicate
            if (IsDuplicate(filePath, out var existing))
            {
                Debug.WriteLine($"[ThemeService] Duplicate detected: {filePath} matches {existing!.Id}");
                return Task.FromResult<ResourceEntry?>(existing);
            }

            var fileInfo = new FileInfo(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var mediaType = DetermineMediaType(extension);

            var entry = new ResourceEntry
            {
                SourcePath = filePath,
                Hash = ComputeHash(filePath),
                Type = mediaType,
                OriginalName = Path.GetFileName(filePath),
                FileName = Path.GetFileName(filePath),
                Format = extension,
                FileSize = fileInfo.Length
            };

            Debug.WriteLine($"[ThemeService] Imported resource by reference: {entry.Id} ({entry.OriginalName})");
            return Task.FromResult<ResourceEntry?>(entry);
        }

        /// <inheritdoc/>
        public bool IsDuplicate(string filePath, out ResourceEntry? existing)
        {
            existing = null;
            if (CurrentTheme == null) return false;

            var hash = ComputeHash(filePath);
            existing = CurrentTheme.ResourceLibrary.GetByHash(hash);
            return existing != null;
        }

        /// <inheritdoc/>
        public string ComputeHash(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var crc = new Crc32();
                var buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    crc.Append(buffer.AsSpan(0, bytesRead));
                }
                var hashValue = crc.GetCurrentHashAsUInt32();
                return $"crc32:{hashValue:X8}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] Hash computation failed for {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        // ==================== Export (Bundle Mode) ====================

        /// <inheritdoc/>
        public async Task<string> ExportThemeAsync(ThemeManifest theme, string exportLocation,
            IProgress<string>? progress = null, CancellationToken ct = default)
        {
            var exportPath = Path.Combine(exportLocation, SanitizeFolderName(theme.Name));

            // Create folder structure
            Directory.CreateDirectory(exportPath);
            Directory.CreateDirectory(Path.Combine(exportPath, "images"));
            Directory.CreateDirectory(Path.Combine(exportPath, "videos"));
            Directory.CreateDirectory(Path.Combine(exportPath, "audio"));
            Directory.CreateDirectory(Path.Combine(exportPath, "thumbnails"));

            progress?.Report("Creating folder structure...");

            // Copy all referenced files
            foreach (var resource in theme.ResourceLibrary.GetAll())
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(resource.SourcePath) || !File.Exists(resource.SourcePath))
                {
                    progress?.Report($"Skipped (missing): {resource.OriginalName}");
                    continue;
                }

                var subfolder = ThemeResourceLibrary.GetTypeFolderName(resource.Type);
                var destFilename = SanitizeFileName(resource.OriginalName);

                // Handle name collisions
                var destPath = Path.Combine(exportPath, subfolder, destFilename);
                if (File.Exists(destPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(destFilename);
                    var ext = Path.GetExtension(destFilename);
                    var idSuffix = resource.Id.Length >= 8 ? resource.Id[..8] : resource.Id;
                    destFilename = $"{nameWithoutExt}_{idSuffix}{ext}";
                    destPath = Path.Combine(exportPath, subfolder, destFilename);
                }

                // Copy file
                await Task.Run(() => File.Copy(resource.SourcePath, destPath, overwrite: true), ct);
                resource.ExportPath = $"{subfolder}/{destFilename}";

                progress?.Report($"Exported: {resource.OriginalName}");
            }

            // Save theme.json with ExportPath values
            theme.ExportBasePath = exportPath;
            var manifestPath = Path.Combine(exportPath, ManifestFileName);
            var json = JsonSerializer.Serialize(theme, _jsonOptions);
            await File.WriteAllTextAsync(manifestPath, json, ct);

            // Clear ExportBasePath on the original theme (it's still in local editing mode)
            theme.ExportBasePath = null;

            progress?.Report("Export complete!");
            Debug.WriteLine($"[ThemeService] Exported theme to: {exportPath}");
            return exportPath;
        }

        // ==================== Validation ====================

        /// <inheritdoc/>
        public void ValidateTheme(ThemeManifest theme)
        {
            theme.ResourceLibrary.ValidateAll();
        }

        // ==================== Utility ====================

        /// <inheritdoc/>
        public string GetThemesRootPath()
        {
            return _themesRootPath;
        }

        /// <inheritdoc/>
        public string GetThemeFolderPath(string themeName)
        {
            return Path.Combine(_themesRootPath, SanitizeFolderName(themeName));
        }

        /// <inheritdoc/>
        public string GetBackupPath(string themeName)
        {
            return Path.Combine(GetThemeFolderPath(themeName), ManifestFileName + BackupSuffix);
        }

        // ==================== Legacy Compatibility ====================

        /// <summary>
        /// Loads a theme from a specific folder path (legacy API for backward compatibility).
        /// </summary>
        public async Task<ThemeManifest?> LoadAsync(string themeFolderPath, bool validateResources = true)
        {
            var manifestPath = Path.Combine(themeFolderPath, ManifestFileName);

            if (!File.Exists(manifestPath))
            {
                Debug.WriteLine($"[ThemeService] Manifest not found: {manifestPath}");
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ThemeManifest>(json, _jsonOptions);

                if (manifest == null)
                {
                    Debug.WriteLine($"[ThemeService] Failed to deserialize manifest: {manifestPath}");
                    return null;
                }

                if (validateResources)
                {
                    manifest.ResourceLibrary.ValidateAll(themeFolderPath);
                }

                Debug.WriteLine($"[ThemeService] Loaded theme: {manifest.Name} v{manifest.Version}");
                return manifest;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] Error loading theme: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves a theme manifest to a specific folder (legacy API for backward compatibility).
        /// </summary>
        public async Task SaveAsync(ThemeManifest manifest, string themeFolderPath)
        {
            Directory.CreateDirectory(themeFolderPath);
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "images"));
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "videos"));
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "audio"));
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "thumbnails"));

            manifest.UpdatedAt = DateTime.UtcNow;

            var manifestPath = Path.Combine(themeFolderPath, ManifestFileName);
            var json = JsonSerializer.Serialize(manifest, _jsonOptions);
            await File.WriteAllTextAsync(manifestPath, json);

            Debug.WriteLine($"[ThemeService] Saved theme: {manifest.Name} to {manifestPath}");
        }

        /// <summary>
        /// Creates a new empty theme (legacy API).
        /// </summary>
        public ThemeManifest CreateNew(string name, string author)
        {
            return new ThemeManifest
            {
                Name = name,
                Author = author,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Validates a theme manifest and its resources (legacy API).
        /// </summary>
        public bool Validate(ThemeManifest manifest, string themeFolderPath, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(manifest.Name))
                errors.Add("Theme name is required");

            if (string.IsNullOrWhiteSpace(manifest.Author))
                errors.Add("Theme author is required");

            if (string.IsNullOrWhiteSpace(manifest.Version))
                errors.Add("Theme version is required");

            if (!manifest.ResourceLibrary.ValidateAllExist(themeFolderPath, out var missingFiles))
            {
                foreach (var m in missingFiles)
                {
                    errors.Add($"Missing resource file: {m}");
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Adds a resource file to the theme package (legacy API — copies file immediately).
        /// </summary>
        public async Task<string> AddResourceAsync(ThemeManifest manifest, string sourceFilePath, string themeFolderPath)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            var mediaType = DetermineMediaType(extension);
            var typeFolder = ThemeResourceLibrary.GetTypeFolderName(mediaType);
            var destFolder = Path.Combine(themeFolderPath, typeFolder);

            Directory.CreateDirectory(destFolder);

            var destPath = Path.Combine(destFolder, fileName);
            if (File.Exists(destPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                fileName = $"{nameWithoutExt}_{Guid.NewGuid()}{extension}";
                destPath = Path.Combine(destFolder, fileName);
            }

            using (var source = File.OpenRead(sourceFilePath))
            using (var dest = File.Create(destPath))
            {
                await source.CopyToAsync(dest);
            }

            var fileInfo = new FileInfo(destPath);
            var entry = new ResourceEntry
            {
                FileName = fileName,
                Type = mediaType,
                Format = extension,
                FileSize = fileInfo.Length
            };

            manifest.ResourceLibrary.Add(entry);

            Debug.WriteLine($"[ThemeService] Added resource: {entry.Id} ({fileName})");
            return entry.Id;
        }

        /// <summary>
        /// Removes a resource from the theme and deletes its files (legacy API).
        /// </summary>
        public Task RemoveResourceAsync(ThemeManifest manifest, string resourceId, string themeFolderPath)
        {
            var entry = manifest.ResourceLibrary.GetById(resourceId);
            if (entry == null) return Task.CompletedTask;

            var mediaPath = manifest.ResourceLibrary.GetMediaPath(resourceId, themeFolderPath);
            if (mediaPath != null && File.Exists(mediaPath))
            {
                File.Delete(mediaPath);
            }

            var thumbPath = manifest.ResourceLibrary.GetThumbnailPath(resourceId, themeFolderPath);
            if (thumbPath != null && File.Exists(thumbPath))
            {
                File.Delete(thumbPath);
            }

            manifest.ResourceLibrary.Remove(resourceId);

            Debug.WriteLine($"[ThemeService] Removed resource: {resourceId}");
            return Task.CompletedTask;
        }

        // ==================== Private Helpers ====================

        private static MediaType DetermineMediaType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif" or ".heic" => MediaType.Image,
                ".mp4" or ".mov" or ".webm" or ".avi" or ".mkv" => MediaType.Video,
                ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" => MediaType.Audio,
                _ => MediaType.Image
            };
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
        }

        private static string SanitizeFolderName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
        }
    }
}
