using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ProductivityWallpaper.ViewModels
{
    public partial class WallpaperViewModel : ObservableObject
    {
        private readonly WallpaperService _wallpaperService;
        private readonly ConfigService _configService;

        [ObservableProperty]
        private ObservableCollection<MediaItem> _mediaLibrary = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanUseAutoTheme))] // 当选中项改变时，通知更新 CanUseAutoTheme
        private MediaItem? _selectedMedia;

        [ObservableProperty]
        private string _currentLibPath = "";

        [ObservableProperty]
        private ObservableCollection<MonitorInfo> _monitors = new();

        [ObservableProperty]
        private MonitorInfo? _selectedMonitor;

        [ObservableProperty]
        private bool _isAutoThemeColor = false;

        // 计算属性：控制 Checkbox 是否可用
        public bool CanUseAutoTheme => SelectedMedia != null && SelectedMedia.Type == MediaType.Image;

        public WallpaperViewModel(WallpaperService wpService, ConfigService configService)
        {
            _wallpaperService = wpService;
            _configService = configService;

            InitializeMonitors();

            var savedPath = _configService.Config.LastLibraryPath;
            if (!string.IsNullOrEmpty(savedPath) && System.IO.Directory.Exists(savedPath))
            {
                CurrentLibPath = savedPath;
            }
            else
            {
                CurrentLibPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            LoadLibrary();
        }

        private void InitializeMonitors()
        {
            Monitors.Clear();
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                Monitors.Add(new MonitorInfo
                {
                    DeviceName = screens[i].DeviceName,
                    Index = i,
                    IsPrimary = screens[i].Primary
                });
            }
            SelectedMonitor = Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();
        }

        [RelayCommand]
        public void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Wallpaper Library Folder",
                InitialDirectory = string.IsNullOrEmpty(CurrentLibPath) ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) : CurrentLibPath
            };

            if (dialog.ShowDialog() == true)
            {
                CurrentLibPath = dialog.FolderName;
                _configService.Config.LastLibraryPath = CurrentLibPath;
                _configService.Save();

                LoadLibrary();
            }
        }

        [RelayCommand]
        public void LoadLibrary()
        {
            if (string.IsNullOrEmpty(CurrentLibPath)) return;

            var cachedItems = _wallpaperService.TryLoadMetadata(CurrentLibPath);

            MediaLibrary.Clear();
            if (cachedItems != null && cachedItems.Count > 0)
            {
                foreach (var item in cachedItems) MediaLibrary.Add(item);
            }
            else
            {
                RefreshMetadata();
            }
        }

        [RelayCommand]
        public async Task RefreshMetadata()
        {
            if (string.IsNullOrEmpty(CurrentLibPath)) return;
            var items = await _wallpaperService.RefreshMetadataAsync(CurrentLibPath);
            MediaLibrary.Clear();
            foreach (var item in items) MediaLibrary.Add(item);
        }

        [RelayCommand]
        public void ApplyWallpaper()
        {
            if (SelectedMedia == null) return;

            // 如果可以应用主题色，则设置；否则强制为 false（或者保持不变但不生效）
            // 如果是视频，用户界面上应该已经禁用了该选项，但这里做双重保险
            if (CanUseAutoTheme)
            {
                _wallpaperService.SetAutoColorization(IsAutoThemeColor);
            }

            if (SelectedMedia.Type == MediaType.Image)
            {
                _wallpaperService.SetStaticWallpaper(SelectedMedia.FilePath, SelectedMonitor?.Index ?? -1);
            }
            else if (SelectedMedia.Type == MediaType.Video)
            {
                // 调用服务播放视频
                _wallpaperService.ApplyVideoWallpaper(SelectedMedia.FilePath, SelectedMonitor?.Index ?? -1);
            }
            else if (SelectedMedia.Type == MediaType.Interactive)
            {
                _wallpaperService.ApplyInteractiveWallpaper(SelectedMedia, SelectedMonitor?.Index ?? -1);
            }
        }
    }
}