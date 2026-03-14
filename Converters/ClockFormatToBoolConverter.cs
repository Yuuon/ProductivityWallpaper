using System;
using System.Globalization;
using System.Windows.Data;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts a ClockFormat enum value to a boolean based on the converter parameter.
    /// Used for binding format toggle buttons (12h/24h).
    /// </summary>
    public class ClockFormatToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts a ClockFormat value to a boolean.
        /// </summary>
        /// <param name="value">The ClockFormat value.</param>
        /// <param name="targetType">The target type (bool).</param>
        /// <param name="parameter">The format to compare against ("Hour12" or "Hour24").</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>True if the value matches the parameter; otherwise, false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ClockFormat format && parameter is string formatString)
            {
                if (Enum.TryParse<ClockFormat>(formatString, out var targetFormat))
                {
                    return format == targetFormat;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts a boolean back to a ClockFormat value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <param name="targetType">The target type (ClockFormat).</param>
        /// <param name="parameter">The format to return when true ("Hour12" or "Hour24").</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>The ClockFormat value if true; otherwise, the current value unchanged.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string formatString)
            {
                if (Enum.TryParse<ClockFormat>(formatString, out var targetFormat))
                {
                    return targetFormat;
                }
            }
            return value;
        }
    }
}
