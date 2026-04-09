using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Window for displaying a static image as a desktop wallpaper in WorkerW.
    /// Mirrors the pattern of VideoPlayerWindow but for image content.
    /// </summary>
    public partial class ImagePlayerWindow : Window
    {
        public ImagePlayerWindow(string imagePath)
        {
            InitializeComponent();
            LoadImage(imagePath);
        }

        /// <summary>
        /// Loads the specified image file into the display.
        /// </summary>
        private void LoadImage(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[ImagePlayerWindow] Image file not found: {imagePath}");
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Thread-safe

                WallpaperImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImagePlayerWindow] Failed to load image '{imagePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the image stretch mode based on the display mode setting.
        /// </summary>
        public void SetStretchMode(Stretch stretch)
        {
            WallpaperImage.Stretch = stretch;
        }

        /// <summary>
        /// Closes the window and releases resources.
        /// </summary>
        public void StopAndClose()
        {
            WallpaperImage.Source = null;
            this.Close();
        }
    }
}
