using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the type of media resource in a theme package.
    /// </summary>
    public enum MediaType
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
    /// Represents a media resource entry in a theme package with pre-computed metadata.
    /// Resources are stored once per theme and referenced by ID from schemes.
    /// </summary>
    public partial class ResourceEntry : ObservableObject
    {
        /// <summary>
        /// Unique identifier for this resource (UUID).
        /// </summary>
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Filename in the type-specific folder (e.g., "background1.mp4").
        /// </summary>
        [ObservableProperty]
        private string _fileName = string.Empty;

        /// <summary>
        /// Type of media resource.
        /// </summary>
        [ObservableProperty]
        private MediaType _type;

        /// <summary>
        /// File format/extension (e.g., ".mp4", ".jpg").
        /// </summary>
        [ObservableProperty]
        private string _format = string.Empty;

        /// <summary>
        /// File size in bytes.
        /// </summary>
        [ObservableProperty]
        private long _fileSize;

        /// <summary>
        /// Duration for video/audio files. Null for images.
        /// </summary>
        [ObservableProperty]
        private TimeSpan? _duration;

        /// <summary>
        /// Width in pixels for image/video files. Null for audio.
        /// </summary>
        [ObservableProperty]
        private int? _width;

        /// <summary>
        /// Height in pixels for image/video files. Null for audio.
        /// </summary>
        [ObservableProperty]
        private int? _height;

        /// <summary>
        /// Thumbnail filename in the /thumbnails/ folder.
        /// </summary>
        [ObservableProperty]
        private string? _thumbnailFileName;
    }
}
