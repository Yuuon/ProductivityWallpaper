using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Interaction logic for PomodoroView.xaml
    /// </summary>
    public partial class PomodoroView : System.Windows.Controls.UserControl
    {
        public PomodoroView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles click on a style card to toggle its activation.
        /// </summary>
        private void OnStyleCardClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is PomodoroStyleModel style)
            {
                var viewModel = DataContext as ViewModels.PomodoroViewModel;
                viewModel?.ToggleStyleActivationCommand.Execute(style);
            }
        }

        /// <summary>
        /// Handles format toggle button clicks.
        /// </summary>
        private void OnFormatToggleClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggleButton && toggleButton.Tag is PomodoroStyleModel style)
            {
                var format = toggleButton.Content?.ToString() == "24h" 
                    ? ClockFormat.Hour24 
                    : ClockFormat.Hour12;
                
                // Update the format directly on the model
                style.Format = format;
            }
        }

        /// <summary>
        /// Handles lost focus on scheme name textbox to finish editing.
        /// </summary>
        private void SchemeNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox)
            {
                var viewModel = DataContext as ViewModels.PomodoroViewModel;
                viewModel?.FinishEditNameCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handles key down on scheme name textbox to finish editing on Enter.
        /// </summary>
        private void SchemeNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox)
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    var viewModel = DataContext as ViewModels.PomodoroViewModel;
                    viewModel?.FinishEditNameCommand.Execute(null);
                    
                    // Remove focus from textbox
                    Keyboard.ClearFocus();
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    // Cancel editing on Escape
                    var viewModel = DataContext as ViewModels.PomodoroViewModel;
                    viewModel?.FinishEditNameCommand.Execute(null);
                    
                    // Remove focus from textbox
                    Keyboard.ClearFocus();
                }
            }
        }
    }
}
