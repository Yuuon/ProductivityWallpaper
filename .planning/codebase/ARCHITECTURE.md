# Architecture

**Analysis Date:** 2026-03-14

## Pattern Overview

**Overall Pattern:** MVVM (Model-View-ViewModel) with Dependency Injection

**Key Characteristics:**
- **MVVM with CommunityToolkit.Mvvm**: Source-generated view models using `[ObservableProperty]` and `[RelayCommand]` attributes
- **Service-Oriented Architecture**: Core business logic encapsulated in singleton services
- **Layered Window System**: Three-layer window stacking for interactive wallpapers (WorkerW injection)
- **Event-Driven Communication**: Services expose events that ViewModels and Views subscribe to
- **Global Input Hooking**: Low-level mouse hooks for desktop interaction detection

## Layers

### Views Layer
- **Purpose:** UI presentation and user interaction handling
- **Location:** `Views/` directory
- **Contains:** WPF Windows (`.xaml`) and UserControls (`.xaml`)
- **Depends on:** ViewModels (via DataContext), Services (indirectly through DI)
- **Used by:** Application framework via DataTemplates

**Special Window Types:**
- `MainWindow.xaml`: Application chrome with custom title bar, navigation, and system tray integration
- `InteractiveUiWindow.xaml`: Transparent overlay for interactive hotspots (always topmost)
- `VideoPlayerWindow.xaml`: VLC-based video player for wallpaper content (bottom layer)

### ViewModels Layer
- **Purpose:** Presentation logic, state management, and command handling
- **Location:** `ViewModels/` directory
- **Contains:** ViewModel classes using CommunityToolkit.Mvvm source generators
- **Depends on:** Services via constructor injection
- **Used by:** Views via DataBinding

**Key ViewModels:**
- `MainViewModel`: Navigation management, window state commands
- `WallpaperViewModel`: Media library browsing, monitor selection, wallpaper application
- `WorkshopViewModel/CreatorViewModel/MyThemeViewModel`: Feature-specific view models

### Services Layer
- **Purpose:** Core business logic, external integration, and system operations
- **Location:** `Services/` directory
- **Contains:** Singleton service classes
- **Depends on:** Other services, Win32 API, external libraries (LibVLC)
- **Used by:** ViewModels, App startup

**Core Services:**
- `WallpaperService`: Window lifecycle management, WorkerW injection, video playback orchestration
- `MouseHookService`: Global mouse hook (WH_MOUSE_LL), click detection, sweep gesture detection
- `ConfigService`: JSON-based user configuration persistence
- `LocalizationService`: Runtime language switching via JSON resource files
- `Win32Api`: P/Invoke declarations for Windows API interop

### Models Layer
- **Purpose:** Data structures and domain entities
- **Location:** `Models/` directory
- **Contains:** POCOs, enums, and configuration models
- **Depends on:** None (pure data)
- **Used by:** Services, ViewModels, Views

**Key Models:**
- `MediaItem`: Represents wallpaper content (image, video, interactive package)
- `InteractiveConfig`: Configuration for interactive wallpapers (hotspots, triggers)
- `InteractionTrigger`: Individual interactive hotspot definition
- `LibraryMetadata`: Media library cache structure

## Data Flow

### Wallpaper Application Flow (Interactive):

1. **User Selection** (`WallpaperViewModel.ApplyWallpaper`)
   → User clicks "Use Now" button
   
2. **Service Invocation** (`WallpaperService.ApplyInteractiveWallpaper`)
   → Cleanup existing wallpaper → Parse config.wallpaper
   
3. **Window Creation**
   - `CreateHiddenVideoWindow()`: Creates 1x1 pixel off-screen window
   - `InteractiveUiWindow`: Transparent overlay with hotspot buttons
   
4. **WorkerW Injection** (`InjectInteractiveLayers`)
   → Find WorkerW window → SetParent() to attach windows
   
5. **Z-Order Management**
   - Idle Video Layer: `HWND_BOTTOM` (desktop icons below)
   - Action Video Layer: Dynamic creation on trigger
   - UI Layer: `HWND_TOP` (always above video)
   
6. **Input Handling**
   - `MouseHookService.Start()`: Install global hook
   - Click events → `SimulateClickIfHit()` collision detection
   - Hover events → Bubble UI display
   - Sweep detection → Speed threshold triggering

### Navigation Flow:

1. **Command Trigger** (`[RelayCommand]` in ViewModel)
   → `NavigateToWorkshopCommand.Execute()`
   
2. **ViewModel Resolution** (`MainViewModel`)
   → `_serviceProvider.GetRequiredService<WorkshopViewModel>()`
   
3. **View Resolution** (DataTemplate in `App.xaml`)
   ```xml
   <DataTemplate DataType="{x:Type vm:WorkshopViewModel}">
       <views:WorkshopView/>
   </DataTemplate>
   ```
   
4. **UI Update**
   → `ContentControl.Content` bound to `CurrentView` updates
   → View instantiated and displayed

## Key Abstractions

### WorkerW Injection System
- **Purpose:** Mount WPF windows behind desktop icons
- **Location:** `Services/WallpaperService.cs` (lines 555-579)
- **Pattern:** P/Invoke window hierarchy manipulation
- **Key Operations:**
  - `FindWorkerW()`: Locates the Windows WorkerW window
  - `SetParent()`: Attaches WPF windows as child of WorkerW
  - `SetWindowPos()`: Controls Z-order layering

### Window Anti-Flicker Pattern
- **Purpose:** Prevent visible window initialization artifacts
- **Location:** `Services/WallpaperService.cs` (lines 393-414)
- **Pattern:** Off-screen initialization sequence
- **Sequence:**
  1. Set `Width=1, Height=1`
  2. Set `Left=-32000, Top=-32000`
  3. Call `Show()`
  4. Inject to WorkerW
  5. Buffer delay (100ms)
  6. Stretch to full screen with `SetWindowPos()`

### Global Input Hook
- **Purpose:** Capture desktop mouse events despite WorkerW attachment
- **Location:** `Services/MouseHookService.cs`
- **Pattern:** Windows low-level hook with coordinate transformation
- **Callbacks:** `OnMouseClick`, `OnMouseSweep`

### MVVM Source Generation
- **Purpose:** Reduce boilerplate for INotifyPropertyChanged
- **Location:** All ViewModels
- **Pattern:** CommunityToolkit.Mvvm attributes
- **Example:**
  ```csharp
  [ObservableProperty]
  private object _currentView;
  
  [RelayCommand]
  public void NavigateToWorkshop() { ... }
  ```

## Entry Points

### Application Entry
- **Location:** `App.xaml.cs`
- **Responsibilities:**
  - DI container configuration (`ConfigureServices()`)
  - VLC core initialization (`Core.Initialize()`)
  - Service initialization sequence
  - MainWindow display

### Main Window
- **Location:** `MainWindow.xaml` / `MainWindow.xaml.cs`
- **Responsibilities:**
  - Custom chrome window (borderless with `WindowChrome`)
  - Navigation header with gradient active states
  - System tray integration (`NotifyIcon`)
  - View container (`ContentControl`)

### Interactive Hotspot Entry
- **Location:** `Views/InteractiveUiWindow.xaml.cs`
- **Responsibilities:**
  - Dynamic button generation from config
  - Hit-testing for mouse events
  - Hover bubble UI display
  - Event raising (`OnTriggerClicked`, `OnTriggerHoverStart/End`)

## Error Handling

**Strategy:** Try-catch with graceful degradation

**Patterns:**
- Service methods wrap external calls (VLC, file I/O) in try-catch blocks
- Config loading falls back to defaults on parse failure
- Window operations silently fail rather than crash
- Debug.WriteLine for diagnostic logging (not production logging)

**Example:** (`WallpaperService.cs` lines 49-50)
```csharp
catch { /* Use defaults if load fails */ }
```

## Cross-Cutting Concerns

**Logging:** Debug-only via `System.Diagnostics.Debug.WriteLine`

**Validation:** Minimal; assumes valid config structure

**Authentication:** Not applicable (local desktop application)

**Resource Management:**
- VLC instances disposed in `StopAndClose()`
- Mouse hooks uninstalled in `Stop()` / `Dispose()`
- NotifyIcon disposed on application exit

---

*Architecture analysis: 2026-03-14*
