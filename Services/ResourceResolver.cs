using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Resolves media resource IDs to file system paths within a theme package.
    /// Each instance is bound to a specific theme and its resource library.
    /// </summary>
    public class ResourceResolver
    {
        private readonly ThemeResourceLibrary _library;
        private readonly string _themeRootPath;

        /// <summary>
        /// Creates a new ResourceResolver for the specified theme.
        /// </summary>
        /// <param name="library">The theme's resource library.</param>
        /// <param name="themeRootPath">Root folder path of the theme package.</param>
        public ResourceResolver(ThemeResourceLibrary library, string themeRootPath)
        {
            _library = library;
            _themeRootPath = themeRootPath;
        }

        /// <summary>
        /// Resolves a resource ID to its media file path.
        /// </summary>
        /// <returns>Full file path, or null if resource not found.</returns>
        public string? ResolveMediaPath(string resourceId)
        {
            return _library.GetMediaPath(resourceId, _themeRootPath);
        }

        /// <summary>
        /// Resolves a resource ID to its thumbnail file path.
        /// </summary>
        /// <returns>Full thumbnail path, or null if resource/thumbnail not found.</returns>
        public string? ResolveThumbnailPath(string resourceId)
        {
            return _library.GetThumbnailPath(resourceId, _themeRootPath);
        }

        /// <summary>
        /// Resolves a resource ID to a MediaItemModel for playback.
        /// </summary>
        public MediaItemModel? ResolveToMediaItem(string resourceId)
        {
            var entry = _library.GetById(resourceId);
            if (entry == null) return null;

            var filePath = Path.Combine(_themeRootPath, ThemeResourceLibrary.GetTypeFolderName(entry.Type), entry.FileName);
            var thumbnailPath = entry.ThumbnailFileName != null
                ? Path.Combine(_themeRootPath, "thumbnails", entry.ThumbnailFileName)
                : string.Empty;

            return new MediaItemModel
            {
                Id = entry.Id,
                FilePath = filePath,
                FileName = entry.FileName,
                Type = entry.Type switch
                {
                    MediaType.Image => MediaFileType.Image,
                    MediaType.Video => MediaFileType.Video,
                    MediaType.Audio => MediaFileType.Audio,
                    _ => MediaFileType.Image
                },
                Format = entry.Format,
                FileSize = entry.FileSize,
                Duration = entry.Duration,
                ThumbnailPath = thumbnailPath
            };
        }

        /// <summary>
        /// Resolves multiple resource IDs to MediaItemModels.
        /// Skips resources that cannot be resolved (graceful degradation).
        /// </summary>
        public IEnumerable<MediaItemModel> ResolveToMediaItems(IEnumerable<string> resourceIds)
        {
            return resourceIds
                .Select(ResolveToMediaItem)
                .Where(item => item != null)!;
        }

        /// <summary>
        /// Checks if a resource exists in the library.
        /// </summary>
        public bool ResourceExists(string resourceId)
        {
            return _library.GetById(resourceId) != null;
        }

        /// <summary>
        /// Gets all resource IDs that don't exist in the library.
        /// </summary>
        public IEnumerable<string> GetMissingResources(IEnumerable<string> resourceIds)
        {
            return resourceIds.Where(id => _library.GetById(id) == null);
        }
    }
}
