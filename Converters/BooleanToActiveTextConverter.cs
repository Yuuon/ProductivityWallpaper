using System;
using System.Globalization;
using System.Windows.Data;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts boolean IsActive to button text ("Activate This Scheme" or "Current Active Scheme").
    /// </summary>
    public class BooleanToActiveTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
                return "Current Active Scheme";
            return "Activate This Scheme";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
