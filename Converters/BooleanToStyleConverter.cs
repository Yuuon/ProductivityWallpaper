using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace ProductivityWallpaper.Converters
{
    public class BooleanToTabStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return Application.Current.Resources["ActiveTabButtonStyle"];
            }
            return Application.Current.Resources["TabButtonStyle"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BooleanToFeatureButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return a style for feature buttons
            // Both states use the same CornerRadius (10) for consistent shape
            if (value is bool boolValue && boolValue)
            {
                return Application.Current.Resources["ActiveFeatureButtonStyle"];
            }
            return Application.Current.Resources["FeatureButtonStyle"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
