using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;

namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Mouse Click configuration view, managing click regions and media assignments.
    /// </summary>
    public partial class MouseClickViewModel : ObservableObject, IFeatureViewModel
    {
        // --- Private Fields ---
        private readonly ConfigService _configService;

        // --- Observable Properties ---

        /// <summary>
        /// Collection of click regions defined on the canvas.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ClickRegionModel> _regions = new();

        /// <summary>
        /// Currently selected region for configuration.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedRegion))]
        [NotifyPropertyChangedFor(nameof(HasRegionMedia))]
        [NotifyPropertyChangedFor(nameof(CanAddAudio))]
        [NotifyPropertyChangedFor(nameof(CanAddVisual))]
        private ClickRegionModel? _selectedRegion;

        /// <summary>
        /// True when in "add region" mode (user is drawing a new region).
        /// </summary>
        [ObservableProperty]
        private bool _isAddingMode;

        /// <summary>
        /// Currently displayed background media on the canvas.
        /// </summary>
        [ObservableProperty]
        private MediaItemModel? _backgroundMedia;

        /// <summary>
        /// Available media items from Desktop Background scheme.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MediaItemModel> _availableMedia = new();

        /// <summary>
        /// Current canvas aspect ratio (width/height).
        /// </summary>
        [ObservableProperty]
        private double _canvasAspectRatio = 16.0 / 9.0;

        /// <summary>
        /// Screen resolution display text (e.g., "1920x1080 (16:9)").
        /// </summary>
        [ObservableProperty]
        private string _screenResolutionText = "1920x1080 (16:9)";

        /// <summary>
        /// Current scheme name being edited.
        /// </summary>
        [ObservableProperty]
        private string _schemeName = string.Empty;

        /// <summary>
        /// True when editing the scheme name.
        /// </summary>
        [ObservableProperty]
        private bool _isEditingName;

        /// <summary>
        /// Whether this scheme is currently active.
        /// </summary>
        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// Returns true when a region is selected.
        /// </summary>
        public bool HasSelectedRegion => SelectedRegion != null;

        /// <summary>
        /// Returns true when the selected region has at least one imported resource (visual or audio).
        /// </summary>
        public bool HasRegionMedia => SelectedRegion?.VisualContent != null || (SelectedRegion?.AudioContent.Count ?? 0) > 0;

        /// <summary>
        /// Returns true when visual content can be added (none exists yet).
        /// </summary>
        public bool CanAddVisual => SelectedRegion?.VisualContent == null;

        /// <summary>
        /// Returns true when audio content can be added (less than 5 files).
        /// </summary>
        public bool CanAddAudio => SelectedRegion?.AudioContent.Count < 5;

        // --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseClickViewModel"/> class.
        /// </summary>
        public MouseClickViewModel()
        {
            _configService = App.Current.Services.GetRequiredService<ConfigService>();
            InitializeScreenInfo();
            LoadAvailableMedia();
            Regions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasContent));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseClickViewModel"/> class with config service.
        /// </summary>
        /// <param name="configService">The configuration service.</param>
        public MouseClickViewModel(ConfigService configService)
        {
            _configService = configService;
            InitializeScreenInfo();
            LoadAvailableMedia();
            Regions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasContent));
        }

        // --- Initialization Methods ---

        /// <summary>
        /// Calculates screen resolution and aspect ratio.
        /// </summary>
        private void InitializeScreenInfo()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            CanvasAspectRatio = screenWidth / screenHeight;
            
            // Calculate simplified aspect ratio
            var gcd = CalculateGcd((int)screenWidth, (int)screenHeight);
            var ratioW = (int)screenWidth / gcd;
            var ratioH = (int)screenHeight / gcd;
            
            ScreenResolutionText = $"{(int)screenWidth}x{(int)screenHeight} ({ratioW}:{ratioH})";
        }

        /// <summary>
        /// Loads available media from Desktop Background scheme.
        /// Called by CreatorViewModel.PopulateAvailableMedia() and LoadFeatureContent().
        /// </summary>
        private void LoadAvailableMedia()
        {
            // AvailableMedia is populated by CreatorViewModel when this VM is loaded.
            // Initialize with empty collection as a placeholder.
            AvailableMedia = new ObservableCollection<MediaItemModel>();
        }

        /// <summary>
        /// Calculates the greatest common divisor using Euclidean algorithm.
        /// </summary>
        private static int CalculateGcd(int a, int b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        // --- Commands ---

        /// <summary>
        /// Enters "add region" mode.
        /// </summary>
        [RelayCommand]
        private void StartAddingRegion()
        {
            IsAddingMode = true;
        }

        /// <summary>
        /// Exits "add region" mode.
        /// </summary>
        [RelayCommand]
        private void CancelAddingRegion()
        {
            IsAddingMode = false;
        }

        /// <summary>
        /// Deletes a region from the collection.
        /// </summary>
        /// <param name="region">The region to delete.</param>
        [RelayCommand]
        private void DeleteRegion(ClickRegionModel? region)
        {
            if (region == null) return;
            
            Regions.Remove(region);
            
            if (SelectedRegion == region)
            {
                SelectedRegion = null;
            }
        }

        /// <summary>
        /// Selects a region for configuration.
        /// </summary>
        /// <param name="region">The region to select, or null to deselect.</param>
        [RelayCommand]
        private void SelectRegion(ClickRegionModel? region)
        {
            // Deselect previous
            if (SelectedRegion != null)
            {
                SelectedRegion.IsSelected = false;
            }
            
            SelectedRegion = region;
            
            // Select new
            if (SelectedRegion != null)
            {
                SelectedRegion.IsSelected = true;
            }
        }

        /// <summary>
        /// Sets the background media for the canvas.
        /// </summary>
        /// <param name="media">The media item to set as background.</param>
        [RelayCommand]
        private void SetBackgroundMedia(MediaItemModel? media)
        {
            BackgroundMedia = media;
        }

        /// <summary>
        /// Imports visual content (image or video) for the selected region.
        /// </summary>
        [RelayCommand]
        private void ImportRegionVisual()
        {
            if (SelectedRegion == null) return;
            if (!CanAddVisual) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Image or Video",
                Filter = "Image/Video Files|*.mp4;*.mov;*.avi;*.webm;*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp|Video Files|*.mp4;*.mov;*.avi;*.webm|Image Files|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp|All Files|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                
                var videoExtensions = new[] { ".mp4", ".mov", ".avi", ".webm", ".mkv" };
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
                var isVideo = videoExtensions.Any(ext => ext == extension);
                var isImage = imageExtensions.Any(ext => ext == extension);
                
                if (isVideo || isImage)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists) return;

                    var mediaItem = new MediaItemModel(filePath)
                    {
                        Type = isVideo ? MediaFileType.Video : MediaFileType.Image,
                        FileSize = fileInfo.Length,
                        DisplayMode = DisplayMode.Fill
                    };

                    // Set thumbnail for images
                    if (isImage)
                    {
                        mediaItem.ThumbnailPath = filePath;
                    }

                    // Generate animated GIF thumbnail for videos
                    if (isVideo)
                    {
                        _ = GenerateVideoThumbnailAsync(mediaItem);
                    }

                    SelectedRegion.VisualContent = mediaItem;
                    OnPropertyChanged(nameof(HasRegionMedia));
                    OnPropertyChanged(nameof(CanAddVisual));
                }
            }
        }

        /// <summary>
        /// Imports audio content for the selected region.
        /// </summary>
        [RelayCommand]
        private void ImportRegionAudio()
        {
            if (SelectedRegion == null) return;
            if (!CanAddAudio) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Audio",
                Filter = "Audio Files|*.mp3;*.wav;*.ogg;*.flac;*.aac;*.m4a|All Files|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var remainingSlots = 5 - SelectedRegion.AudioContent.Count;
                var filesToAdd = dialog.FileNames.Take(remainingSlots);
                
                foreach (var filePath in filesToAdd)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists) continue;

                    var mediaItem = new MediaItemModel(filePath)
                    {
                        Type = MediaFileType.Audio,
                        FileSize = fileInfo.Length,
                        OrderIndex = SelectedRegion.AudioContent.Count
                    };
                    SelectedRegion.AudioContent.Add(mediaItem);
                    
                    if (SelectedRegion.AudioContent.Count >= 5) break;
                }
                OnPropertyChanged(nameof(HasRegionMedia));
                OnPropertyChanged(nameof(CanAddAudio));
            }
        }

        /// <summary>
        /// Removes the visual content from the selected region.
        /// Also deletes the associated thumbnail file.
        /// </summary>
        [RelayCommand]
        private void RemoveRegionVisual()
        {
            if (SelectedRegion == null) return;
            
            // Delete thumbnail file if it exists
            if (SelectedRegion.VisualContent != null)
            {
                DeleteThumbnailFile(SelectedRegion.VisualContent);
            }
            
            SelectedRegion.VisualContent = null;
            OnPropertyChanged(nameof(HasRegionMedia));
            OnPropertyChanged(nameof(CanAddVisual));
        }

        /// <summary>
        /// Removes an audio item from the selected region.
        /// </summary>
        /// <param name="audio">The audio item to remove.</param>
        [RelayCommand]
        private void RemoveRegionAudio(MediaItemModel? audio)
        {
            if (SelectedRegion == null || audio == null) return;
            SelectedRegion.AudioContent.Remove(audio);
            OnPropertyChanged(nameof(HasRegionMedia));
            OnPropertyChanged(nameof(CanAddAudio));
        }

        /// <summary>
        /// Deletes the thumbnail file associated with a media item.
        /// Only deletes if the thumbnail path is different from the source file path.
        /// </summary>
        private static void DeleteThumbnailFile(MediaItemModel item)
        {
            if (string.IsNullOrEmpty(item.ThumbnailPath)) return;
            if (item.ThumbnailPath == item.FilePath) return;

            try
            {
                if (File.Exists(item.ThumbnailPath))
                {
                    File.Delete(item.ThumbnailPath);
                    System.Diagnostics.Debug.WriteLine(
                        $"[MouseClickViewModel] Deleted thumbnail: {item.ThumbnailPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MouseClickViewModel] Error deleting thumbnail: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggles mute state on a media item.
        /// </summary>
        [RelayCommand]
        private void ToggleMute(MediaItemModel? item)
        {
            if (item != null)
            {
                item.IsMuted = !item.IsMuted;
            }
        }

        /// <summary>
        /// Opens a preview window for the given media item.
        /// </summary>
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

        /// <summary>
        /// Toggles scheme name editing mode.
        /// </summary>
        [RelayCommand]
        private void ToggleEditName()
        {
            IsEditingName = !IsEditingName;
        }

        /// <summary>
        /// Exits scheme name editing mode.
        /// </summary>
        [RelayCommand]
        private void FinishEditName()
        {
            IsEditingName = false;
        }

        /// <summary>
        /// Marks this scheme as the active scheme.
        /// CreatorViewModel.SelectScheme() handles deactivating others.
        /// </summary>
        [RelayCommand]
        private void ActivateScheme()
        {
            IsActive = true;
        }

        /// <summary>
        /// Returns true if there are any regions defined (for activate button visibility).
        /// </summary>
        public bool HasContent => Regions.Count > 0;

        // --- Public Methods ---

        /// <summary>
        /// Creates a new region from normalized coordinates (0–1).
        /// Called by the view after mouse drawing operation.
        /// </summary>
        /// <param name="x">X position normalized (0–1).</param>
        /// <param name="y">Y position normalized (0–1).</param>
        /// <param name="width">Width normalized (0–1).</param>
        /// <param name="height">Height normalized (0–1).</param>
        public void CreateRegion(double x, double y, double width, double height)
        {
            // Normalize negative dimensions
            if (width < 0)
            {
                x += width;
                width = -width;
            }
            if (height < 0)
            {
                y += height;
                height = -height;
            }

            // Clamp to canvas bounds (0–1)
            x = Math.Max(0, Math.Min(1.0 - width, x));
            y = Math.Max(0, Math.Min(1.0 - height, y));

            var region = new ClickRegionModel
            {
                Name = $"Region {GetNextRegionNumber()}",
                X = x,
                Y = y,
                Width = width,
                Height = height
            };

            // Validate
            if (!region.IsValid())
            {
                var error = region.GetValidationError();
                System.Windows.MessageBox.Show($"Invalid region: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Regions.Add(region);
            
            // Auto-select the new region
            SelectRegion(region);
            
            // Exit adding mode
            IsAddingMode = false;
        }

        /// <summary>
        /// Gets the next available region number for auto-naming.
        /// </summary>
        /// <returns>The next region number.</returns>
        private int GetNextRegionNumber()
        {
            var maxNumber = 0;
            foreach (var region in Regions)
            {
                if (region.Name.StartsWith("Region "))
                {
                    if (int.TryParse(region.Name[7..], out var num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }
            }
            return maxNumber + 1;
        }

        /// <summary>
        /// Gets the current theme folder path for thumbnail storage.
        /// </summary>
        private static string? GetCurrentThemeFolderPath()
        {
            var themeService = App.Current.Services.GetService<IThemeService>();
            if (themeService?.CurrentTheme != null && !string.IsNullOrEmpty(themeService.CurrentTheme.Name))
            {
                return themeService.GetThemeFolderPath(themeService.CurrentTheme.Name);
            }
            return null;
        }

        /// <summary>
        /// Generates an animated GIF thumbnail for a video item asynchronously.
        /// Stores the thumbnail in the current theme's thumbnails/ folder for persistence.
        /// </summary>
        private static async Task GenerateVideoThumbnailAsync(MediaItemModel item)
        {
            try
            {
                var thumbnailService = App.Current.Services.GetService<IThumbnailService>();
                if (thumbnailService == null) return;

                var themeFolderPath = GetCurrentThemeFolderPath();
                if (string.IsNullOrEmpty(themeFolderPath))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[MouseClickViewModel] No theme folder — cannot generate video thumbnail");
                    return;
                }

                var thumbPath = await thumbnailService.GenerateVideoGifThumbnailAsync(
                    item.FilePath, item.Id, themeFolderPath);

                if (!string.IsNullOrEmpty(thumbPath))
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        item.ThumbnailPath = thumbPath;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MouseClickViewModel] Video thumbnail generation failed: {ex.Message}");
            }
        }
    }
}
