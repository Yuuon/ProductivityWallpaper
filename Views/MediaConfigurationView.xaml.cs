using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductivityWallpaper.Models;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using Point = System.Windows.Point;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Unified view for all media configuration features (Desktop Background, Shutdown,
    /// Boot/Restart, Screen Wake). Binds to any MediaConfigurationViewModel subclass.
    /// </summary>
    public partial class MediaConfigurationView : System.Windows.Controls.UserControl
    {
        // --- Drag-and-Drop State ---
        private Point _dragStartPoint;
        private bool _isDragStartPending;

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
                mediaElement.Position = System.TimeSpan.FromMilliseconds(1);
                mediaElement.Play();
            }
        }

        #region ListView Drag-and-Drop Reorder

        /// <summary>
        /// Records the mouse position when a potential drag starts.
        /// </summary>
        private void OnListViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragStartPending = true;
        }

        /// <summary>
        /// Starts a drag operation if mouse has moved enough distance from the start point.
        /// </summary>
        private void OnListViewPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragStartPending || e.LeftButton != MouseButtonState.Pressed)
            {
                _isDragStartPending = false;
                return;
            }

            var currentPos = e.GetPosition(null);
            var diff = _dragStartPoint - currentPos;

            if (System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragStartPending = false;

                if (sender is not ListView listView) return;

                // Find the source item from the event position
                var sourceItem = FindAncestorItem(e.OriginalSource as DependencyObject, listView);
                if (sourceItem == null) return;

                var data = new DataObject("MediaItemModel", sourceItem);
                DragDrop.DoDragDrop(listView, data, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// Allows the drop and shows move cursor.
        /// </summary>
        private void OnListViewDragOver(object sender, DragEventArgs e)
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
        /// Handles the drop by reordering items in the collection.
        /// </summary>
        private void OnListViewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("MediaItemModel")) return;
            if (sender is not ListView listView) return;
            if (listView.ItemsSource is not ObservableCollection<MediaItemModel> collection) return;

            var droppedItem = e.Data.GetData("MediaItemModel") as MediaItemModel;
            if (droppedItem == null) return;

            // Find the drop target
            var targetItem = FindAncestorItem(e.OriginalSource as DependencyObject, listView);
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
        private static MediaItemModel? FindAncestorItem(DependencyObject? element, ListView listView)
        {
            while (element != null && element != listView)
            {
                if (element is ListViewItem lvi)
                {
                    return lvi.DataContext as MediaItemModel;
                }
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        #endregion
    }
}
