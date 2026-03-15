using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Service for loading, saving, and validating theme packages.
    /// A theme package is a folder containing theme.json and organized resource subfolders.
    /// </summary>
    public class ThemeService
    {
        private const string ManifestFileName = "theme.json";
        private readonly JsonSerializerOptions _jsonOptions;

        public ThemeService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Loads a theme from a folder containing theme.json.
        /// </summary>
        /// <param name="themeFolderPath">Path to the theme folder.</param>
        /// <param name="validateResources">Whether to validate that all resource files exist.</param>
        /// <returns>The loaded theme manifest, or null on failure.</returns>
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
                    if (!manifest.ResourceLibrary.ValidateAllExist(themeFolderPath, out var missing))
                    {
                        foreach (var m in missing)
                        {
                            Debug.WriteLine($"[ThemeService] Warning: Missing resource - {m}");
                        }
                    }
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
        /// Saves a theme manifest to a folder. Creates subfolders if they don't exist.
        /// </summary>
        public async Task SaveAsync(ThemeManifest manifest, string themeFolderPath)
        {
            // Ensure folder structure exists
            Directory.CreateDirectory(themeFolderPath);
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "images"));
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "videos"));
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "audio"));
            Directory.CreateDirectory(Path.Combine(themeFolderPath, "thumbnails"));

            // Update timestamp
            manifest.UpdatedAt = DateTime.UtcNow;

            var manifestPath = Path.Combine(themeFolderPath, ManifestFileName);
            var json = JsonSerializer.Serialize(manifest, _jsonOptions);
            await File.WriteAllTextAsync(manifestPath, json);

            Debug.WriteLine($"[ThemeService] Saved theme: {manifest.Name} to {manifestPath}");
        }

        /// <summary>
        /// Creates a new empty theme with the specified metadata.
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
        /// Validates a theme manifest and its resources.
        /// </summary>
        /// <returns>True if valid; false with error messages.</returns>
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
        /// Adds a resource file to the theme package.
        /// Copies the file to the appropriate subfolder and creates a ResourceEntry.
        /// </summary>
        /// <returns>The ID of the newly added resource.</returns>
        public async Task<string> AddResourceAsync(ThemeManifest manifest, string sourceFilePath, string themeFolderPath)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            var mediaType = DetermineMediaType(extension);
            var typeFolder = ThemeResourceLibrary.GetTypeFolderName(mediaType);
            var destFolder = Path.Combine(themeFolderPath, typeFolder);

            Directory.CreateDirectory(destFolder);

            // Handle name collisions
            var destPath = Path.Combine(destFolder, fileName);
            if (File.Exists(destPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                fileName = $"{nameWithoutExt}_{Guid.NewGuid().ToString()[..8]}{extension}";
                destPath = Path.Combine(destFolder, fileName);
            }

            // Copy file
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

            manifest.ResourceLibrary.Resources.Add(entry);
            manifest.ResourceLibrary.InvalidateIndex();

            Debug.WriteLine($"[ThemeService] Added resource: {entry.Id} ({fileName})");
            return entry.Id;
        }

        /// <summary>
        /// Removes a resource from the theme and deletes its files.
        /// </summary>
        public Task RemoveResourceAsync(ThemeManifest manifest, string resourceId, string themeFolderPath)
        {
            var entry = manifest.ResourceLibrary.GetById(resourceId);
            if (entry == null) return Task.CompletedTask;

            // Delete media file
            var mediaPath = manifest.ResourceLibrary.GetMediaPath(resourceId, themeFolderPath);
            if (mediaPath != null && File.Exists(mediaPath))
            {
                File.Delete(mediaPath);
            }

            // Delete thumbnail
            var thumbPath = manifest.ResourceLibrary.GetThumbnailPath(resourceId, themeFolderPath);
            if (thumbPath != null && File.Exists(thumbPath))
            {
                File.Delete(thumbPath);
            }

            // Remove from library
            manifest.ResourceLibrary.Resources.Remove(entry);
            manifest.ResourceLibrary.InvalidateIndex();

            Debug.WriteLine($"[ThemeService] Removed resource: {resourceId}");
            return Task.CompletedTask;
        }

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
    }
}
