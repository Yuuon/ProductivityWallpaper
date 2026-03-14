using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Screen Wake configuration view.
    /// Manages media items, playback settings, and file operations for screen wake events.
    /// </summary>
    public partial class ScreenWakeViewModel : ObservableObject
    {
        // --- Constants ---
        private const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500MB
        private const string ImageVideoFilter = 
            "Media files|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic;*.svg;*.gif;*.apng;*.mp4;*.mov;*.webm|" +
            "Image files|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.heic;*.svg;*.gif;*.apng|" +
            "Video files|*.mp4;*.mov;*.webm|" +
            "All files|*.*";
        private const string AudioFilter = 
            "Audio files|*.mp3;*.wav;*.ogg;*.flac;*.aac;*.wma;*.m4a|" +
            "All files|*.*";

        // --- Properties ---
        /// <summary>
        /// Gets or sets the name of the current scheme.
        /// </summary>
        [ObservableProperty]
        private string _schemeName = "Screen Wake";

        /// <summary>
        /// Gets or sets a value indicating whether the scheme name is being edited.
        /// </summary>
        [ObservableProperty]
        private bool _isEditingName;

        /// <summary>
        /// Gets or sets the collection of image and video media items.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _imageVideoItems = new();

        /// <summary>
        /// Gets or sets the collection of audio media items.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _audioItems = new();

        /// <summary>
        /// Gets or sets the selected playback mode for image/video files.
        /// </summary>
        [ObservableProperty]
        private PlaybackMode _selectedPlaybackMode = PlaybackMode.Sequential;

        /// <summary>
        /// Gets or sets the selected playback mode for audio files.
        /// </summary>
        [ObservableProperty]
        private PlaybackMode _selectedAudioPlaybackMode = PlaybackMode.Sequential;

        /// <summary>
        /// Gets or sets a value indicating whether this is the currently active scheme.
        /// </summary>
        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// Gets a value indicating whether the scheme has any content.
        /// </summary>
        public bool HasContent => ImageVideoItems.Count > 0;

        /// <summary>
        /// Gets the storage key prefix for this feature type.
        /// </summary>
        public string StorageKeyPrefix => "ScreenWake";

        // --- Commands ---

        /// <summary>
        /// Opens a file dialog to import media files (images and videos).
        /// </summary>
        [RelayCommand]
        private void ImportMedia()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Media Files",
                Filter = ImageVideoFilter,
                Multiselect = true,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    AddMediaFile(filePath);
                }
            }
        }

        /// <summary>
        /// Opens a file dialog to import audio files.
        /// </summary>
        [RelayCommand]
        private void ImportAudio()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Audio Files",
                Filter = AudioFilter,
                Multiselect = true,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    AddAudioFile(filePath);
                }
            }
        }

        /// <summary>
        /// Removes a media item from the collection.
        /// </summary>
        /// <param name="item">The media item to remove.</param>
        [RelayCommand]
        private void RemoveMedia(MediaItemModel? item)
        {
            if (item == null)
                return;

            if (item.Type == MediaFileType.Audio)
            {
                AudioItems.Remove(item);
            }
            else
            {
                ImageVideoItems.Remove(item);
                // Reorder remaining items
                ReorderItems(ImageVideoItems);
            }

            OnPropertyChanged(nameof(HasContent));
        }

        /// <summary>
        /// Toggles the mute state of a media item.
        /// </summary>
        /// <param name="item">The media item to toggle mute for.</param>
        [RelayCommand]
        private void ToggleMute(MediaItemModel? item)
        {
            if (item != null)
            {
                item.IsMuted = !item.IsMuted;
            }
        }

        /// <summary>
        /// Opens a preview window for the specified media item.
        /// </summary>
        /// <param name="item">The media item to preview.</param>
        [RelayCommand]
        private void PreviewMedia(MediaItemModel? item)
        {
            if (item == null || !File.Exists(item.FilePath))
                return;

            // Open preview window - will be implemented with PreviewWindow
            // var previewWindow = new Views.PreviewWindow
            // {
            //     DataContext = item,
            //     Title = $"Preview - {item.FileName}"
            // };
            // previewWindow.Show();
        }

        /// <summary>
        /// Toggles the editing state for the scheme name.
        /// </summary>
        [RelayCommand]
        private void ToggleEditName()
        {
            IsEditingName = !IsEditingName;
        }

        /// <summary>
        /// Finishes editing the scheme name.
        /// </summary>
        [RelayCommand]
        private void FinishEditName()
        {
            IsEditingName = false;
        }

        /// <summary>
        /// Activates this scheme as the current screen wake scheme.
        /// </summary>
        [RelayCommand]
        private void ActivateScheme()
        {
            IsActive = true;
        }

        // --- Helper Methods ---

        /// <summary>
        /// Adds a media file to the image/video collection.
        /// </summary>
        /// <param name="filePath">The path to the media file.</param>
        private void AddMediaFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // Validate file size
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    System.Windows.MessageBox.Show(
                        $"File '{fileInfo.Name}' exceeds the maximum size of 500MB.",
                        "File Too Large",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Determine media type
                var extension = fileInfo.Extension.ToLowerInvariant();
                var mediaType = GetMediaTypeFromExtension(extension);

                if (mediaType == MediaFileType.Audio)
                {
                    // Audio files go to audio collection
                    AddAudioFile(filePath);
                    return;
                }

                var item = new MediaItemModel(filePath)
                {
                    Type = mediaType,
                    FileSize = fileInfo.Length,
                    OrderIndex = ImageVideoItems.Count,
                    DisplayMode = DisplayMode.Fill
                };

                // Try to get duration for video files
                if (mediaType == MediaFileType.Video)
                {
                    item.Duration = GetVideoDuration(filePath);
                }

                ImageVideoItems.Add(item);
                OnPropertyChanged(nameof(HasContent));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to add file '{filePath}': {ex.Message}",
                    "Import Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Adds an audio file to the audio collection.
        /// </summary>
        /// <param name="filePath">The path to the audio file.</param>
        private void AddAudioFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // Validate file size
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    System.Windows.MessageBox.Show(
                        $"File '{fileInfo.Name}' exceeds the maximum size of 500MB.",
                        "File Too Large",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var item = new MediaItemModel(filePath)
                {
                    Type = MediaFileType.Audio,
                    FileSize = fileInfo.Length,
                    OrderIndex = AudioItems.Count,
                    DisplayMode = DisplayMode.Fill // Not used for audio, but set default
                };

                // Try to get duration
                item.Duration = GetAudioDuration(filePath);

                AudioItems.Add(item);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to add audio file '{filePath}': {ex.Message}",
                    "Import Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets the media type from a file extension.
        /// </summary>
        /// <param name="extension">The file extension (including the dot).</param>
        /// <returns>The corresponding MediaFileType.</returns>
        private static MediaFileType GetMediaTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or 
                ".heic" or ".svg" or ".gif" or ".apng" or ".tiff" or ".tif" or ".ico" => MediaFileType.Image,
                ".mp4" or ".mov" or ".webm" or ".avi" or ".mkv" or ".flv" or ".wmv" => MediaFileType.Video,
                ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => MediaFileType.Audio,
                _ => MediaFileType.Image // Default to image for unknown types
            };
        }

        /// <summary>
        /// Reorders items in a collection after removal.
        /// </summary>
        /// <param name="items">The collection to reorder.</param>
        private static void ReorderItems(ObservableCollection<MediaItemModel> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].OrderIndex = i;
            }
        }

        /// <summary>
        /// Gets the duration of a video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>The duration as a TimeSpan, or null if unable to determine.</returns>
        private static TimeSpan? GetVideoDuration(string filePath)
        {
            // TODO: Implement using FFmediaToolkit or similar library
            // For now, return null as placeholder
            return null;
        }

        /// <summary>
        /// Gets the duration of an audio file.
        /// </summary>
        /// <param name="filePath">The path to the audio file.</param>
        /// <returns>The duration as a TimeSpan, or null if unable to determine.</returns>
        private static TimeSpan? GetAudioDuration(string filePath)
        {
            // TODO: Implement using TagLib# or similar library
            // For now, return null as placeholder
            return null;
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>A formatted string (e.g., "1.5 MB").</returns>
        public static string FormatFileSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            return bytes switch
            {
                >= GB => $"{bytes / (double)GB:F2} GB",
                >= MB => $"{bytes / (double)MB:F2} MB",
                >= KB => $"{bytes / (double)KB:F2} KB",
                _ => $"{bytes} B"
            };
        }

        /// <summary>
        /// Formats a TimeSpan to a display string.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>A formatted string (e.g., "3:45" or "1:23:45").</returns>
        public static string? FormatDuration(TimeSpan? duration)
        {
            if (!duration.HasValue)
                return null;

            var ts = duration.Value;
            if (ts.Hours > 0)
                return $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }
}