# Coding Conventions

**Analysis Date:** 2026-03-14

## Naming Patterns

### Files
- **Classes:** PascalCase matching class name (e.g., `WallpaperService.cs`, `MainViewModel.cs`)
- **Views:** Suffix with type (e.g., `MainWindow.xaml`, `InteractiveUiWindow.xaml`, `WallpaperView.xaml`)
- **ViewModels:** Suffix with `ViewModel` (e.g., `WallpaperViewModel.cs`, `CreatorViewModel.cs`)
- **Converters:** Descriptive name with `Converter` suffix (e.g., `BooleanToVisibilityConverter.cs`)
- **Services:** Suffix with `Service` (e.g., `WallpaperService.cs`, `MouseHookService.cs`)

### Types
- **Classes:** PascalCase (e.g., `WallpaperService`, `MediaItem`, `InteractiveConfig`)
- **Enums:** PascalCase, single word or compound (e.g., `MediaType`, `TriggerType`, `NavigationItem`)
- **Enum values:** PascalCase (e.g., `Head`, `Body`, `Special`, `Image`, `Video`, `Interactive`)
- **Interfaces:** Prefix with `I` (e.g., `IServiceProvider` from DI)

### Methods
- **Public methods:** PascalCase (e.g., `ApplyInteractiveWallpaper()`, `RefreshMetadataAsync()`)
- **Private methods:** PascalCase with `_` prefix for fields (e.g., `CreateHiddenVideoWindow()`, `ProcessMouseMove()`)
- **Event handlers:** Prefix with `On` + descriptive name (e.g., `OnButtonMouseEnter`, `OnWindowClosed`)
- **Async methods:** Suffix with `Async` (e.g., `RefreshMetadataAsync()`, `FadeWindowAsync()`)

### Variables and Fields
- **Private fields:** CamelCase with `_` prefix (e.g., `_wallpaperService`, `_idleVideoWindow`, `_mouseHook`)
- **Constructor parameters:** Same name as field without prefix (e.g., `WallpaperService wpService`)
- **Local variables:** CamelCase (e.g., `videoPath`, `screenPoint`, `trigger`)
- **Constants:** PascalCase without underscore (e.g., `MetaFileName`, `InteractiveConfigName`)
- **Static readonly:** Same as constants (e.g., `HWND_BOTTOM`, `HWND_TOP`)

### Properties (MVVM Pattern)
- **Observable properties:** Use `[ObservableProperty]` attribute from CommunityToolkit.Mvvm
  - Source generator creates property from field (e.g., `_currentView` → `CurrentView`)
  - Fields use `_` prefix with PascalCase base name
- **Computed properties:** PascalCase, readonly (e.g., `CanUseAutoTheme`)
- **Event declarations:** PascalCase with `?` nullable (e.g., `OnMouseClick`, `OnTriggerHoverStart`)

## Code Organization

### Namespace Structure
```
ProductivityWallpaper/
├── Services/       # Business logic, external integrations
├── ViewModels/     # MVVM ViewModels (data binding)
├── Views/          # WPF XAML views
├── Models/         # Data models and configuration
├── Converters/     # WPF value converters
└── Resources/      # XAML resources and themes
```

### Class Organization (Inside Files)
1. Constants and readonly fields
2. Private fields
3. Public properties (or `[ObservableProperty]` fields)
4. Constructor
5. Public methods
6. Private helper methods
7. Event handlers (at end or in `#region`)

### Using Statements Order
```csharp
// 1. System namespaces
using System;
using System.IO;
using System.Threading.Tasks;

// 2. Third-party libraries
using LibVLCSharp.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

// 3. Local project namespaces
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
```

## WPF/XAML Conventions

### View-ViewModel Binding
- **DataTemplates:** Defined in `App.xaml` for type-based view resolution
```xml
<DataTemplate DataType="{x:Type vm:WallpaperViewModel}">
    <views:WallpaperView/>
</DataTemplate>
```

### XAML Structure
- Root element: `Window` or `UserControl` with full namespace
- xmlns ordering: default, x, local, then external
- Use `x:Name` only when code-behind access needed

### Converters
- Register as resources in `App.xaml`
- Naming: `[Source]To[Target]Converter`
- Implement `IValueConverter` with both `Convert` and `ConvertBack`
- Available converters in `Converters/`:
  - `BooleanToVisibilityConverter`
  - `InverseBooleanToVisibilityConverter`
  - `StringEmptyToVisibilityConverter`
  - `BooleanToBrushConverter`
  - `BooleanToTabStyleConverter`
  - `BooleanToFeatureButtonConverter`
  - `CountToVisibilityConverter`

## MVVM Pattern (CommunityToolkit.Mvvm)

### ViewModel Pattern
```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _propertyName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputedProperty))]
    private int _otherProperty;

    public bool ComputedProperty => OtherProperty > 0;

    [RelayCommand]
    private void ExecuteAction() { }
}
```

### Command Pattern
- Use `[RelayCommand]` attribute for automatic generation
- CanConvert pattern: `CanExecute` logic in property
- Async commands supported: `[RelayCommand]` on async methods

## Async/Await Patterns

### Method Signatures
- Always suffix async methods with `Async`
- Return `Task` for void-like, `Task<T>` for value-returning
- Avoid `async void` except for event handlers

### Patterns Used
```csharp
// Fire-and-forget with proper exception handling
await Task.Run(() => { /* background work */ });

// UI thread marshaling
Application.Current.Dispatcher.Invoke(() => { /* UI updates */ });

// Async with timeout/animation
await Task.Delay(100);

// TaskCompletionSource for animation callbacks
var tcs = new TaskCompletionSource<bool>();
anim.Completed += (s, e) => tcs.SetResult(true);
return tcs.Task;
```

### Error Handling in Async
- Use `try/catch` blocks within async methods
- Silent failure pattern: `catch { }` for non-critical operations
- Debug output for diagnostics: `System.Diagnostics.Debug.WriteLine()`

## Event Handling Patterns

### Event Declaration
```csharp
// Nullable event with Action<T>
public event Action<System.Windows.Point>? OnMouseClick;
public event Action<InteractionTrigger>? OnTriggerHoverStart;
```

### Event Subscription (Lambda Pattern)
```csharp
// Service layer subscription
_currentUiWindow.OnTriggerClicked += (actionVideoName) =>
{
    var actionPath = Path.Combine(item.FilePath, actionVideoName);
    if (File.Exists(actionPath)) PlayActionVideo(actionPath);
};

// Unsubscribe via -= or use WeakEvent pattern (not currently used)
```

### Event Handler Naming
- UI events: `On[Element][Event]` (e.g., `OnButtonMouseEnter`)
- Window lifecycle: `OnWindowClosed`, `OnStartup`
- Custom events: `On[Noun][Verb]` (e.g., `OnTriggerHoverStart`)

## P/Invoke Conventions

### Native API Wrappers
- Located in `Services/Win32Api.cs`
- Static class with static methods
- Use `IntPtr` for handles
- Constants as `public const int` or `public static readonly IntPtr`

```csharp
[DllImport("user32.dll")]
public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

public const int GWL_STYLE = -16;
public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
```

## Code Style

### Comments
- **Section headers:** `// --- Section Name ---`
- **Implementation notes:** Inline comments for complex logic
- **Critical fixes:** Mark with context (e.g., `// [核心修复] 将尺寸设置为 1x1 像素`)
- **Chinese comments:** Allowed for domain-specific concepts

### Regions
- Use `#region Name` / `#endregion` for related method groups:
  - `#region Hover Logic`
  - `#region Bubble UI`
  - `#region Helper Methods`

### Nullable Reference Types
- Project uses `<Nullable>enable</Nullable>`
- Use `?` for nullable reference types: `MediaItem?`, `InteractionTrigger?`
- Use `string.Empty` instead of `null` for default strings

### String Handling
- Prefer `string.Empty` over `""`
- Use `Path.Combine()` for file paths
- Use interpolated strings for simple concatenation
- Use `string.IsNullOrEmpty()` and `string.IsNullOrWhiteSpace()`

## Dependency Injection

### Service Registration (App.xaml.cs)
```csharp
// Singleton services
services.AddSingleton<WallpaperService>();
services.AddSingleton<ConfigService>();

// ViewModels
services.AddSingleton<MainViewModel>();

// Views
services.AddSingleton<MainWindow>();
```

### Constructor Injection Pattern
```csharp
public partial class WallpaperViewModel : ObservableObject
{
    private readonly WallpaperService _wallpaperService;
    private readonly ConfigService _configService;

    public WallpaperViewModel(WallpaperService wpService, ConfigService configService)
    {
        _wallpaperService = wpService;
        _configService = configService;
    }
}
```

## Error Handling

### Silent Failure Pattern
```csharp
try
{
    // Operation that might fail
}
catch
{
    // Use default or skip
}
```

### Debug Logging
```csharp
System.Diagnostics.Debug.WriteLine($"Hover started on {trigger.Type}: {trigger.HoverText}");
```

### Resource Cleanup
```csharp
// IDisposable pattern
public void Dispose()
{
    Stop();
}

// Cleanup with null checks
if (_idleVideoWindow != null)
{
    try { _idleVideoWindow.StopAndClose(); } catch { }
    _idleVideoWindow = null;
}
```

---

*Convention analysis: 2026-03-14*
