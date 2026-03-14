using System;
using System.Globalization;
using System.Windows.Data;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts MediaFileType to Visibility based on whether it's an image.
    /// </summary>
    public class MediaTypeIsImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MediaFileType mediaType)
                return System.Windows.Visibility.Collapsed;

            var isImage = mediaType == MediaFileType.Image;

            // Check for invert parameter
            if (parameter?.ToString()?.ToLowerInvariant() == "invert")
                isImage = !isImage;

            return isImage ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
