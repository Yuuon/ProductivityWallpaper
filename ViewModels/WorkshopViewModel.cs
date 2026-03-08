using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ProductivityWallpaper.ViewModels
{
    public partial class WorkshopViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchText = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<string> _resolutions = new()
        {
            "All Resolutions", "1920x1080", "2560x1440", "3840x2160"
        };
        
        [ObservableProperty]
        private string _selectedResolution = "All Resolutions";
        
        [ObservableProperty]
        private ObservableCollection<string> _sortOptions = new()
        {
            "Most Popular", "Latest", "Name A-Z"
        };
        
        [ObservableProperty]
        private string _selectedSortOption = "Most Popular";
        
        [ObservableProperty]
        private ObservableCollection<ThemeItem> _themeItems = new();
        
        [ObservableProperty]
        private ThemeItem? _selectedTheme;
        
        [ObservableProperty]
        private bool _hasSelectedTheme;
        
        public WorkshopViewModel()
        {
            // Load mock data
            LoadMockData();
        }
        
        private void LoadMockData()
        {
            for (int i = 0; i < 9; i++)
            {
                ThemeItems.Add(new ThemeItem
                {
                    Name = $"Theme {i + 1}",
                    Author = $"Author {i + 1}",
                    FileSize = 45.2 + i,
                    Resolution = "3840x2160",
                    Type = "Video",
                    Tags = new ObservableCollection<TagItem>
                    {
                        new TagItem { Name = "Desktop Background", IsSelected = i == 0 },
                        new TagItem { Name = "Mouse Click", IsSelected = false },
                        new TagItem { Name = "Shutdown", IsSelected = false }
                    }
                });
            }
        }
        
        [RelayCommand]
        private void SelectTheme(ThemeItem theme)
        {
            SelectedTheme = theme;
            HasSelectedTheme = theme != null;
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
    
    public partial class ThemeItem : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private string _author = string.Empty;
        
        [ObservableProperty]
        private double _fileSize;
        
        [ObservableProperty]
        private string _resolution = string.Empty;
        
        [ObservableProperty]
        private string _type = string.Empty;
        
        [ObservableProperty]
        private string _thumbnail = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<TagItem> _tags = new();
    }
    
    public partial class TagItem : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private bool _isSelected;
    }
}
