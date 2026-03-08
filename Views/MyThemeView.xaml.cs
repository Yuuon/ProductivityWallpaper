using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductivityWallpaper.ViewModels;

namespace ProductivityWallpaper.Views
{
    public partial class MyThemeView : System.Windows.Controls.UserControl
    {
        public MyThemeView()
        {
            InitializeComponent();
        }
        
        private void OnThemeCardClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ThemeItem theme)
            {
                if (DataContext is MyThemeViewModel viewModel)
                {
                    viewModel.SelectThemeCommand.Execute(theme);
                }
            }
        }
    }
}
