using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductivityWallpaper.ViewModels;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Interaction logic for DesktopBackgroundView.xaml
    /// </summary>
    public partial class DesktopBackgroundView : System.Windows.Controls.UserControl
    {
        public DesktopBackgroundView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the LostFocus event for the scheme name TextBox.
        /// Finishes editing when the text box loses focus.
        /// </summary>
        private void SchemeNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is DesktopBackgroundViewModel viewModel)
            {
                viewModel.FinishEditNameCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handles the KeyDown event for the scheme name TextBox.
        /// Finishes editing when Enter is pressed.
        /// </summary>
        private void SchemeNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is DesktopBackgroundViewModel viewModel)
            {
                viewModel.FinishEditNameCommand.Execute(null);
            }
        }
    }
}
