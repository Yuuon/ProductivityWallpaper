using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProductivityWallpaper.Converters
{
    // 将布尔值转换为颜色 (Brush)
    // 用于 CheckBox：可用时显示白色，禁用时显示灰色
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled && isEnabled)
            {
                return System.Drawing.Brushes.White; // 可用颜色
            }
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100)); // 禁用颜色 (深灰)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}