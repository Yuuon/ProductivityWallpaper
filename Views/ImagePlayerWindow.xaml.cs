using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Window for displaying a static image as a desktop wallpaper.
    /// Hosts a WPF Image element — Lively uses the same approach inside its injected wallpaper window.
    /// Earlier GDI WM_PAINT implementation was removed because paint fired once at 1x1 size and never
    /// re-rendered after the post-injection resize on Win11 raised desktop.
    /// </summary>
    public partial class ImagePlayerWindow : Window
    {
        public ImagePlayerWindow(string imagePath)
        {
            InitializeComponent();
            LoadImage(imagePath);
        }

        /// <summary>
        /// Loads the specified image into the WPF Image element.
        /// Uses BitmapCacheOption.OnLoad so the file handle is released immediately,
        /// allowing the source file to be replaced or deleted while the wallpaper runs.
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
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                WallpaperImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImagePlayerWindow] Failed to load image '{imagePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Adjusts the WPF Image stretch mode based on user display preference.
        /// </summary>
        public void SetStretchMode(Stretch stretch)
        {
            WallpaperImage.Stretch = stretch;
        }

        /// <summary>
        /// Closes the window. Image source release happens automatically when the window is collected.
        /// </summary>
        public void StopAndClose()
        {
            try
            {
                WallpaperImage.Source = null;
            }
            catch { }
            this.Close();
        }
    }
}
