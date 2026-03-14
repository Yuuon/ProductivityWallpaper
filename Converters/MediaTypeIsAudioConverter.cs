using System;
using System.Globalization;
using System.Windows.Data;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts MediaFileType to Visibility based on whether it's audio.
    /// </summary>
    public class MediaTypeIsAudioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MediaFileType mediaType)
                return System.Windows.Visibility.Collapsed;

            var isAudio = mediaType == MediaFileType.Audio;

            // Check for invert parameter
            if (parameter?.ToString()?.ToLowerInvariant() == "invert")
                isAudio = !isAudio;

            return isAudio ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
