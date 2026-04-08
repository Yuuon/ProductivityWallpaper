using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProductivityWallpaper.ViewModels
{
    public partial class MyThemeViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IThemeService _themeService;
        
        [ObservableProperty]
        private bool _isUsageHistorySelected = true;
        
        [ObservableProperty]
        private bool _isMyWorksSelected;
        
        [ObservableProperty]
        private ObservableCollection<ThemeItem> _themeItems = new();
        
        [ObservableProperty]
        private ThemeItem? _selectedTheme;
        
        [ObservableProperty]
        private bool _hasSelectedTheme;
        
        [ObservableProperty]
        private bool _hasContent = false;
        
        [ObservableProperty]
        private string _emptyMessage = "Nothing here yet\nGo find themes you like~";
        
        [ObservableProperty]
        private string _actionButtonText = "Browse Workshop";
        
        public MyThemeViewModel(MainViewModel mainViewModel, IThemeService themeService)
        {
            _mainViewModel = mainViewModel;
            _themeService = themeService;
            ShowUsageHistory();
        }
        
        [RelayCommand]
        private void ShowUsageHistory()
        {
            IsUsageHistorySelected = true;
            IsMyWorksSelected = false;
            EmptyMessage = System.Windows.Application.Current.TryFindResource("MyThemes_EmptyHistoryMessage") as string 
                ?? "Nothing here yet\nGo find themes you like~";
            ActionButtonText = System.Windows.Application.Current.TryFindResource("MyThemes_GoToWorkshop") as string 
                ?? "Browse Workshop";
            StopAllSlideshows();
            HasContent = false;
        }
        
        [RelayCommand]
        private async Task ShowMyWorks()
        {
            IsUsageHistorySelected = false;
            IsMyWorksSelected = true;
            EmptyMessage = System.Windows.Application.Current.TryFindResource("MyThemes_EmptyWorksMessage") as string 
                ?? "Nothing here yet\nGo create your own theme~";
            ActionButtonText = System.Windows.Application.Current.TryFindResource("MyThemes_GoCreate") as string 
                ?? "Start Creating";

            // Load saved themes from disk
            await LoadSavedThemesAsync();
            HasContent = ThemeItems.Count > 0;
        }

        /// <summary>
        /// Loads all saved themes from the themes folder into ThemeItems.
        /// Collects image/video resource source paths as thumbnails for slideshow display.
        /// </summary>
        private async Task LoadSavedThemesAsync()
        {
            StopAllSlideshows();
            ThemeItems.Clear();
            SelectedTheme = null;
            HasSelectedTheme = false;

            try
            {
                var themeNames = _themeService.GetThemeNames();
                foreach (var name in themeNames)
                {
                    var manifest = await _themeService.LoadThemeAsync(name);
                    if (manifest == null) continue;

                    var themeItem = new ThemeItem
                    {
                        Name = manifest.Name,
                        Author = string.IsNullOrEmpty(manifest.Author) ? "Me" : manifest.Author,
                        Type = "Custom",
                        FileSize = 0,
                        Resolution = "",
                        ThemeFolderName = name
                    };

                    // Collect thumbnail paths from theme resources (images and videos)
                    CollectThumbnailPaths(themeItem, manifest);

                    ThemeItems.Add(themeItem);
                }

                Debug.WriteLine($"[MyThemeViewModel] Loaded {ThemeItems.Count} saved themes");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MyThemeViewModel] Error loading themes: {ex.Message}");
            }
        }

        /// <summary>
        /// Collects existing thumbnail or source paths from theme resources for slideshow display.
        /// Prioritizes image source paths (they are their own thumbnails) and video resource source paths.
        /// Starts the slideshow timer if multiple thumbnails are found.
        /// </summary>
        private static void CollectThumbnailPaths(ThemeItem themeItem, ThemeManifest manifest)
        {
            var resources = manifest.ResourceLibrary?.Resources;
            if (resources == null || resources.Count == 0) return;

            foreach (var resource in resources)
            {
                if (resource.Type == MediaType.Audio) continue;

                // Use source path for images (they are their own thumbnails)
                // Use thumbnail path for videos if available, otherwise source path
                string? path = null;

                if (resource.Type == MediaType.Image && !string.IsNullOrEmpty(resource.SourcePath) 
                    && File.Exists(resource.SourcePath))
                {
                    path = resource.SourcePath;
                }
                else if (!string.IsNullOrEmpty(resource.ThumbnailPath) && File.Exists(resource.ThumbnailPath))
                {
                    path = resource.ThumbnailPath;
                }
                else if (!string.IsNullOrEmpty(resource.SourcePath) && File.Exists(resource.SourcePath)
                         && resource.Type == MediaType.Image)
                {
                    path = resource.SourcePath;
                }

                if (!string.IsNullOrEmpty(path))
                {
                    themeItem.ThumbnailPaths.Add(path);
                }
            }

            // Set initial thumbnail
            if (themeItem.ThumbnailPaths.Count > 0)
            {
                themeItem.Thumbnail = themeItem.ThumbnailPaths[0];
            }

            // Start slideshow if multiple thumbnails available
            if (themeItem.ThumbnailPaths.Count > 1)
            {
                themeItem.StartSlideshow();
            }
        }

        /// <summary>
        /// Stops all active slideshow timers to prevent resource leaks.
        /// </summary>
        private void StopAllSlideshows()
        {
            foreach (var item in ThemeItems)
            {
                item.StopSlideshow();
            }
        }
        
        [RelayCommand]
        private void SelectTheme(ThemeItem theme)
        {
            SelectedTheme = theme;
            HasSelectedTheme = theme != null;
        }
        
        [RelayCommand]
        private void EmptyStateAction()
        {
            if (IsUsageHistorySelected)
            {
                _mainViewModel.NavigateToWorkshopCommand.Execute(null);
            }
            else
            {
                _mainViewModel.NavigateToCreatorCommand.Execute(null);
            }
        }
        
        [RelayCommand]
        private void UseTheme()
        {
            // TODO: Implement use theme logic (apply theme to desktop)
        }
        
        [RelayCommand]
        private async Task EditTheme()
        {
            if (SelectedTheme == null || string.IsNullOrEmpty(SelectedTheme.ThemeFolderName))
                return;

            await _mainViewModel.NavigateToCreatorWithThemeCommand.ExecuteAsync(SelectedTheme.ThemeFolderName);
        }
    }
}
