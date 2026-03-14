using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents a clickable region on the desktop with associated media content.
    /// </summary>
    public partial class ClickRegionModel : ObservableObject
    {
        // --- Private Fields ---

        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private double _x;  // Percentage 0-100

        [ObservableProperty]
        private double _y;  // Percentage 0-100

        [ObservableProperty]
        private double _width;  // Percentage 0-100

        [ObservableProperty]
        private double _height;  // Percentage 0-100

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private MediaItemModel? _visualContent;

        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _audioContent = new();

        // --- Validation ---

        /// <summary>
        /// Validates the region position and size.
        /// </summary>
        public bool IsValid()
        {
            return X >= 0 && X <= 100 &&
                   Y >= 0 && Y <= 100 &&
                   Width > 0 && Height > 0 &&
                   X + Width <= 100 &&
                   Y + Height <= 100 &&
                   AudioContent.Count <= 5;
        }

        /// <summary>
        /// Gets the validation error message if invalid.
        /// </summary>
        public string? GetValidationError()
        {
            if (X < 0 || X > 100) return "X position must be between 0 and 100";
            if (Y < 0 || Y > 100) return "Y position must be between 0 and 100";
            if (Width <= 0) return "Width must be greater than 0";
            if (Height <= 0) return "Height must be greater than 0";
            if (X + Width > 100) return "Region exceeds canvas right boundary";
            if (Y + Height > 100) return "Region exceeds canvas bottom boundary";
            if (AudioContent.Count > 5) return "Maximum 5 audio files allowed";
            return null;
        }

        // --- Helper Methods ---

        /// <summary>
        /// Checks if a point (in percentage coordinates) is contained within this region.
        /// </summary>
        /// <param name="x">X coordinate as percentage (0-100).</param>
        /// <param name="y">Y coordinate as percentage (0-100).</param>
        /// <returns>True if the point is inside the region.</returns>
        public bool ContainsPoint(double x, double y)
        {
            return x >= X && x <= X + Width &&
                   y >= Y && y <= Y + Height;
        }

        /// <summary>
        /// Converts percentage-based region to absolute pixel coordinates.
        /// </summary>
        /// <param name="canvasWidth">The actual canvas width in pixels.</param>
        /// <param name="canvasHeight">The actual canvas height in pixels.</param>
        /// <returns>A Rect with absolute pixel values.</returns>
        public Rect ToAbsoluteRect(double canvasWidth, double canvasHeight)
        {
            return new Rect(
                X / 100.0 * canvasWidth,
                Y / 100.0 * canvasHeight,
                Width / 100.0 * canvasWidth,
                Height / 100.0 * canvasHeight
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
        /// <returns>A new ClickRegionModel with percentage values.</returns>
        public static ClickRegionModel FromAbsoluteRect(
            double left, double top, double width, double height,
            double canvasWidth, double canvasHeight)
        {
            return new ClickRegionModel
            {
                X = left / canvasWidth * 100,
                Y = top / canvasHeight * 100,
                Width = width / canvasWidth * 100,
                Height = height / canvasHeight * 100
            };
        }
    }
}
