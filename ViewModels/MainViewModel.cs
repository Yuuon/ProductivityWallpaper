using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ProductivityWallpaper.ViewModels
{
    // --- MAIN VIEW MODEL ---
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private object _currentView;

        [ObservableProperty]
        private string _windowTitle = "Productivity Wallpaper";

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // Default View
            NavigateToWallpaperCommand.Execute(null);
        }

        [RelayCommand]
        public void NavigateToWallpaper() => CurrentView = _serviceProvider.GetRequiredService<WallpaperViewModel>();

        [RelayCommand]
        public void NavigateToAiTool() => CurrentView = _serviceProvider.GetRequiredService<AiToolViewModel>();

        [RelayCommand]
        public void NavigateToSettings() => CurrentView = _serviceProvider.GetRequiredService<SettingsViewModel>();
    }
}