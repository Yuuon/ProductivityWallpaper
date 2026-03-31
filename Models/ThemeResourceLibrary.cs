using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Container for all media resources in a theme package.
    /// Provides O(1) lookup by ID and hash, and path building utilities.
    /// </summary>
    public partial class ThemeResourceLibrary : ObservableObject
    {
        /// <summary>
        /// All resources in this theme.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ResourceEntry> _resources = new();

        // Internal lookup caches (rebuilt when resources change)
        private Dictionary<string, ResourceEntry>? _idIndex;
        private Dictionary<string, ResourceEntry>? _hashIndex;

        /// <summary>
        /// Gets a resource by its unique ID.
        /// </summary>
        public ResourceEntry? GetById(string id)
        {
            EnsureIndex();
            return _idIndex!.TryGetValue(id, out var entry) ? entry : null;
        }

        /// <summary>
        /// Gets a resource by its content hash for deduplication.
        /// </summary>
        public ResourceEntry? GetByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return null;
            EnsureIndex();
            return _hashIndex!.TryGetValue(hash, out var entry) ? entry : null;
        }

        /// <summary>
        /// Checks if a resource with the given hash already exists (fast duplicate check).
        /// </summary>
        public bool ContainsHash(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;
            EnsureIndex();
            return _hashIndex!.ContainsKey(hash);
        }

        /// <summary>
        /// Gets a resource by filename and type.
        /// </summary>
        public ResourceEntry? GetByFileName(string fileName, MediaType type)
        {
            return Resources.FirstOrDefault(r =>
                r.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) &&
                r.Type == type);
        }

        /// <summary>
        /// Gets all resources of a specific type.
        /// </summary>
        public IEnumerable<ResourceEntry> GetByType(MediaType type)
        {
            return Resources.Where(r => r.Type == type);
        }

        /// <summary>
        /// Gets all resources in the library.
        /// </summary>
        public IEnumerable<ResourceEntry> GetAll()
        {
            return Resources;
        }

        /// <summary>
        /// Total number of resources in the library.
        /// </summary>
        public int Count => Resources.Count;

        /// <summary>
        /// Adds a resource to the library and updates lookup indices.
        /// </summary>
        public void Add(ResourceEntry entry)
        {
            Resources.Add(entry);
            InvalidateIndex();
        }

        /// <summary>
        /// Removes a resource from the library by ID.
        /// </summary>
        /// <returns>True if the resource was found and removed.</returns>
        public bool Remove(string id)
        {
            var entry = GetById(id);
            if (entry == null) return false;
            Resources.Remove(entry);
            InvalidateIndex();
            return true;
        }

        /// <summary>
        /// Gets the full path to a resource's thumbnail.
        /// </summary>
        public string? GetThumbnailPath(string resourceId, string themeRootPath)
        {
            var entry = GetById(resourceId);
            if (entry?.ThumbnailFileName == null) return null;
            return Path.Combine(themeRootPath, "thumbnails", entry.ThumbnailFileName);
        }

        /// <summary>
        /// Gets the full path to a resource's media file.
        /// For local editing mode, returns SourcePath if available.
        /// For exported themes, constructs path from theme root and type folder.
        /// </summary>
        public string? GetMediaPath(string resourceId, string themeRootPath)
        {
            var entry = GetById(resourceId);
            if (entry == null) return null;

            // Local editing mode: use absolute SourcePath if available
            if (!string.IsNullOrEmpty(entry.SourcePath))
                return entry.SourcePath;

            // Export/bundle mode: construct from theme root
            var typeFolder = GetTypeFolderName(entry.Type);
            return Path.Combine(themeRootPath, typeFolder, entry.FileName);
        }

        /// <summary>
        /// Validates all resources by checking file existence and updating their Status.
        /// For local editing mode, checks SourcePath. For exported themes, checks relative paths.
        /// </summary>
        /// <param name="themeRootPath">Root path of the theme folder (used for export mode only).</param>
        public void ValidateAll(string? themeRootPath = null)
        {
            foreach (var resource in Resources)
            {
                // Check SourcePath first (local editing mode)
                if (!string.IsNullOrEmpty(resource.SourcePath))
                {
                    if (File.Exists(resource.SourcePath))
                    {
                        resource.Status = ResourceStatus.OK;
                        resource.StatusMessage = null;
                    }
                    else
                    {
                        resource.Status = ResourceStatus.Missing;
                        resource.StatusMessage = $"File not found: {resource.SourcePath}";
                    }
                }
                // Check export path (exported theme mode)
                else if (!string.IsNullOrEmpty(resource.ExportPath) && !string.IsNullOrEmpty(themeRootPath))
                {
                    var fullPath = Path.Combine(themeRootPath, resource.ExportPath);
                    if (File.Exists(fullPath))
                    {
                        resource.Status = ResourceStatus.OK;
                        resource.StatusMessage = null;
                    }
                    else
                    {
                        resource.Status = ResourceStatus.Missing;
                        resource.StatusMessage = $"File not found: {fullPath}";
                    }
                }
                // Check FileName-based path (legacy bundle mode)
                else if (!string.IsNullOrEmpty(resource.FileName) && !string.IsNullOrEmpty(themeRootPath))
                {
                    var path = Path.Combine(themeRootPath, GetTypeFolderName(resource.Type), resource.FileName);
                    if (!File.Exists(path))
                    {
                        resource.Status = ResourceStatus.Missing;
                        resource.StatusMessage = $"File not found: {path}";
                    }
                    else
                    {
                        resource.Status = ResourceStatus.OK;
                        resource.StatusMessage = null;
                    }
                }
            }
        }

        /// <summary>
        /// Validates that all referenced resource files exist on disk (legacy compatibility).
        /// </summary>
        public bool ValidateAllExist(string themeRootPath, out List<string> missingFiles)
        {
            missingFiles = new List<string>();
            foreach (var resource in Resources)
            {
                // For local editing, check SourcePath
                if (!string.IsNullOrEmpty(resource.SourcePath))
                {
                    if (!File.Exists(resource.SourcePath))
                    {
                        missingFiles.Add($"{resource.Id}: {resource.SourcePath}");
                    }
                    continue;
                }

                // For exported/bundle mode, check relative path
                var path = Path.Combine(themeRootPath, GetTypeFolderName(resource.Type), resource.FileName);
                if (!File.Exists(path))
                {
                    missingFiles.Add($"{resource.Id}: {path}");
                }
            }
            return missingFiles.Count == 0;
        }

        /// <summary>
        /// Invalidates the internal lookup index. Call after modifying Resources collection.
        /// </summary>
        public void InvalidateIndex()
        {
            _idIndex = null;
            _hashIndex = null;
        }

        private void EnsureIndex()
        {
            if (_idIndex == null || _hashIndex == null)
            {
                _idIndex = new Dictionary<string, ResourceEntry>();
                _hashIndex = new Dictionary<string, ResourceEntry>();
                foreach (var entry in Resources)
                {
                    _idIndex[entry.Id] = entry;
                    if (!string.IsNullOrEmpty(entry.Hash))
                    {
                        _hashIndex[entry.Hash] = entry;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the folder name for a media type (images, videos, audio).
        /// </summary>
        public static string GetTypeFolderName(MediaType type)
        {
            return type switch
            {
                MediaType.Image => "images",
                MediaType.Video => "videos",
                MediaType.Audio => "audio",
                _ => "other"
            };
        }

        partial void OnResourcesChanged(ObservableCollection<ResourceEntry> value)
        {
            InvalidateIndex();
        }
    }
}
