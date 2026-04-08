using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace ProductivityWallpaper.ViewModels
{
    public partial class WorkshopViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchText = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<string> _resolutions = new()
        {
            "All Resolutions", "1920x1080", "2560x1440", "3840x2160"
        };
        
        [ObservableProperty]
        private string _selectedResolution = "All Resolutions";
        
        [ObservableProperty]
        private ObservableCollection<string> _sortOptions = new()
        {
            "Most Popular", "Latest", "Name A-Z"
        };
        
        [ObservableProperty]
        private string _selectedSortOption = "Most Popular";
        
        [ObservableProperty]
        private ObservableCollection<ThemeItem> _themeItems = new();
        
        [ObservableProperty]
        private ThemeItem? _selectedTheme;
        
        [ObservableProperty]
        private bool _hasSelectedTheme;
        
        public WorkshopViewModel()
        {
            // Load mock data
            LoadMockData();
        }
        
        private void LoadMockData()
        {
            for (int i = 0; i < 9; i++)
            {
                ThemeItems.Add(new ThemeItem
                {
                    Name = $"Theme {i + 1}",
                    Author = $"Author {i + 1}",
                    FileSize = 45.2 + i,
                    Resolution = "3840x2160",
                    Type = "Video",
                    Tags = new ObservableCollection<TagItem>
                    {
                        new TagItem { Name = "Desktop Background", IsSelected = i == 0 },
                        new TagItem { Name = "Mouse Click", IsSelected = false },
                        new TagItem { Name = "Shutdown", IsSelected = false }
                    }
                });
            }
        }
        
        [RelayCommand]
        private void SelectTheme(ThemeItem theme)
        {
            SelectedTheme = theme;
            HasSelectedTheme = theme != null;
        }
        
        [RelayCommand]
        private void UseTheme()
        {
            // Implement use theme logic
        }
        
        [RelayCommand]
        private void EditTheme()
        {
            // Implement edit theme logic
        }
    }
    
    public partial class ThemeItem : ObservableObject, IDisposable
    {
        private DispatcherTimer? _slideshowTimer;
        private int _currentThumbnailIndex;
        private bool _disposed;

        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private string _author = string.Empty;
        
        [ObservableProperty]
        private double _fileSize;
        
        [ObservableProperty]
        private string _resolution = string.Empty;
        
        [ObservableProperty]
        private string _type = string.Empty;
        
        [ObservableProperty]
        private string _thumbnail = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<TagItem> _tags = new();

        /// <summary>
        /// Folder name (sanitized theme name) used for loading/editing the theme.
        /// </summary>
        [ObservableProperty]
        private string _themeFolderName = string.Empty;

        /// <summary>
        /// Collection of thumbnail paths for slideshow display.
        /// When multiple paths are set, the Thumbnail property cycles through them.
        /// </summary>
        public List<string> ThumbnailPaths { get; set; } = new();

        /// <summary>
        /// Initializes the slideshow timer to cycle through thumbnails.
        /// Only starts if there are multiple thumbnail paths.
        /// </summary>
        public void StartSlideshow()
        {
            if (ThumbnailPaths.Count <= 1) return;

            // Set initial thumbnail
            if (ThumbnailPaths.Count > 0 && string.IsNullOrEmpty(Thumbnail))
            {
                Thumbnail = ThumbnailPaths[0];
            }

            _currentThumbnailIndex = 0;
            _slideshowTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _slideshowTimer.Tick += OnSlideshowTick;
            _slideshowTimer.Start();
        }

        /// <summary>
        /// Stops the slideshow timer.
        /// </summary>
        public void StopSlideshow()
        {
            if (_slideshowTimer != null)
            {
                _slideshowTimer.Stop();
                _slideshowTimer.Tick -= OnSlideshowTick;
                _slideshowTimer = null;
            }
        }

        private void OnSlideshowTick(object? sender, EventArgs e)
        {
            if (ThumbnailPaths.Count == 0) return;

            _currentThumbnailIndex = (_currentThumbnailIndex + 1) % ThumbnailPaths.Count;
            Thumbnail = ThumbnailPaths[_currentThumbnailIndex];
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopSlideshow();
                _disposed = true;
            }
        }
    }
    
    public partial class TagItem : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private bool _isSelected;
    }
}
