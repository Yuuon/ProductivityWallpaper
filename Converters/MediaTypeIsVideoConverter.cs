using System;
using System.Globalization;
using System.Windows.Data;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts MediaFileType to Visibility based on whether it's a video.
    /// </summary>
    public class MediaTypeIsVideoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MediaFileType mediaType)
                return System.Windows.Visibility.Collapsed;

            var isVideo = mediaType == MediaFileType.Video;

            // Check for invert parameter
            if (parameter?.ToString()?.ToLowerInvariant() == "invert")
                isVideo = !isVideo;

            return isVideo ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
