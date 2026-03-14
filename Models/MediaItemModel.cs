using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the type of media file for desktop background schemes.
    /// </summary>
    public enum MediaFileType
    {
        /// <summary>
        /// Static image file (JPG, PNG, WebP, etc.).
        /// </summary>
        Image,

        /// <summary>
        /// Video file (MP4, MOV, WebM, etc.).
        /// </summary>
        Video,

        /// <summary>
        /// Audio file (MP3, WAV, OGG, etc.).
        /// </summary>
        Audio
    }

    /// <summary>
    /// Represents the display mode for images on the desktop.
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>
        /// Fill the entire screen (stretch to fit).
        /// </summary>
        Fill,

        /// <summary>
        /// Center the image on screen.
        /// </summary>
        Center,

        /// <summary>
        /// Tile the image across the screen (images only).
        /// </summary>
        Tile
    }

    /// <summary>
    /// Represents the playback mode for media files.
    /// </summary>
    public enum PlaybackMode
    {
        /// <summary>
        /// Play files in sequential order and loop.
        /// </summary>
        Sequential,

        /// <summary>
        /// Play files in random order.
        /// </summary>
        Random
    }

    /// <summary>
    /// Represents a media item (image, video, or audio) in a desktop background scheme.
    /// </summary>
    public partial class MediaItemModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the unique identifier for this media item.
        /// </summary>
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the full file path to the media file.
        /// </summary>
        [ObservableProperty]
        private string _filePath = string.Empty;

        /// <summary>
        /// Gets or sets the file name (without path) of the media file.
        /// </summary>
        [ObservableProperty]
        private string _fileName = string.Empty;

        /// <summary>
        /// Gets or sets the type of media (Image, Video, or Audio).
        /// </summary>
        [ObservableProperty]
        private MediaFileType _type;

        /// <summary>
        /// Gets or sets the file format/extension (e.g., ".mp4", ".jpg").
        /// </summary>
        [ObservableProperty]
        private string _format = string.Empty;

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [ObservableProperty]
        private long _fileSize;

        /// <summary>
        /// Gets or sets the duration of the media (for video/audio files).
        /// </summary>
        [ObservableProperty]
        private TimeSpan? _duration;

        /// <summary>
        /// Gets or sets a value indicating whether the media is muted.
        /// </summary>
        [ObservableProperty]
        private bool _isMuted;

        /// <summary>
        /// Gets or sets the display mode for this media item.
        /// </summary>
        [ObservableProperty]
        private DisplayMode _displayMode = DisplayMode.Fill;

        /// <summary>
        /// Gets or sets the path to the thumbnail image for this media item.
        /// </summary>
        [ObservableProperty]
        private string _thumbnailPath = string.Empty;

        /// <summary>
        /// Gets or sets the order index for sorting media items in a playlist.
        /// </summary>
        [ObservableProperty]
        private int _orderIndex;

        /// <summary>
        /// Partial method called when DisplayMode changes.
        /// Validates that Tile mode is only used for images.
        /// </summary>
        /// <param name="value">The new display mode value.</param>
        partial void OnDisplayModeChanged(DisplayMode value)
        {
            // Tile mode is only valid for images
            if (value == DisplayMode.Tile && Type != MediaType.Image)
            {
                // Revert to Fill mode for non-image types
                DisplayMode = DisplayMode.Fill;
            }
        }

        /// <summary>
        /// Partial method called when Type changes.
        /// Ensures DisplayMode is valid for the media type.
        /// </summary>
        /// <param name="value">The new media type value.</param>
        partial void OnTypeChanged(MediaFileType value)
        {
            // If changing to video/audio and current display mode is Tile, change to Fill
            if (value != MediaType.Image && DisplayMode == DisplayMode.Tile)
            {
                DisplayMode = DisplayMode.Fill;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaItemModel"/> class.
        /// </summary>
        public MediaItemModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaItemModel"/> class with the specified file path.
        /// </summary>
        /// <param name="filePath">The full path to the media file.</param>
        public MediaItemModel(string filePath)
        {
            _filePath = filePath;
            _fileName = System.IO.Path.GetFileName(filePath);
            _format = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        }
    }
}
