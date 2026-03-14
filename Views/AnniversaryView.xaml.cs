using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.ViewModels;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Interaction logic for AnniversaryView.xaml
    /// </summary>
    public partial class AnniversaryView : System.Windows.Controls.UserControl
    {
        public AnniversaryView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the LostFocus event for the scheme name text box.
        /// </summary>
        private void SchemeNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && DataContext is AnniversaryViewModel viewModel)
            {
                viewModel.FinishEditNameCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handles the KeyDown event for the scheme name text box.
        /// Finishes editing when Enter key is pressed.
        /// </summary>
        private void SchemeNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && e.Key == Key.Enter)
            {
                // Move focus to remove it from the text box
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);
                Keyboard.ClearFocus();

                if (DataContext is AnniversaryViewModel viewModel)
                {
                    viewModel.FinishEditNameCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Handles the LostFocus event for the event name text box.
        /// </summary>
        private void EventNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && textBox.Tag is AnniversaryEventModel anniversary)
            {
                if (DataContext is AnniversaryViewModel viewModel)
                {
                    viewModel.FinishEditNameCommand.Execute(anniversary);
                }
            }
        }

        /// <summary>
        /// Handles the KeyDown event for the event name text box.
        /// Finishes editing when Enter key is pressed.
        /// </summary>
        private void EventNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && e.Key == Key.Enter)
            {
                // Move focus to remove it from the text box
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);
                Keyboard.ClearFocus();

                if (textBox.Tag is AnniversaryEventModel anniversary && DataContext is AnniversaryViewModel viewModel)
                {
                    viewModel.FinishEditNameCommand.Execute(anniversary);
                }
            }
        }
    }
}
