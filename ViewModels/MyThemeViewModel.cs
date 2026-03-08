using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ProductivityWallpaper.ViewModels
{
    public partial class MyThemeViewModel : ObservableObject
    {
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
        private bool _hasContent = true;
        
        [ObservableProperty]
        private string _emptyMessage = "Nothing here yet\nGo find themes you like~";
        
        [ObservableProperty]
        private string _actionButtonText = "Browse Workshop";
        
        public MyThemeViewModel()
        {
            LoadMockData();
        }
        
        private void LoadMockData()
        {
            // Mock data - in real app this would come from local storage
            for (int i = 0; i < 6; i++)
            {
                ThemeItems.Add(new ThemeItem
                {
                    Name = $"My Theme {i + 1}",
                    Author = "You",
                    FileSize = 30.5 + i,
                    Resolution = "1920x1080",
                    Type = "Video"
                });
            }
        }
        
        [RelayCommand]
        private void ShowUsageHistory()
        {
            IsUsageHistorySelected = true;
            IsMyWorksSelected = false;
            EmptyMessage = "Nothing here yet\nGo find themes you like~";
            ActionButtonText = "Browse Workshop";
            HasContent = ThemeItems.Count > 0;
        }
        
        [RelayCommand]
        private void ShowMyWorks()
        {
            IsUsageHistorySelected = false;
            IsMyWorksSelected = true;
            EmptyMessage = "Nothing here yet\nGo create your own theme~";
            ActionButtonText = "Start Creating";
            HasContent = ThemeItems.Count > 0;
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
                // Navigate to Workshop
            }
            else
            {
                // Navigate to Creator
            }
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
}
