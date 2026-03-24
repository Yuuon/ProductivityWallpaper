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
    /// Provides O(1) lookup by ID and path building utilities.
    /// </summary>
    public partial class ThemeResourceLibrary : ObservableObject
    {
        /// <summary>
        /// All resources in this theme.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ResourceEntry> _resources = new();

        // Internal lookup cache (rebuilt when resources change)
        private Dictionary<string, ResourceEntry>? _idIndex;

        /// <summary>
        /// Gets a resource by its unique ID.
        /// </summary>
        public ResourceEntry? GetById(string id)
        {
            EnsureIndex();
            return _idIndex!.TryGetValue(id, out var entry) ? entry : null;
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
        /// </summary>
        public string? GetMediaPath(string resourceId, string themeRootPath)
        {
            var entry = GetById(resourceId);
            if (entry == null) return null;
            var typeFolder = GetTypeFolderName(entry.Type);
            return Path.Combine(themeRootPath, typeFolder, entry.FileName);
        }

        /// <summary>
        /// Validates that all referenced resource files exist on disk.
        /// </summary>
        public bool ValidateAllExist(string themeRootPath, out List<string> missingFiles)
        {
            missingFiles = new List<string>();
            foreach (var resource in Resources)
            {
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
        }

        private void EnsureIndex()
        {
            if (_idIndex == null)
            {
                _idIndex = new Dictionary<string, ResourceEntry>();
                foreach (var entry in Resources)
                {
                    _idIndex[entry.Id] = entry;
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
