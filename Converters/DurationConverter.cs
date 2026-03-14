using System;
using System.Globalization;
using System.Windows.Data;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts a TimeSpan to a display string format (M:SS or H:MM:SS).
    /// </summary>
    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TimeSpan duration)
                return string.Empty;

            if (duration.Hours > 0)
                return $"{duration.Hours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            return $"{duration.Minutes}:{duration.Seconds:D2}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
