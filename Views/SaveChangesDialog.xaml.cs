using System.Windows;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Result of the SaveChangesDialog interaction.
    /// </summary>
    public enum SaveChangesResult
    {
        /// <summary>
        /// User chose to save changes before leaving.
        /// </summary>
        Save,

        /// <summary>
        /// User chose to discard changes and leave.
        /// </summary>
        DontSave,

        /// <summary>
        /// User cancelled and wants to stay on the current page.
        /// </summary>
        Cancel
    }

    /// <summary>
    /// Themed confirmation dialog shown when navigating away with unsaved changes.
    /// Provides [Save], [Don't Save], and [Cancel] options.
    /// </summary>
    public partial class SaveChangesDialog : Window
    {
        /// <summary>
        /// Gets the user's choice from the dialog.
        /// </summary>
        public SaveChangesResult Result { get; private set; } = SaveChangesResult.Cancel;

        public SaveChangesDialog()
        {
            InitializeComponent();

            // Try to center on owner window
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded)
            {
                Owner = mainWindow;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SaveChangesResult.Save;
            DialogResult = true;
        }

        private void DontSaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SaveChangesResult.DontSave;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SaveChangesResult.Cancel;
            DialogResult = false;
        }
    }
}
