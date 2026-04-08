using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Unified view for all media configuration features (Desktop Background, Shutdown,
    /// Boot/Restart, Screen Wake). Binds to any MediaConfigurationViewModel subclass.
    /// </summary>
    public partial class MediaConfigurationView : System.Windows.Controls.UserControl
    {
        public MediaConfigurationView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the LostFocus event for the scheme name TextBox.
        /// </summary>
        private void SchemeNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MediaConfigurationViewModel vm)
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
                if (DataContext is ViewModels.MediaConfigurationViewModel vm)
                {
                    vm.FinishEditNameCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Handles MediaEnded event for GIF/video thumbnail MediaElements.
        /// Loops the media by resetting position to the beginning.
        /// </summary>
        private void OnThumbnailMediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is MediaElement mediaElement)
            {
                mediaElement.Position = System.TimeSpan.FromMilliseconds(1);
                mediaElement.Play();
            }
        }
    }
}
