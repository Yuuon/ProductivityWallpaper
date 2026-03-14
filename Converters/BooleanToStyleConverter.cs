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

    /// <summary>
    /// Converts boolean to GridLength star value.
    /// When true, returns the star value from parameter (e.g., "6" becomes 6*).
    /// When false, returns 0 (collapsed width).
    /// </summary>
    public class BooleanToStarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Parse the parameter as a double and return star length
                if (parameter != null && double.TryParse(parameter.ToString(), out double starValue))
                {
                    return new GridLength(starValue, GridUnitType.Star);
                }
                return new GridLength(1, GridUnitType.Star);
            }
            // When false, return 0 width (collapsed)
            return new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to Thickness for left margin.
    /// When true, returns the thickness from parameter.
    /// When false, returns 0 (no margin).
    /// </summary>
    public class BooleanToLeftMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Parse the parameter as a thickness string (e.g., "16,0,0,0")
                if (parameter != null)
                {
                    try
                    {
                        var converter = new ThicknessConverter();
                        return converter.ConvertFromString(parameter.ToString());
                    }
                    catch
                    {
                        return new Thickness(16, 0, 0, 0);
                    }
                }
                return new Thickness(16, 0, 0, 0);
            }
            // When false, return 0 margin (no left margin when full width)
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
