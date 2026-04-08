using System.Threading;
using System.Threading.Tasks;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Service for generating and managing media thumbnails.
    /// All thumbnails are stored in the theme's /thumbnails/ folder alongside the theme configuration.
    /// This ensures thumbnails persist across sessions and are included in theme package exports.
    /// Uses FFMpegCore (NuGet) for animated GIF generation from video files.
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
        /// JPEG quality level for generated image thumbnails.
        /// </summary>
        const int ThumbnailQuality = 85;

        /// <summary>
        /// Generates a thumbnail for a resource entry.
        /// Stores the result in the theme's thumbnails/ subfolder.
        /// </summary>
        /// <param name="resource">The resource to generate a thumbnail for.</param>
        /// <param name="themeFolderPath">Theme folder path where thumbnails are stored.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated thumbnail file path, or empty string on failure.</returns>
        Task<string> GenerateThumbnailAsync(ResourceEntry resource, string themeFolderPath,
            CancellationToken ct = default);

        /// <summary>
        /// Generates a thumbnail from a file path.
        /// Stores the result in the theme's thumbnails/ subfolder.
        /// </summary>
        /// <param name="sourcePath">Path to the source media file.</param>
        /// <param name="resourceId">Resource ID for naming the thumbnail.</param>
        /// <param name="resourceType">Type of media resource.</param>
        /// <param name="themeFolderPath">Theme folder path where thumbnails are stored.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated thumbnail file path, or empty string on failure.</returns>
        Task<string> GenerateThumbnailAsync(string sourcePath, string resourceId, MediaType resourceType,
            string themeFolderPath, CancellationToken ct = default);

        /// <summary>
        /// Gets the expected thumbnail path for a resource in the theme folder.
        /// For images: returns JPEG path. For videos: returns GIF path.
        /// </summary>
        /// <param name="resourceId">Resource ID.</param>
        /// <param name="themeFolderPath">Theme folder path.</param>
        /// <param name="isVideo">True for video (GIF), false for image (JPEG).</param>
        /// <returns>Expected thumbnail file path in theme's thumbnails/ subfolder.</returns>
        string GetThumbnailPath(string resourceId, string themeFolderPath, bool isVideo = false);

        /// <summary>
        /// Checks if a thumbnail exists for the given resource in the theme folder.
        /// </summary>
        bool ThumbnailExists(string resourceId, string themeFolderPath, bool isVideo = false);

        /// <summary>
        /// Deletes the thumbnail for the given resource from the theme folder.
        /// Tries both JPEG and GIF variants.
        /// </summary>
        void DeleteThumbnail(string resourceId, string themeFolderPath);

        /// <summary>
        /// Generates an animated GIF thumbnail from a video file using FFMpegCore.
        /// Creates a looping GIF from the first 5 seconds (maximum) of the video.
        /// Thumbnails are always stored in the theme's thumbnails/ subfolder.
        /// </summary>
        /// <param name="sourcePath">Path to the source video file.</param>
        /// <param name="resourceId">Resource ID for naming the thumbnail.</param>
        /// <param name="themeFolderPath">Theme folder path (required). Thumbnails are saved
        /// to {themeFolderPath}/thumbnails/{resourceId}_thumb.gif.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated GIF thumbnail file path, or empty string on failure.</returns>
        Task<string> GenerateVideoGifThumbnailAsync(string sourcePath, string resourceId,
            string themeFolderPath, CancellationToken ct = default);

        /// <summary>
        /// Gets the expected GIF thumbnail path for a video resource.
        /// </summary>
        /// <param name="resourceId">Resource ID.</param>
        /// <param name="themeFolderPath">Theme folder path (required).</param>
        string GetGifThumbnailPath(string resourceId, string themeFolderPath);

        /// <summary>
        /// Checks if FFmpeg is available on the system (required by FFMpegCore).
        /// </summary>
        bool IsFFmpegAvailable();
    }
}
