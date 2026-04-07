using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// Base ViewModel for all media configuration features (DesktopBackground, Shutdown,
    /// BootRestart, ScreenWake, and future system event types).
    /// Contains all shared media import/remove/preview logic, playback settings, and scheme identity.
    /// Concrete subclasses only need to provide metadata via abstract properties.
    /// </summary>
    public abstract partial class MediaConfigurationViewModel : ObservableObject, IFeatureViewModel
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

        // --- Abstract Metadata ---

        /// <summary>
        /// The default scheme name shown when a new scheme is created.
        /// </summary>
        protected abstract string DefaultSchemeName { get; }

        /// <summary>
        /// Storage key prefix for persisting feature-specific settings.
        /// </summary>
        public abstract string StorageKeyPrefix { get; }

        /// <summary>
        /// Whether this feature includes an audio media section.
        /// Override to return false for features that don't support separate audio (e.g. DesktopBackground).
        /// </summary>
        public virtual bool IncludeAudio => true;

        // --- Observable Properties ---

        [ObservableProperty]
        private string _schemeName = string.Empty;

        [ObservableProperty]
        private bool _isEditingName;

        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _imageVideoItems = new();

        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _audioItems = new();

        [ObservableProperty]
        private PlaybackMode _selectedPlaybackMode = PlaybackMode.Sequential;

        [ObservableProperty]
        private PlaybackMode _selectedAudioPlaybackMode = PlaybackMode.Sequential;

        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// Whether the scheme has any image/video content.
        /// </summary>
        public bool HasContent => ImageVideoItems.Count > 0;

        // --- Constructor ---

        protected MediaConfigurationViewModel()
        {
            _schemeName = DefaultSchemeName;
        }

        // --- Commands ---

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
                ReorderItems(ImageVideoItems);
            }

            OnPropertyChanged(nameof(HasContent));
        }

        [RelayCommand]
        private void ToggleMute(MediaItemModel? item)
        {
            if (item != null)
            {
                item.IsMuted = !item.IsMuted;
            }
        }

        [RelayCommand]
        private void PreviewMedia(MediaItemModel? item)
        {
            if (item == null || !File.Exists(item.FilePath))
                return;

            var previewWindow = new Views.PreviewWindow
            {
                DataContext = item,
                Title = $"Preview - {item.FileName}"
            };
            previewWindow.Show();
        }

        [RelayCommand]
        private void ToggleEditName()
        {
            IsEditingName = !IsEditingName;
        }

        [RelayCommand]
        private void FinishEditName()
        {
            IsEditingName = false;
        }

        [RelayCommand]
        private void ActivateScheme()
        {
            IsActive = true;
        }

        // --- Helper Methods ---

        private void AddMediaFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    System.Windows.MessageBox.Show(
                        $"File '{fileInfo.Name}' exceeds the maximum size of 500MB.",
                        "File Too Large",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var extension = fileInfo.Extension.ToLowerInvariant();
                var mediaType = GetMediaTypeFromExtension(extension);

                if (mediaType == MediaFileType.Audio)
                {
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

                if (mediaType == MediaFileType.Image)
                {
                    item.ThumbnailPath = filePath;
                }

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

        private void AddAudioFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

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
                    DisplayMode = DisplayMode.Fill
                };

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

        private static MediaFileType GetMediaTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or
                ".heic" or ".svg" or ".gif" or ".apng" or ".tiff" or ".tif" or ".ico" => MediaFileType.Image,
                ".mp4" or ".mov" or ".webm" or ".avi" or ".mkv" or ".flv" or ".wmv" => MediaFileType.Video,
                ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => MediaFileType.Audio,
                _ => MediaFileType.Image
            };
        }

        private static void ReorderItems(ObservableCollection<MediaItemModel> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].OrderIndex = i;
            }
        }

        private static TimeSpan? GetVideoDuration(string filePath)
        {
            // TODO: Implement using FFmediaToolkit or similar library
            return null;
        }

        private static TimeSpan? GetAudioDuration(string filePath)
        {
            // TODO: Implement using TagLib# or similar library
            return null;
        }

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
