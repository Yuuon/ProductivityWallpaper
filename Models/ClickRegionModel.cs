using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents a clickable region on the desktop with associated media content.
    /// All position and size values are stored as normalized 0–1 values
    /// (0 = left/top edge, 1 = right/bottom edge of the screen).
    /// This ensures theme packs are resolution-independent and portable.
    /// </summary>
    public partial class ClickRegionModel : ObservableObject
    {
        // --- Private Fields ---

        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private double _x;  // Normalized 0–1 (0 = left edge)

        [ObservableProperty]
        private double _y;  // Normalized 0–1 (0 = top edge)

        [ObservableProperty]
        private double _width;  // Normalized 0–1 (fraction of screen width)

        [ObservableProperty]
        private double _height;  // Normalized 0–1 (fraction of screen height)

        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// The click action for this region (visual + audio IDs).
        /// Uses ID-based references to ThemeResourceLibrary.
        /// </summary>
        [ObservableProperty]
        private ClickAction _clickAction = new();

        // Legacy properties kept for backward compatibility
        [ObservableProperty]
        private MediaItemModel? _visualContent;

        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _audioContent = new();

        /// <summary>
        /// Playback mode for audio files in this region (Sequential or Random).
        /// </summary>
        [ObservableProperty]
        private PlaybackMode _audioPlaybackMode = PlaybackMode.Sequential;

        // --- Validation ---

        /// <summary>
        /// Validates the region position and size (normalized 0–1).
        /// </summary>
        public bool IsValid()
        {
            return X >= 0 && X <= 1 &&
                   Y >= 0 && Y <= 1 &&
                   Width > 0 && Height > 0 &&
                   X + Width <= 1.001 &&   // small epsilon for floating-point
                   Y + Height <= 1.001 &&
                   AudioContent.Count <= 5;
        }

        /// <summary>
        /// Gets the validation error message if invalid.
        /// </summary>
        public string? GetValidationError()
        {
            if (X < 0 || X > 1) return "X position must be between 0 and 1";
            if (Y < 0 || Y > 1) return "Y position must be between 0 and 1";
            if (Width <= 0) return "Width must be greater than 0";
            if (Height <= 0) return "Height must be greater than 0";
            if (X + Width > 1.001) return "Region exceeds canvas right boundary";
            if (Y + Height > 1.001) return "Region exceeds canvas bottom boundary";
            if (AudioContent.Count > 5) return "Maximum 5 audio files allowed";
            return null;
        }

        // --- Helper Methods ---

        /// <summary>
        /// Checks if a point (in normalized 0–1 coordinates) is contained within this region.
        /// </summary>
        /// <param name="x">X coordinate normalized 0–1.</param>
        /// <param name="y">Y coordinate normalized 0–1.</param>
        /// <returns>True if the point is inside the region.</returns>
        public bool ContainsPoint(double x, double y)
        {
            return x >= X && x <= X + Width &&
                   y >= Y && y <= Y + Height;
        }

        /// <summary>
        /// Converts normalized region to absolute pixel coordinates.
        /// </summary>
        /// <param name="canvasWidth">The actual canvas width in pixels.</param>
        /// <param name="canvasHeight">The actual canvas height in pixels.</param>
        /// <returns>A Rect with absolute pixel values.</returns>
        public Rect ToAbsoluteRect(double canvasWidth, double canvasHeight)
        {
            return new Rect(
                X * canvasWidth,
                Y * canvasHeight,
                Width * canvasWidth,
                Height * canvasHeight
            );
        }

        /// <summary>
        /// Creates a region from absolute pixel coordinates.
        /// </summary>
        /// <param name="left">Left position in pixels.</param>
        /// <param name="top">Top position in pixels.</param>
        /// <param name="width">Width in pixels.</param>
        /// <param name="height">Height in pixels.</param>
        /// <param name="canvasWidth">The canvas width in pixels.</param>
        /// <param name="canvasHeight">The canvas height in pixels.</param>
        /// <returns>A new ClickRegionModel with normalized 0–1 values.</returns>
        public static ClickRegionModel FromAbsoluteRect(
            double left, double top, double width, double height,
            double canvasWidth, double canvasHeight)
        {
            return new ClickRegionModel
            {
                X = left / canvasWidth,
                Y = top / canvasHeight,
                Width = width / canvasWidth,
                Height = height / canvasHeight
            };
        }
    }
}
