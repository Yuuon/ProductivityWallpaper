using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    public partial class MouseClickViewModel : ObservableObject
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

        // --- Computed Properties ---

        /// <summary>
        /// Returns true when a region is selected.
        /// </summary>
        public bool HasSelectedRegion => SelectedRegion != null;

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
        /// </summary>
        private void LoadAvailableMedia()
        {
            // TODO: Load from Desktop Background scheme when integration is available
            // For now, initialize empty collection
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
                    SelectedRegion.VisualContent = new MediaItemModel(filePath)
                    {
                        Type = isVideo ? MediaFileType.Video : MediaFileType.Image,
                        DisplayMode = DisplayMode.Fill
                    };
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
                    var mediaItem = new MediaItemModel(filePath)
                    {
                        Type = MediaFileType.Audio
                    };
                    SelectedRegion.AudioContent.Add(mediaItem);
                    
                    if (SelectedRegion.AudioContent.Count >= 5) break;
                }
            }
        }

        /// <summary>
        /// Removes the visual content from the selected region.
        /// </summary>
        [RelayCommand]
        private void RemoveRegionVisual()
        {
            if (SelectedRegion == null) return;
            SelectedRegion.VisualContent = null;
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
        }

        /// <summary>
        /// Toggles scheme name editing mode.
        /// </summary>
        [RelayCommand]
        private void ToggleEditName()
        {
            IsEditingName = !IsEditingName;
        }

        // --- Public Methods ---

        /// <summary>
        /// Creates a new region from percentage coordinates.
        /// Called by the view after mouse drawing operation.
        /// </summary>
        /// <param name="x">X position as percentage (0-100).</param>
        /// <param name="y">Y position as percentage (0-100).</param>
        /// <param name="width">Width as percentage (0-100).</param>
        /// <param name="height">Height as percentage (0-100).</param>
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

            // Clamp to canvas bounds
            x = Math.Max(0, Math.Min(100 - width, x));
            y = Math.Max(0, Math.Min(100 - height, y));

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
    }
}
