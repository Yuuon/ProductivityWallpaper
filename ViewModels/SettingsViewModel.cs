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

        public SettingsViewModel(LocalizationService locService, ConfigService configService)
        {
            _locService = locService;
            _configService = configService;
            _selectedLanguage = _locService.CurrentLanguageCode;
            _mediaLibraryPath = _configService.Config.MediaLibraryPath;
        }
    }
}