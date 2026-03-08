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
            // Active state uses gradient background
            if (value is bool boolValue && boolValue)
            {
                return Application.Current.Resources["PrimaryButtonStyle"];
            }
            // Default state uses transparent background
            return new Style(typeof(Button))
            {
                Setters = 
                {
                    new Setter(Button.BackgroundProperty, Application.Current.Resources["BackgroundBlockBrush"]),
                    new Setter(Button.ForegroundProperty, Application.Current.Resources["TextPrimaryBrush"]),
                    new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                    new Setter(Button.PaddingProperty, new Thickness(16, 8, 16, 8)),
                    new Setter(Button.HeightProperty, 40.0),
                    new Setter(Button.HorizontalAlignmentProperty, HorizontalAlignment.Stretch)
                }
            };
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
