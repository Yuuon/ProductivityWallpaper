using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Interaction logic for ScreenWakeView.xaml
    /// </summary>
    public partial class ScreenWakeView : System.Windows.Controls.UserControl
    {
        public ScreenWakeView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the LostFocus event for the scheme name TextBox.
        /// </summary>
        private void SchemeNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.ScreenWakeViewModel vm)
            {
                vm.FinishEditNameCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handles the KeyDown event for the scheme name TextBox.
        /// Finishes editing when Enter key is pressed.
        /// </summary>
        private void SchemeNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ViewModels.ScreenWakeViewModel vm)
                {
                    vm.FinishEditNameCommand.Execute(null);
                }
            }
        }
    }
}
