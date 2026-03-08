using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductivityWallpaper.ViewModels;

namespace ProductivityWallpaper.Views
{
    public partial class WorkshopView : System.Windows.Controls.UserControl
    {
        public WorkshopView()
        {
            InitializeComponent();
        }
        
        private void OnThemeCardClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ThemeItem theme)
            {
                if (DataContext is WorkshopViewModel viewModel)
                {
                    viewModel.SelectThemeCommand.Execute(theme);
                }
            }
        }
        
        private void OnTagChecked(object sender, RoutedEventArgs e)
        {
            // Handle tag checked
        }
        
        private void OnTagUnchecked(object sender, RoutedEventArgs e)
        {
            // Handle tag unchecked
        }
    }
}
