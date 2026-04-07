using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        /// </summary>
        private async Task LoadSavedThemesAsync()
        {
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
                        FileSize = 0, // Could calculate total size if needed
                        Resolution = "",
                        ThemeFolderName = name // Store for edit navigation
                    };

                    ThemeItems.Add(themeItem);
                }

                Debug.WriteLine($"[MyThemeViewModel] Loaded {ThemeItems.Count} saved themes");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[MyThemeViewModel] Error loading themes: {ex.Message}");
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
