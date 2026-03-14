# Technology Stack

**Analysis Date:** 2026-03-14

## Languages

**Primary:**
- C# 12 - All application logic, services, and ViewModels (`ProductivityWallpaper.csproj` line 7: `<ImplicitUsings>enable</ImplicitUsings>`)

**Secondary:**
- XAML - UI layouts and styling (WPF)
- JSON - Configuration files (`user_config.json`, localization files, metadata)

## Runtime

**Environment:**
- .NET 8.0 (TargetFramework: net8.0-windows) (`ProductivityWallpaper.csproj` line 5)
- Windows-specific build with WPF support

**Package Manager:**
- NuGet (standard .NET package manager)
- Lockfile: Not present (packages restored via `dotnet restore`)

## Frameworks

**Core:**
- WPF (Windows Presentation Foundation) - Desktop UI framework (`ProductivityWallpaper.csproj` line 8: `<UseWPF>true</UseWPF>`)
- Windows Forms - Used for thumbnail generation (`ProductivityWallpaper.csproj` line 9: `<UseWindowsForms>true</UseWindowsForms>`)
- MVVM - Model-View-ViewModel pattern via CommunityToolkit.Mvvm

**Design Pattern:**
- MVVM (Model-View-ViewModel) with CommunityToolkit.Mvvm 8.4.0
  - ObservableObject base class (`WallpaperModuleModel.cs` line 1)
  - RelayCommand for ICommand implementation

**Testing:**
- Not detected - No test framework references found

**Build/Dev:**
- `dotnet build` - Standard .NET SDK build
- MSBuild - Implicit via SDK-style project

## Key Dependencies

**Critical Media Handling:**
- `LibVLCSharp` 3.9.5 - VLC media player .NET bindings (`ProductivityWallpaper.csproj` line 14)
- `LibVLCSharp.WPF` 3.9.5 - WPF-specific video rendering host (`ProductivityWallpaper.csproj` line 15)
- `VideoLAN.LibVLC.Windows` 3.0.23 - VLC native runtime for Windows (`ProductivityWallpaper.csproj` line 19)

**MVVM Infrastructure:**
- `CommunityToolkit.Mvvm` 8.4.0 - MVVM pattern implementation (ObservableObject, RelayCommand, etc.) (`ProductivityWallpaper.csproj` line 13)

**Dependency Injection:**
- `Microsoft.Extensions.DependencyInjection` 10.0.2 - Service container and DI (`ProductivityWallpaper.csproj` line 16)

**Data/Graphics:**
- `System.Drawing.Common` 10.0.2 - GDI+ wrapper for thumbnail generation (`ProductivityWallpaper.csproj` line 17)
- `System.Text.Json` 10.0.2 - JSON serialization for config and metadata (`ProductivityWallpaper.csproj` line 18)

## Configuration

**Environment:**
- JSON-based configuration (no .env file pattern)
- `user_config.json` - User preferences (last library path, media library path) (`ConfigService.cs` line 14)
- `config.wallpaper` - Interactive wallpaper configurations (per-folder JSON)
- `.wp_meta.json` - Cached library metadata

**Localization:**
- JSON-based localization files in `Resources/Languages/`
  - `en-US.json` - English translations
  - `zh-CN.json` - Chinese (Simplified) translations
  - Auto-detection via `CultureInfo.CurrentCulture.Name` (`LocalizationService.cs` line 53)

**Build:**
- SDK-style project file (`ProductivityWallpaper.csproj`)
- No custom build scripts detected

## Platform Requirements

**Development:**
- .NET 8.0 SDK
- Windows 10/11 (uses P/Invoke to Windows APIs)
- VLC runtime (automatically included via VideoLAN.LibVLC.Windows NuGet)

**Production:**
- Windows 10/11 x64
- Desktop composition enabled (for transparent windows)
- WorkerW desktop layer access (for wallpaper injection)

---

*Stack analysis: 2026-03-14*
