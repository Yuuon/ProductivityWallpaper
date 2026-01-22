using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ProductivityWallpaper.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly LocalizationService _locService;

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

        public SettingsViewModel(LocalizationService locService)
        {
            _locService = locService;
            _selectedLanguage = _locService.CurrentLanguageCode;
        }
    }
}