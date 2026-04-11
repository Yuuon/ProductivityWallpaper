using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingImage = System.Drawing.Image;
using DrawingBitmap = System.Drawing.Bitmap;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Window for displaying a static image as a desktop wallpaper in WorkerW.
    /// 
    /// Dual rendering strategy:
    /// 1. WPF Image control — works in normal mode but invisible inside WorkerW (DWM issue).
    /// 2. GDI native painting — paints the image directly on the Win32 window surface via
    ///    System.Drawing, bypassing WPF's DirectX/DWM pipeline. This is the primary method
    ///    that actually works inside WorkerW.
    ///
    /// The GDI painting is activated by WallpaperService after injection into WorkerW.
    /// </summary>
    public partial class ImagePlayerWindow : Window
    {
        private DrawingBitmap? _gdiBitmap;
        private readonly string _imagePath;

        public ImagePlayerWindow(string imagePath)
        {
            InitializeComponent();
            _imagePath = imagePath;
            LoadImage(imagePath);
        }

        /// <summary>
        /// The file path of the image loaded in this window.
        /// </summary>
        public string ImagePath => _imagePath;

        /// <summary>
        /// Gets the GDI bitmap for native painting inside WorkerW.
        /// Loads it lazily on first access to avoid System.Drawing dependency
        /// when the window is used outside WorkerW context.
        /// </summary>
        public DrawingBitmap? GetGdiBitmap()
        {
            if (_gdiBitmap != null) return _gdiBitmap;

            try
            {
                if (File.Exists(_imagePath))
                {
                    _gdiBitmap = new DrawingBitmap(_imagePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImagePlayerWindow] Failed to load GDI bitmap: {ex.Message}");
            }
            return _gdiBitmap;
        }

        /// <summary>
        /// Loads the specified image file into the WPF display.
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
        /// Closes the window and releases resources including the GDI bitmap.
        /// </summary>
        public void StopAndClose()
        {
            WallpaperImage.Source = null;
            try { _gdiBitmap?.Dispose(); } catch { }
            _gdiBitmap = null;
            this.Close();
        }
    }
}
