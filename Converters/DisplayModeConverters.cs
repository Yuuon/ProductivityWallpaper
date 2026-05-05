using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts a <see cref="MediaFileType"/> into the list of valid <see cref="DisplayMode"/>
    /// values for that type. Images get Fill/Center/Tile; video and audio get Fill/Center only
    /// (Tile is image-only by product spec).
    /// </summary>
    public class DisplayModeOptionsConverter : IValueConverter
    {
        private static readonly DisplayMode[] _imageOptions = new[]
        {
            DisplayMode.Fill, DisplayMode.Center, DisplayMode.Tile
        };

        private static readonly DisplayMode[] _nonImageOptions = new[]
        {
            DisplayMode.Fill, DisplayMode.Center
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaFileType type && type == MediaFileType.Image)
                return _imageOptions;
            return _nonImageOptions;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts DisplayMode enum to Stretch enum for image display.
    /// </summary>
    public class DisplayModeToStretchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DisplayMode displayMode)
            {
                return displayMode switch
                {
                    DisplayMode.Fill => Stretch.UniformToFill,
                    DisplayMode.Center => Stretch.Uniform,
                    DisplayMode.Tile => Stretch.None,
                    _ => Stretch.UniformToFill
                };
            }
            return Stretch.UniformToFill;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts null value to Visibility (Visible when not null, Collapsed when null).
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts null value to inverse Visibility (Collapsed when not null, Visible when null).
    /// </summary>
    public class InverseNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a normalized value (0–1) to pixel value based on canvas size.
    /// Expects normalized (0–1) and converts using converter parameter as total size.
    /// </summary>
    public class PercentageToPixelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double normalized)
            {
                // The actual canvas size should be passed as parameter
                // For now, use a default scale (the canvas will resize via Viewbox)
                const double DefaultCanvasWidth = 1920;
                
                var totalSize = DefaultCanvasWidth; // Default to width
                
                // If parameter is provided, use it
                if (parameter is double size)
                {
                    totalSize = size;
                }
                else if (parameter is FrameworkElement element)
                {
                    totalSize = element.ActualWidth > 0 ? element.ActualWidth : DefaultCanvasWidth;
                }
                
                return normalized * totalSize;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a normalized value (0–1) to a pixel value using a MultiBinding.
    /// values[0]: normalized (0–1) from the model (X, Y, Width, or Height)
    /// values[1]: total size in pixels from the Canvas (ActualWidth or ActualHeight)
    /// </summary>
    public class PercentageToPixelMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2
                && values[0] is double normalized
                && values[1] is double totalSize
                && totalSize > 0)
            {
                return normalized * totalSize;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
