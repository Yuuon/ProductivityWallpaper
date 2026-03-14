using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts a boolean value to a cursor type (Cross when true, Arrow when false).
    /// </summary>
    public class BooleanToCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return System.Windows.Input.Cursors.Cross;
            }
            return System.Windows.Input.Cursors.Arrow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
