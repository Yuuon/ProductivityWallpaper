using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Application = System.Windows.Application;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private object _currentView;

        [ObservableProperty]
        private string _windowTitle = "Productivity Wallpaper";

        [ObservableProperty]
        private NavigationItem _currentNavigation = NavigationItem.Workshop;

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // Default View
            NavigateToWorkshopCommand.Execute(null);
        }

        // New Navigation Commands
        [RelayCommand]
        public void NavigateToWorkshop()
        {
            CurrentView = _serviceProvider.GetRequiredService<WorkshopViewModel>();
            CurrentNavigation = NavigationItem.Workshop;
        }

        [RelayCommand]
        public void NavigateToMyThemes()
        {
            CurrentView = _serviceProvider.GetRequiredService<MyThemeViewModel>();
            CurrentNavigation = NavigationItem.MyThemes;
        }

        [RelayCommand]
        public void NavigateToCreator()
        {
            CurrentView = _serviceProvider.GetRequiredService<CreatorViewModel>();
            CurrentNavigation = NavigationItem.Creator;
        }

        // Legacy Navigation Commands
        [RelayCommand]
        public void NavigateToWallpaper() => CurrentView = _serviceProvider.GetRequiredService<WallpaperViewModel>();

        [RelayCommand]
        public void NavigateToAiTool() => CurrentView = _serviceProvider.GetRequiredService<AiToolViewModel>();

        [RelayCommand]
        public void NavigateToSettings()
        {
            CurrentView = _serviceProvider.GetRequiredService<SettingsViewModel>();
            CurrentNavigation = NavigationItem.Settings;
        }
        
        // Window Control Commands
        [RelayCommand]
        public void Minimize()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
        
        [RelayCommand]
        public void Close()
        {
            Application.Current.MainWindow.Close();
        }
        
        [RelayCommand]
        public void OpenSettings()
        {
            NavigateToSettingsCommand.Execute(null);
        }
    }
}
