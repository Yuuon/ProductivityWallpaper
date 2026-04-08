using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;

namespace ProductivityWallpaper.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly LocalizationService _locService;
        private readonly ConfigService _configService;
        private readonly IThemeService _themeService;

        public List<string> Languages => _locService.AvailableLanguages;

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    _locService.LoadLanguage(value);
                }
            }
        }

        [ObservableProperty]
        private string _mediaLibraryPath;

        /// <summary>
        /// The path where theme files are stored (%AppData%/ProductivityWallpaper/Themes/).
        /// </summary>
        public string ThemeStoragePath => _themeService.GetThemesRootPath();

        partial void OnMediaLibraryPathChanged(string value)
        {
            _configService.Config.MediaLibraryPath = value;
            _configService.Save();
        }

        [RelayCommand]
        private void BrowseMediaLibrary()
        {
            // Use current path if it exists, otherwise use default (Pictures folder)
            var initialPath = Directory.Exists(MediaLibraryPath) 
                ? MediaLibraryPath 
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择媒体库文件夹",
                SelectedPath = initialPath,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MediaLibraryPath = dialog.SelectedPath;
            }
        }

        [RelayCommand]
        private void OpenThemeFolder()
        {
            var path = ThemeStoragePath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Failed to open theme folder: {ex.Message}");
            }
        }

        public SettingsViewModel(LocalizationService locService, ConfigService configService, IThemeService themeService)
        {
            _locService = locService;
            _configService = configService;
            _themeService = themeService;
            _selectedLanguage = _locService.CurrentLanguageCode;
            _mediaLibraryPath = _configService.Config.MediaLibraryPath;
        }
    }
}