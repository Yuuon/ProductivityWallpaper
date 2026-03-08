using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProductivityWallpaper.ViewModels;

namespace ProductivityWallpaper.Views
{
    public partial class CreatorView : System.Windows.Controls.UserControl
    {
        public CreatorView()
        {
            InitializeComponent();
        }
        
        private void ThemeNameTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is CreatorViewModel vm)
            {
                vm.FinishEditThemeNameCommand.Execute(null);
            }
        }
        
        private void ThemeNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && DataContext is CreatorViewModel vm)
            {
                vm.FinishEditThemeNameCommand.Execute(null);
            }
        }
        
        private void CreatorView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // When clicking anywhere on the page, finish editing theme name
            if (DataContext is CreatorViewModel vm && vm.IsEditingThemeName)
            {
                // Check if the click is not on the TextBox itself
                if (e.OriginalSource != ThemeNameTextBox && 
                    !IsChildOf(e.OriginalSource as DependencyObject, ThemeNameTextBox))
                {
                    vm.FinishEditThemeNameCommand.Execute(null);
                }
            }
        }
        
        private bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            if (child == null) return false;
            if (child == parent) return true;
            
            DependencyObject current = child;
            while (current != null)
            {
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                if (current == parent) return true;
            }
            return false;
        }
    }
}
