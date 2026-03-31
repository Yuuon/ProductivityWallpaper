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
    /// Supports two-phase workflow:
    /// - Local editing: files referenced by absolute SourcePath (no copying)
    /// - Export: files copied to theme package with relative ExportPath
    /// </summary>
    public partial class ResourceEntry : ObservableObject
    {
        /// <summary>
        /// Unique identifier for this resource (UUID).
        /// </summary>
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// CRC32 hash of the file content for deduplication (e.g., "crc32:A1B2C3D4").
        /// </summary>
        [ObservableProperty]
        private string _hash = string.Empty;

        /// <summary>
        /// Type of media resource.
        /// </summary>
        [ObservableProperty]
        private MediaType _type;

        /// <summary>
        /// Absolute path to the source file for local editing mode (e.g., "C:\Users\...\photo.jpg").
        /// This is the primary path used during theme creation and editing.
        /// </summary>
        [ObservableProperty]
        private string _sourcePath = string.Empty;

        /// <summary>
        /// Relative path within the exported theme package (e.g., "images/photo.jpg").
        /// Null until the theme is exported. Set during export when files are copied.
        /// </summary>
        [ObservableProperty]
        private string? _exportPath;

        /// <summary>
        /// Original filename of the imported file (e.g., "photo.jpg").
        /// </summary>
        [ObservableProperty]
        private string _originalName = string.Empty;

        /// <summary>
        /// Filename in the type-specific folder (e.g., "background1.mp4").
        /// Used for exported theme packages.
        /// </summary>
        [ObservableProperty]
        private string _fileName = string.Empty;

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
        /// Thumbnail filename in the /thumbnails/ folder (for exported themes).
        /// </summary>
        [ObservableProperty]
        private string? _thumbnailFileName;

        /// <summary>
        /// Path to the cached thumbnail (temp folder for local editing, export folder for packages).
        /// </summary>
        [ObservableProperty]
        private string? _thumbnailPath;

        /// <summary>
        /// Current availability status of the resource file.
        /// </summary>
        [ObservableProperty]
        private ResourceStatus _status = ResourceStatus.OK;

        /// <summary>
        /// Human-readable status message (e.g., "File not found: C:\path\to\file.jpg").
        /// </summary>
        [ObservableProperty]
        private string? _statusMessage;
    }
}
