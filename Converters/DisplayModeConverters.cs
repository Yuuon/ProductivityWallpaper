using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Converters
{
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
    /// Converts percentage value to pixel value based on canvas size.
    /// Expects percentage (0-100) and converts using converter parameter as total size.
    /// </summary>
    public class PercentageToPixelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                // The actual canvas size should be passed as parameter
                // For now, use a default scale (the canvas will resize via Viewbox)
                const double DefaultCanvasWidth = 1920;
                const double DefaultCanvasHeight = 1080;
                
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
                
                return percentage / 100.0 * totalSize;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
