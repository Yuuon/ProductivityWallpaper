using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts a string file path to an ImageSource.
    /// Returns UnsetValue for null, empty, or invalid paths to allow fallback.
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    // Freeze for performance
                    if (bitmap.CanFreeze)
                    {
                        bitmap.Freeze();
                    }
                    
                    return bitmap;
                }
                catch
                {
                    // Invalid path - return UnsetValue to use fallback
                    return DependencyProperty.UnsetValue;
                }
            }
            
            // Null or empty - return UnsetValue to use fallback
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
