using System.Threading;
using System.Threading.Tasks;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Service for generating and managing media thumbnails.
    /// Supports two modes:
    /// - Temp mode: thumbnails saved to %Temp%/ProductivityWallpaper/Thumbnails/ (local editing)
    /// - Export mode: thumbnails saved to theme's /thumbnails/ folder (theme package)
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// Default thumbnail width in pixels.
        /// </summary>
        const int ThumbnailWidth = 320;

        /// <summary>
        /// Default thumbnail height in pixels.
        /// </summary>
        const int ThumbnailHeight = 180;

        /// <summary>
        /// JPEG quality level for generated thumbnails.
        /// </summary>
        const int ThumbnailQuality = 85;

        /// <summary>
        /// Generates a thumbnail for a resource entry.
        /// </summary>
        /// <param name="resource">The resource to generate a thumbnail for.</param>
        /// <param name="forExport">If true, saves to export folder; otherwise saves to temp.</param>
        /// <param name="exportFolder">Export folder path (required when forExport is true).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated thumbnail file path, or empty string on failure.</returns>
        Task<string> GenerateThumbnailAsync(ResourceEntry resource, bool forExport = false,
            string? exportFolder = null, CancellationToken ct = default);

        /// <summary>
        /// Generates a thumbnail from a file path.
        /// </summary>
        /// <param name="sourcePath">Path to the source media file.</param>
        /// <param name="resourceId">Resource ID for naming the thumbnail.</param>
        /// <param name="resourceType">Type of media resource.</param>
        /// <param name="forExport">If true, saves to export folder.</param>
        /// <param name="exportFolder">Export folder path.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated thumbnail file path, or empty string on failure.</returns>
        Task<string> GenerateThumbnailAsync(string sourcePath, string resourceId, MediaType resourceType,
            bool forExport = false, string? exportFolder = null, CancellationToken ct = default);

        /// <summary>
        /// Gets the expected thumbnail path for a resource without generating it.
        /// </summary>
        /// <param name="resourceId">Resource ID.</param>
        /// <param name="forExport">If true, returns export path; otherwise temp path.</param>
        /// <param name="exportFolder">Export folder path.</param>
        /// <returns>Expected thumbnail file path.</returns>
        string GetThumbnailPath(string resourceId, bool forExport = false, string? exportFolder = null);

        /// <summary>
        /// Checks if a thumbnail exists for the given resource.
        /// </summary>
        bool ThumbnailExists(string resourceId, bool forExport = false, string? exportFolder = null);

        /// <summary>
        /// Deletes the thumbnail for the given resource if it exists.
        /// </summary>
        void DeleteThumbnail(string resourceId, bool forExport = false, string? exportFolder = null);

        /// <summary>
        /// Generates an animated GIF thumbnail from a video file using FFmpeg.
        /// Creates a looping GIF from the first 5 seconds (maximum) of the video.
        /// Falls back to a static JPEG thumbnail if FFmpeg is not available.
        /// </summary>
        /// <param name="sourcePath">Path to the source video file.</param>
        /// <param name="resourceId">Resource ID for naming the thumbnail.</param>
        /// <param name="themeFolderPath">Optional theme folder path. When provided, thumbnails are saved
        /// to the theme's thumbnails/ subfolder instead of %Temp%.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated GIF thumbnail file path, or empty string on failure.</returns>
        Task<string> GenerateVideoGifThumbnailAsync(string sourcePath, string resourceId,
            string? themeFolderPath = null, CancellationToken ct = default);

        /// <summary>
        /// Gets the expected GIF thumbnail path for a video resource.
        /// </summary>
        /// <param name="resourceId">Resource ID.</param>
        /// <param name="themeFolderPath">Optional theme folder path. When provided, returns the path
        /// inside the theme's thumbnails/ subfolder.</param>
        string GetGifThumbnailPath(string resourceId, string? themeFolderPath = null);

        /// <summary>
        /// Checks if FFmpeg is available on the system PATH.
        /// </summary>
        bool IsFFmpegAvailable();
    }
}
