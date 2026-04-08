using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.ViewModels;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Interaction logic for MouseClickView.xaml - handles canvas drawing and region interaction.
    /// </summary>
    public partial class MouseClickView : System.Windows.Controls.UserControl
    {
        // --- Drawing State ---
        private Point _dragStartPoint;
        private bool _isDragging;
        private Rectangle? _dragPreviewRect;
        private Canvas? _regionCanvas;

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseClickView"/> class.
        /// </summary>
        public MouseClickView()
        {
            InitializeComponent();
        }

        #region Scheme Name Editing

        /// <summary>
        /// Handles the LostFocus event for the scheme name text box.
        /// </summary>
        private void OnSchemeNameLostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is MouseClickViewModel vm)
            {
                vm.IsEditingName = false;
            }
        }

        /// <summary>
        /// Handles the KeyDown event for the scheme name text box.
        /// Exits edit mode on Enter or Escape.
        /// </summary>
        private void OnSchemeNameKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not MouseClickViewModel vm) return;

            if (e.Key == Key.Enter)
            {
                vm.IsEditingName = false;
            }
            else if (e.Key == Key.Escape)
            {
                vm.IsEditingName = false;
            }
        }

        #endregion

        #region Canvas Size Management

        /// <summary>
        /// Handles the SizeChanged event for the canvas container.
        /// Maintains aspect ratio of the canvas.
        /// </summary>
        private void OnCanvasContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not Border container) return;
            if (DataContext is not MouseClickViewModel vm) return;

            // Get the available size
            var availableWidth = container.ActualWidth - 32; // Margin
            var availableHeight = container.ActualHeight - 32; // Margin

            if (availableWidth <= 0 || availableHeight <= 0) return;

            // Calculate size maintaining aspect ratio
            var aspectRatio = vm.CanvasAspectRatio;
            var targetWidth = availableWidth;
            var targetHeight = targetWidth / aspectRatio;

            if (targetHeight > availableHeight)
            {
                targetHeight = availableHeight;
                targetWidth = targetHeight * aspectRatio;
            }

            // Apply to canvas container
            // Note: CanvasContainer is defined in XAML with x:Name
            var canvasContainer = FindName("CanvasContainer") as FrameworkElement;
            if (canvasContainer != null)
            {
                canvasContainer.Width = targetWidth;
                canvasContainer.Height = targetHeight;
            }
        }

        #endregion

        #region Canvas Drawing

        /// <summary>
        /// Handles MouseLeftButtonDown on the canvas.
        /// Starts drawing a new region if in adding mode, or selects existing region.
        /// </summary>
        private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MouseClickViewModel vm) return;

            _regionCanvas = sender as Canvas;
            if (_regionCanvas == null) return;

            var position = e.GetPosition(_regionCanvas);

            if (vm.IsAddingMode)
            {
                // Start drawing new region
                _dragStartPoint = position;
                _isDragging = true;
                _regionCanvas.CaptureMouse();

                // Create preview rectangle
                _dragPreviewRect = new Rectangle
                {
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    Fill = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)),
                    Width = 0,
                    Height = 0
                };

                Canvas.SetLeft(_dragPreviewRect, position.X);
                Canvas.SetTop(_dragPreviewRect, position.Y);
                _regionCanvas.Children.Add(_dragPreviewRect);
            }
            else
            {
                // Hit test for region selection
                HandleRegionSelection(position);
            }
        }

        /// <summary>
        /// Handles MouseMove on the canvas.
        /// Updates preview rectangle during drag operation.
        /// </summary>
        private void OnCanvasMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging || _regionCanvas == null || _dragPreviewRect == null) return;

            var currentPosition = e.GetPosition(_regionCanvas);

            // Calculate rectangle dimensions
            var left = Math.Min(_dragStartPoint.X, currentPosition.X);
            var top = Math.Min(_dragStartPoint.Y, currentPosition.Y);
            var width = Math.Abs(currentPosition.X - _dragStartPoint.X);
            var height = Math.Abs(currentPosition.Y - _dragStartPoint.Y);

            // Clamp to canvas bounds
            left = Math.Max(0, left);
            top = Math.Max(0, top);
            width = Math.Min(_regionCanvas.ActualWidth - left, width);
            height = Math.Min(_regionCanvas.ActualHeight - top, height);

            // Update preview rectangle
            Canvas.SetLeft(_dragPreviewRect, left);
            Canvas.SetTop(_dragPreviewRect, top);
            _dragPreviewRect.Width = width;
            _dragPreviewRect.Height = height;
        }

        /// <summary>
        /// Handles MouseLeftButtonUp on the canvas.
        /// Finishes drawing and creates the new region.
        /// </summary>
        private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging || _regionCanvas == null || _dragPreviewRect == null) return;
            if (DataContext is not MouseClickViewModel vm) return;

            // Get final rectangle position and size
            var left = Canvas.GetLeft(_dragPreviewRect);
            var top = Canvas.GetTop(_dragPreviewRect);
            var width = _dragPreviewRect.Width;
            var height = _dragPreviewRect.Height;

            // Minimum size check
            const double minSize = 20;
            if (width >= minSize && height >= minSize)
            {
                // Convert to percentages
                var canvasWidth = _regionCanvas.ActualWidth;
                var canvasHeight = _regionCanvas.ActualHeight;

                var xPercent = left / canvasWidth * 100;
                var yPercent = top / canvasHeight * 100;
                var widthPercent = width / canvasWidth * 100;
                var heightPercent = height / canvasHeight * 100;

                // Create the region
                vm.CreateRegion(xPercent, yPercent, widthPercent, heightPercent);
            }

            // Clean up
            _regionCanvas.Children.Remove(_dragPreviewRect);
            _dragPreviewRect = null;
            _regionCanvas.ReleaseMouseCapture();
            _isDragging = false;
        }

        /// <summary>
        /// Handles hit testing and selection of existing regions.
        /// </summary>
        private void HandleRegionSelection(Point clickPosition)
        {
            if (DataContext is not MouseClickViewModel vm) return;
            if (_regionCanvas == null) return;

            // Convert click position to percentages
            var xPercent = clickPosition.X / _regionCanvas.ActualWidth * 100;
            var yPercent = clickPosition.Y / _regionCanvas.ActualHeight * 100;

            // Find top-most region containing the point
            ClickRegionModel? hitRegion = null;
            foreach (var region in vm.Regions)
            {
                if (region.ContainsPoint(xPercent, yPercent))
                {
                    hitRegion = region;
                }
            }

            // Select the region (or deselect if clicking empty space)
            vm.SelectRegionCommand.Execute(hitRegion);
        }

        #endregion

        #region Thumbnail GIF Looping

        /// <summary>
        /// Handles MediaOpened event for thumbnail MediaElements.
        /// Required when LoadedBehavior="Manual": starts playback once the media source is loaded.
        /// </summary>
        private void OnThumbnailMediaOpened(object sender, RoutedEventArgs e)
        {
            if (sender is MediaElement mediaElement)
            {
                mediaElement.Play();
            }
        }

        /// <summary>
        /// Handles MediaEnded event for GIF/video thumbnail MediaElements.
        /// Loops the media by resetting position to the beginning.
        /// Note: Position is set to 1ms instead of Zero because MediaElement does not
        /// restart playback when Position is set to exactly TimeSpan.Zero after MediaEnded.
        /// </summary>
        private void OnThumbnailMediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is MediaElement mediaElement)
            {
                mediaElement.Position = TimeSpan.FromMilliseconds(1);
                mediaElement.Play();
            }
        }

        #endregion

        #region Audio ListView Drag-and-Drop Reorder

        private Point _audioDragStartPoint;
        private bool _isAudioDragStartPending;

        /// <summary>
        /// Records the mouse position when a potential drag starts on the audio list.
        /// </summary>
        private void OnAudioListViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _audioDragStartPoint = e.GetPosition(null);
            _isAudioDragStartPending = true;
        }

        /// <summary>
        /// Starts a drag operation if mouse has moved enough distance.
        /// </summary>
        private void OnAudioListViewPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isAudioDragStartPending || e.LeftButton != MouseButtonState.Pressed)
            {
                _isAudioDragStartPending = false;
                return;
            }

            var currentPos = e.GetPosition(null);
            var diff = _audioDragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isAudioDragStartPending = false;

                if (sender is not ListView listView) return;

                var sourceItem = FindAncestorMediaItem(e.OriginalSource as DependencyObject, listView);
                if (sourceItem == null) return;

                var data = new DataObject("MediaItemModel", sourceItem);
                DragDrop.DoDragDrop(listView, data, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// Allows the drop and shows move cursor.
        /// </summary>
        private void OnAudioListViewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("MediaItemModel"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles the drop by reordering items in the audio collection.
        /// </summary>
        private void OnAudioListViewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("MediaItemModel")) return;
            if (sender is not ListView listView) return;
            if (listView.ItemsSource is not System.Collections.ObjectModel.ObservableCollection<MediaItemModel> collection) return;

            var droppedItem = e.Data.GetData("MediaItemModel") as MediaItemModel;
            if (droppedItem == null) return;

            var targetItem = FindAncestorMediaItem(e.OriginalSource as DependencyObject, listView);
            if (targetItem == null || targetItem == droppedItem) return;

            var oldIndex = collection.IndexOf(droppedItem);
            var newIndex = collection.IndexOf(targetItem);

            if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex) return;

            collection.Move(oldIndex, newIndex);

            // Re-index all items
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].OrderIndex = i;
            }
        }

        /// <summary>
        /// Finds the MediaItemModel data context of the visual tree ancestor that is a ListViewItem.
        /// </summary>
        private static MediaItemModel? FindAncestorMediaItem(DependencyObject? element, ListView listView)
        {
            while (element != null && element != listView)
            {
                if (element is ListViewItem lvi)
                {
                    return lvi.DataContext as MediaItemModel;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        #endregion
    }
}
