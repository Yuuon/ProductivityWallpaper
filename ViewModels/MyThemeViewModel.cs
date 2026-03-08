using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ProductivityWallpaper.ViewModels
{
    public partial class MyThemeViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        
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
        
        public MyThemeViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            // ThemeItems will be loaded from local storage in the future
            // For now, start with empty collection to show empty state
            // Initialize with correct localized strings
            ShowUsageHistory();
        }
        
        [RelayCommand]
        private void ShowUsageHistory()
        {
            IsUsageHistorySelected = true;
            IsMyWorksSelected = false;
            // Get localized strings from application resources
            EmptyMessage = System.Windows.Application.Current.TryFindResource("MyThemes_EmptyHistoryMessage") as string 
                ?? "Nothing here yet\nGo find themes you like~";
            ActionButtonText = System.Windows.Application.Current.TryFindResource("MyThemes_GoToWorkshop") as string 
                ?? "Browse Workshop";
            // HasContent should reflect actual usage history data (currently empty)
            HasContent = false;
        }
        
        [RelayCommand]
        private void ShowMyWorks()
        {
            IsUsageHistorySelected = false;
            IsMyWorksSelected = true;
            // Get localized strings from application resources
            EmptyMessage = System.Windows.Application.Current.TryFindResource("MyThemes_EmptyWorksMessage") as string 
                ?? "Nothing here yet\nGo create your own theme~";
            ActionButtonText = System.Windows.Application.Current.TryFindResource("MyThemes_GoCreate") as string 
                ?? "Start Creating";
            // HasContent reflects user's created themes
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
                _mainViewModel.NavigateToWorkshopCommand.Execute(null);
            }
            else
            {
                // Navigate to Creator
                _mainViewModel.NavigateToCreatorCommand.Execute(null);
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
