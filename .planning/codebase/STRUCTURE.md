# Codebase Structure

**Analysis Date:** 2026-03-14

## Directory Layout

```
ProductivityWallpaper/
├── .planning/
│   └── codebase/           # Architecture documentation
│       ├── ARCHITECTURE.md
│       └── STRUCTURE.md
├── Converters/             # WPF Value Converters
│   ├── BooleanToBrushConverter.cs
│   ├── BooleanToStyleConverter.cs
│   └── BooleanToVisibilityConverter.cs
├── Models/                 # Data models and enums
│   ├── AIModuleModel.cs
│   ├── NavigationItem.cs
│   └── WallpaperModuleModel.cs
├── Services/               # Business logic and system integration
│   ├── ConfigService.cs
│   ├── LocalizationService.cs
│   ├── MouseHookService.cs
│   ├── WallpaperService.cs
│   └── Win32Api.cs
├── ViewModels/             # MVVM ViewModels (CommunityToolkit)
│   ├── AiToolViewModel.cs
│   ├── CreatorViewModel.cs
│   ├── MainViewModel.cs
│   ├── MyThemeViewModel.cs
│   ├── PromptSegmentViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── WallpaperViewModel.cs
│   └── WorkshopViewModel.cs
├── Views/                  # WPF Views (UserControls and Windows)
│   ├── AiToolView.xaml
│   ├── CreatorView.xaml
│   ├── InteractiveUiWindow.xaml      # Transparent overlay
│   ├── MyThemeView.xaml
│   ├── SettingsView.xaml
│   ├── VideoPlayerWindow.xaml        # VLC video player
│   ├── WallpaperView.xaml
│   └── WorkshopView.xaml
├── Resources/              # Application resources
│   ├── Languages/          # JSON localization files
│   │   ├── en-US.json
│   │   └── zh-CN.json
│   ├── Theme.xaml          # Colors, brushes, control styles
│   └── Images/             # Default thumbnails (referenced)
├── ReferenceResources/     # Development reference (excluded from build)
│   └── exampleWindow.xaml
├── App.xaml                # Application resources and DataTemplates
├── App.xaml.cs             # Entry point and DI configuration
├── MainWindow.xaml         # Main application window
├── MainWindow.xaml.cs      # System tray and window chrome
├── ProductivityWallpaper.csproj
└── AssemblyInfo.cs
```

## Directory Purposes

**Converters/**
- **Purpose:** WPF value converters for XAML data binding transformations
- **Contains:** `IValueConverter` implementations
- **Key files:** `BooleanToVisibilityConverter.cs` (includes `InverseBooleanToVisibilityConverter`, `StringEmptyToVisibilityConverter`)

**Models/**
- **Purpose:** Data structures, domain entities, and configuration objects
- **Contains:** POCOs, enums, and metadata classes
- **Key files:**
  - `WallpaperModuleModel.cs`: `MediaItem`, `InteractiveConfig`, `InteractionTrigger`, `LibraryMetadata`
  - `NavigationItem.cs`: Navigation enum

**Services/**
- **Purpose:** Core business logic, external integrations, and system operations
- **Contains:** Singleton services registered in DI container
- **Key files:**
  - `WallpaperService.cs` (654 lines): Window management, WorkerW injection, video orchestration
  - `MouseHookService.cs` (188 lines): Global mouse hook and gesture detection
  - `ConfigService.cs` (66 lines): JSON configuration persistence
  - `LocalizationService.cs` (255 lines): Runtime localization
  - `Win32Api.cs` (146 lines): P/Invoke declarations

**ViewModels/**
- **Purpose:** Presentation logic with CommunityToolkit.Mvvm source generators
- **Contains:** ViewModel classes with `[ObservableProperty]` and `[RelayCommand]`
- **Key files:**
  - `MainViewModel.cs`: Navigation management
  - `WallpaperViewModel.cs`: Media library and wallpaper application
  - `CreatorViewModel.cs`: Theme creation workflow

**Views/**
- **Purpose:** UI layout and visual presentation
- **Contains:** WPF Windows (`.xaml`) and UserControls (`.xaml`)
- **Key files:**
  - `MainWindow.xaml`: Application shell with custom chrome
  - `InteractiveUiWindow.xaml`: Transparent hotspot overlay
  - `VideoPlayerWindow.xaml`: VLC video player host
  - `WorkshopView.xaml`, `CreatorView.xaml`, `MyThemeView.xaml`: Feature views

**Resources/**
- **Purpose:** Application-wide resources and configuration
- **Contains:** XAML resource dictionaries, localization files
- **Key files:**
  - `Theme.xaml`: Color palette, brushes, control styles
  - `Languages/*.json`: Runtime localization strings

## Key File Locations

### Entry Points
- `App.xaml.cs`: Application startup, DI configuration, VLC initialization
- `MainWindow.xaml`: Primary application window

### Configuration
- `ProductivityWallpaper.csproj`: Project configuration, NuGet packages
- `user_config.json`: Runtime user preferences (created by `ConfigService`)
- `Resources/Theme.xaml`: Visual theme definition

### Core Logic
- `Services/WallpaperService.cs`: Primary wallpaper management service
- `Services/MouseHookService.cs`: Input capture and gesture detection
- `Services/Win32Api.cs`: Windows API interop definitions

### Models
- `Models/WallpaperModuleModel.cs`: All wallpaper-related data structures

### Testing
- **Not detected:** No test projects or test files found

## Naming Conventions

### Files
- **Views:** `{Name}View.xaml` for UserControls, `{Name}Window.xaml` for Windows
- **ViewModels:** `{Name}ViewModel.cs`
- **Services:** `{Name}Service.cs`
- **Models:** `{Name}Model.cs` or `{Name}.cs` for simple types
- **Converters:** `{Function}Converter.cs`

### Directories
- **PascalCase:** `Views`, `ViewModels`, `Services`, `Converters`
- **Singular:** `Models` (not `Models`)

### XAML Resources
- **Brushes:** `{Description}Brush` (e.g., `BackgroundMainBrush`)
- **Colors:** `{Description}Color` (e.g., `PrimaryGradientStartColor`)
- **Styles:** `{Target}{State}Style` (e.g., `PrimaryButtonStyle`, `ActiveTabButtonStyle`)
- **Converters:** Registered in `App.xaml` with keys like `BooleanToVisibilityConverter`

## Where to Add New Code

### New Feature (e.g., New View)
1. **Create View:** `Views/{Feature}View.xaml`
2. **Create ViewModel:** `ViewModels/{Feature}ViewModel.cs`
3. **Register ViewModel:** Add to DI in `App.xaml.cs` `ConfigureServices()`
4. **Add DataTemplate:** Add to `App.xaml` resources
5. **Add Navigation:** Add command and navigation logic to `MainViewModel.cs`
6. **Add Strings:** Add localization keys to `LocalizationService.cs`

### New Service
1. **Create Service:** `Services/{Name}Service.cs`
2. **Register Service:** Add singleton registration in `App.xaml.cs`
3. **Inject:** Add constructor parameter to consuming ViewModels

### New Model
1. **Add to existing file:** Extend `Models/WallpaperModuleModel.cs` for related concepts
2. **Or create new:** `Models/{Name}.cs` for standalone entities

### New Converter
1. **Create:** `Converters/{Function}Converter.cs` implementing `IValueConverter`
2. **Register:** Add to `App.xaml` resources with `x:Key`
3. **Use:** Reference in XAML via `{StaticResource Key}`

### New Interactive Feature
1. **Extend Model:** Add properties to `InteractiveConfig` or `InteractionTrigger` in `Models/WallpaperModuleModel.cs`
2. **Update UI:** Modify `InteractiveUiWindow.xaml.cs` for new interactions
3. **Handle Events:** Subscribe to new events in `WallpaperService.ApplyInteractiveWallpaper()`

## Special Directories

**ReferenceResources/**
- **Purpose:** Development reference materials (excluded from build)
- **Excluded:** Via `.csproj` `<ItemGroup>` with `Remove` directives
- **Committed:** Yes (development aid)

**Resources/Languages/**
- **Purpose:** Runtime localization files
- **Generated:** No (static JSON files)
- **Auto-created:** Default language files created on first run if missing

**.wp_cache/**
- **Purpose:** Thumbnail cache for media library
- **Generated:** Yes (created by `WallpaperService`)
- **Committed:** No (runtime data)

---

*Structure analysis: 2026-03-14*
