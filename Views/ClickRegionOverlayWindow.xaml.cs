using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Transparent overlay window injected into WorkerW at the topmost Z-order.
    /// Displays click regions as invisible hit-testable rectangles (or colored for debug).
    /// Provides hit-testing for mouse click detection in theme-based click regions.
    /// 
    /// This follows the same pattern as InteractiveUiWindow: a transparent Canvas-based
    /// window parented to WorkerW, sitting above the wallpaper but below desktop icons.
    /// </summary>
    public partial class ClickRegionOverlayWindow : Window
    {
        private readonly List<ClickRegionModel> _regions = new();
        private readonly Dictionary<Rectangle, ClickRegionModel> _rectRegionMap = new();
        private bool _debugVisible;

        /// <summary>
        /// Fired when a click region is hit. Passes the ClickRegionModel that was clicked.
        /// </summary>
        public event Action<ClickRegionModel>? OnRegionClicked;

        public ClickRegionOverlayWindow()
        {
            InitializeComponent();
            // Auto-recalculate layout when canvas resizes (after injection into WorkerW, maximize, etc.)
            OverlayCanvas.SizeChanged += (_, _) => UpdateRegionLayout();
        }

        /// <summary>
        /// Loads click regions from the theme data and creates rectangles on the canvas.
        /// Regions use normalized coordinates (0–1) relative to screen size.
        /// </summary>
        /// <param name="regions">The click regions to display.</param>
        /// <param name="debugVisible">If true, regions are shown in semi-transparent red for debugging.</param>
        public void LoadRegions(IEnumerable<ClickRegionModel> regions, bool debugVisible = false)
        {
            OverlayCanvas.Children.Clear();
            _regions.Clear();
            _rectRegionMap.Clear();
            _debugVisible = debugVisible;

            foreach (var region in regions)
            {
                _regions.Add(region);

                var rect = new Rectangle
                {
                    // Debug mode: semi-transparent red so regions are visually obvious
                    // Production mode: fully transparent (invisible) but still hit-testable
                    Fill = debugVisible
                        ? new SolidColorBrush(Color.FromArgb(100, 255, 0, 0))  // Semi-transparent red
                        : Brushes.Transparent,
                    Stroke = debugVisible
                        ? Brushes.Red
                        : null,
                    StrokeThickness = debugVisible ? 2 : 0,
                    Tag = region,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    IsHitTestVisible = true
                };

                _rectRegionMap[rect] = region;
                OverlayCanvas.Children.Add(rect);
            }

            // If the canvas already has valid dimensions, update layout immediately
            UpdateRegionLayout();
        }

        /// <summary>
        /// Updates the layout of all click region rectangles based on the canvas's actual size.
        /// Automatically called when the canvas resizes (after injection into WorkerW).
        /// Uses the canvas's ActualWidth/ActualHeight so it's DPI-safe — WPF handles 
        /// the logical-to-physical pixel mapping automatically.
        /// Region model stores normalized 0–1 values; multiply by canvas size to get pixels.
        /// </summary>
        public void UpdateRegionLayout()
        {
            double canvasW = OverlayCanvas.ActualWidth;
            double canvasH = OverlayCanvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return;

            foreach (var child in OverlayCanvas.Children)
            {
                if (child is Rectangle rect && rect.Tag is ClickRegionModel region)
                {
                    // Convert normalized (0–1) to absolute pixel coordinates
                    // relative to the canvas's actual dimensions
                    double x = region.X * canvasW;
                    double y = region.Y * canvasH;
                    double w = region.Width * canvasW;
                    double h = region.Height * canvasH;

                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                    rect.Width = Math.Max(1, w);
                    rect.Height = Math.Max(1, h);
                }
            }
        }

        /// <summary>
        /// Checks if a screen point falls within any click region.
        /// Returns the matched region, or null if no region was hit.
        /// </summary>
        /// <param name="screenPoint">The click point in screen coordinates.</param>
        /// <returns>The ClickRegionModel that was hit, or null.</returns>
        public ClickRegionModel? HitTest(Point screenPoint)
        {
            if (_regions.Count == 0) return null;

            // Use physical screen resolution for coordinate conversion since
            // mouse hook coordinates are in physical screen pixels
            int physicalW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int physicalH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);
            if (physicalW <= 0 || physicalH <= 0) return null;

            // Convert physical screen point to normalized (0–1)
            double xNorm = screenPoint.X / physicalW;
            double yNorm = screenPoint.Y / physicalH;

            foreach (var region in _regions)
            {
                if (region.ContainsPoint(xNorm, yNorm))
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
