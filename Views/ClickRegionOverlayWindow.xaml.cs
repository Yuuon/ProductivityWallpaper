using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProductivityWallpaper.Models;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Transparent overlay window injected into WorkerW at the topmost Z-order.
    /// Displays click regions as colored rectangles (red for debugging visibility).
    /// Provides hit-testing for mouse click detection in theme-based click regions.
    /// 
    /// This follows the same pattern as InteractiveUiWindow: a transparent Canvas-based
    /// window parented to WorkerW, sitting above the wallpaper but below desktop icons.
    /// </summary>
    public partial class ClickRegionOverlayWindow : Window
    {
        private readonly List<ClickRegionModel> _regions = new();
        private readonly Dictionary<Rectangle, ClickRegionModel> _rectRegionMap = new();

        /// <summary>
        /// Fired when a click region is hit. Passes the ClickRegionModel that was clicked.
        /// </summary>
        public event Action<ClickRegionModel>? OnRegionClicked;

        public ClickRegionOverlayWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loads click regions from the theme data and creates visible rectangles on the canvas.
        /// Regions use percentage-based coordinates (0-100) relative to screen size.
        /// </summary>
        /// <param name="regions">The click regions to display.</param>
        /// <param name="debugVisible">If true, regions are shown in semi-transparent red for debugging.</param>
        public void LoadRegions(IEnumerable<ClickRegionModel> regions, bool debugVisible = true)
        {
            OverlayCanvas.Children.Clear();
            _regions.Clear();
            _rectRegionMap.Clear();

            foreach (var region in regions)
            {
                _regions.Add(region);

                var rect = new Rectangle
                {
                    // Debug mode: semi-transparent red so regions are visually obvious
                    // Production mode: fully transparent (invisible) but still hit-testable
                    Fill = debugVisible
                        ? new SolidColorBrush(Color.FromArgb(100, 255, 0, 0))  // Semi-transparent red
                        : System.Windows.Media.Brushes.Transparent,
                    Stroke = debugVisible
                        ? System.Windows.Media.Brushes.Red
                        : null,
                    StrokeThickness = debugVisible ? 2 : 0,
                    Tag = region,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    IsHitTestVisible = true
                };

                _rectRegionMap[rect] = region;
                OverlayCanvas.Children.Add(rect);
            }
        }

        /// <summary>
        /// Updates the layout of all click region rectangles based on the current screen/canvas size.
        /// Must be called after the window is injected and maximized in WorkerW.
        /// </summary>
        /// <param name="screenWidth">The actual screen width in pixels.</param>
        /// <param name="screenHeight">The actual screen height in pixels.</param>
        public void UpdateRegionLayout(double screenWidth, double screenHeight)
        {
            foreach (var child in OverlayCanvas.Children)
            {
                if (child is Rectangle rect && rect.Tag is ClickRegionModel region)
                {
                    // Convert percentage (0-100) to absolute pixel coordinates
                    double x = region.X / 100.0 * screenWidth;
                    double y = region.Y / 100.0 * screenHeight;
                    double w = region.Width / 100.0 * screenWidth;
                    double h = region.Height / 100.0 * screenHeight;

                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                    rect.Width = w;
                    rect.Height = h;
                }
            }
        }

        /// <summary>
        /// Checks if a screen point falls within any click region and fires the event.
        /// Returns the matched region, or null if no region was hit.
        /// </summary>
        /// <param name="screenPoint">The click point in screen coordinates.</param>
        /// <returns>The ClickRegionModel that was hit, or null.</returns>
        public ClickRegionModel? HitTest(Point screenPoint)
        {
            if (_regions.Count == 0) return null;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            // Convert screen point to percentage (0-100)
            double xPct = screenPoint.X / screenW * 100.0;
            double yPct = screenPoint.Y / screenH * 100.0;

            foreach (var region in _regions)
            {
                if (region.ContainsPoint(xPct, yPct))
                {
                    return region;
                }
            }

            return null;
        }

        /// <summary>
        /// Performs a hit test and fires the OnRegionClicked event if a region was hit.
        /// Returns true if a click was handled.
        /// </summary>
        public bool HandleClick(Point screenPoint)
        {
            var region = HitTest(screenPoint);
            if (region != null)
            {
                OnRegionClicked?.Invoke(region);
                return true;
            }
            return false;
        }
    }
}
