using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProductivityWallpaper.ViewModels
{
    public partial class CreatorViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isWelcomePage = true;
        
        [ObservableProperty]
        private bool _isCreatingPage;
        
        [ObservableProperty]
        private string _newThemeName = string.Empty;
        
        [ObservableProperty]
        private string _currentThemeName = string.Empty;
        
        [ObservableProperty]
        private bool _isEditingThemeName;
        
        // Feature selection states
        [ObservableProperty]
        private bool _isThemePreviewSelected = true;
        
        [ObservableProperty]
        private bool _isDesktopBackgroundSelected;
        
        [ObservableProperty]
        private bool _isMouseClickSelected;
        
        [ObservableProperty]
        private bool _isShutdownSelected;
        
        [ObservableProperty]
        private bool _isBootRestartSelected;
        
        [ObservableProperty]
        private bool _isScreenWakeSelected;
        
        [ObservableProperty]
        private bool _isOpenAppSelected;
        
        [ObservableProperty]
        private bool _isDesktopClockSelected;
        
        [ObservableProperty]
        private bool _isPomodoroSelected;
        
        [ObservableProperty]
        private bool _isAnniversarySelected;
        
        // Feature counts
        [ObservableProperty]
        private int _desktopBackgroundCount;
        
        [ObservableProperty]
        private int _mouseClickCount;
        
        [ObservableProperty]
        private int _shutdownCount;
        
        [ObservableProperty]
        private int _bootRestartCount;
        
        [ObservableProperty]
        private bool _hasPreviewContent;
        
        [ObservableProperty]
        private object? _previewContent;
        
        [ObservableProperty]
        private object? _configurationContent;
        
        [RelayCommand]
        private void StartCreating()
        {
            // Use user input if provided, otherwise use default placeholder text
            CurrentThemeName = string.IsNullOrWhiteSpace(NewThemeName) 
                ? "输入主题名字..." 
                : NewThemeName.Trim();
            
            IsWelcomePage = false;
            IsCreatingPage = true;
            SelectFeature("ThemePreview");
        }
        
        [RelayCommand]
        private void BackToWelcome()
        {
            IsWelcomePage = true;
            IsCreatingPage = false;
            NewThemeName = string.Empty;
        }
        
        [RelayCommand]
        private void SelectFeature(string featureName)
        {
            // Reset all selections
            IsThemePreviewSelected = false;
            IsDesktopBackgroundSelected = false;
            IsMouseClickSelected = false;
            IsShutdownSelected = false;
            IsBootRestartSelected = false;
            IsScreenWakeSelected = false;
            IsOpenAppSelected = false;
            IsDesktopClockSelected = false;
            IsPomodoroSelected = false;
            IsAnniversarySelected = false;
            
            // Set selected feature
            switch (featureName)
            {
                case "ThemePreview":
                    IsThemePreviewSelected = true;
                    break;
                case "DesktopBackground":
                    IsDesktopBackgroundSelected = true;
                    break;
                case "MouseClick":
                    IsMouseClickSelected = true;
                    break;
                case "Shutdown":
                    IsShutdownSelected = true;
                    break;
                case "BootRestart":
                    IsBootRestartSelected = true;
                    break;
                case "ScreenWake":
                    IsScreenWakeSelected = true;
                    break;
                case "OpenApp":
                    IsOpenAppSelected = true;
                    break;
                case "DesktopClock":
                    IsDesktopClockSelected = true;
                    break;
                case "Pomodoro":
                    IsPomodoroSelected = true;
                    break;
                case "Anniversary":
                    IsAnniversarySelected = true;
                    break;
            }
            
            // Load preview and configuration content
            LoadFeatureContent(featureName);
        }
        
        [RelayCommand]
        private void ToggleEditThemeName()
        {
            IsEditingThemeName = true;
        }
        
        [RelayCommand]
        private void FinishEditThemeName()
        {
            IsEditingThemeName = false;
        }
        
        private void LoadFeatureContent(string featureName)
        {
            // Load appropriate content based on feature
            // This is a placeholder - actual implementation would load specific user controls
            HasPreviewContent = false;
        }
    }
}
