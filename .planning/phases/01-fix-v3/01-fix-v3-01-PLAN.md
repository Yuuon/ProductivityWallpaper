---
phase: 01-fix-v3
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CreatorView.xaml
  - ViewModels/CreatorViewModel.cs
  - ViewModels/CreatorViewState.cs
  - Converters/StringToImageSourceConverter.cs
  - App.xaml
autonomous: true
requirements:
  - FIX-V3-001
  - FIX-V3-002
  - FIX-V3-003
must_haves:
  truths:
    - ContentControl displays ViewModels correctly (ContentPresenter works)
    - Single navigation state enum controls all highlighting
    - Empty PreviewImagePath no longer causes binding errors
    - All 9 features show content when clicked
  artifacts:
    - path: Views/CreatorView.xaml
      provides: Fixed ContentPresenter with explicit template resolution
      pattern: ContentPresenter.*ConfigurationContent
    - path: ViewModels/CreatorViewState.cs
      provides: Enum with 9 values for navigation state
      contains: CreatorViewState enum
    - path: ViewModels/CreatorViewModel.cs
      provides: CurrentState property and IsXXXActive computed properties
      pattern: public CreatorViewState CurrentState
    - path: Converters/StringToImageSourceConverter.cs
      provides: Handles null/empty/invalid paths for ImageSource
      pattern: IValueConverter.*ImageSource
  key_links:
    - from: CreatorViewModel.CurrentState
      to: CreatorView.xaml button bindings
      via: IsXXXActive properties
    - from: StringToImageSourceConverter
      to: DesktopClockView.xaml, PomodoroView.xaml, AnniversaryView.xaml
      via: Image.Source binding with converter
    - from: ContentPresenter.Content
      to: DataTemplates in App.xaml
      via: Type-based template resolution
---

<objective>
Fix the critical content display issue and unify navigation state management. This plan addresses the ROOT CAUSE identified in logs: ViewModels ARE being created (NavigationMonitor proves this), but ContentControl is not displaying them. Also fixes navigation state chaos by replacing multiple booleans with a single enum.

Purpose: Make Creator View actually work - users can click any feature and see content display.
Output: Working ContentPresenter, unified navigation enum, ImageSource converter.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/PROJECT.md
@.planning/ROADMAP.md
@.planning/STATE.md
@.planning/phases/01-fix-v3/01-CONTEXT.md

@E:/Projects/ProductivityWallpaper/Views/CreatorView.xaml
@E:/Projects/ProductivityWallpaper/ViewModels/CreatorViewModel.cs
@E:/Projects/ProductivityWallpaper/App.xaml
@E:/Projects/ProductivityWallpaper/Views/DesktopClockView.xaml
@E:/Projects/ProductivityWallpaper/Views/PomodoroView.xaml
@E:/Projects/ProductivityWallpaper/Views/AnniversaryView.xaml

<interfaces>
From App.xaml (existing DataTemplates):
```xml
<DataTemplate DataType="{x:Type vm:DesktopBackgroundViewModel}">
    <views:DesktopBackgroundView/>
</DataTemplate>
<DataTemplate DataType="{x:Type vm:DesktopClockViewModel}">
    <views:DesktopClockView/>
</DataTemplate>
<!-- etc for all 9 ViewModels -->
```

From CreatorViewModel.cs (current navigation state):
```csharp
[ObservableProperty]
private string _selectedFeature = "ThemePreview";

public bool IsThemePreviewSelected => SelectedFeature == "ThemePreview";
public bool IsDesktopBackgroundSelected => SelectedFeature == "DesktopBackground";
// ... etc
```

From DesktopClockView.xaml (Image binding causing errors):
```xml
<Image Source="{Binding PreviewImagePath}" ... />
```

Key insight from logs:
```
[NavigationMonitor] SUCCESS: DesktopBackground -> DesktopBackgroundViewModel
[NavigationMonitor] SUCCESS: DesktopClock -> DesktopClockViewModel
```
ViewModels ARE created. ContentControl just not displaying them.
</interfaces>
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Create CreatorViewState enum and refactor navigation</name>
  <files>ViewModels/CreatorViewState.cs, ViewModels/CreatorViewModel.cs</files>
  <behavior>
    - Test 1: CreatorViewState enum has 9 values (ThemePreview, DesktopBackground, MouseClick, DesktopClock, Pomodoro, Anniversary, Shutdown, BootRestart, ScreenWake, OpenApp)
    - Test 2: CreatorViewModel has CurrentState property with default ThemePreview
    - Test 3: IsXXXActive computed properties return true only when CurrentState matches
    - Test 4: Setting CurrentState updates exactly one IsXXXActive to true
  </behavior>
  <action>
Create `ViewModels/CreatorViewState.cs`:
```csharp
namespace ProductivityWallpaper.ViewModels
{
    public enum CreatorViewState
    {
        ThemePreview,
        DesktopBackground,
        MouseClick,
        DesktopClock,
        Pomodoro,
        Anniversary,
        Shutdown,
        BootRestart,
        ScreenWake,
        OpenApp
    }
}
```

Refactor `CreatorViewModel.cs`:
1. Add `using ProductivityWallpaper.ViewModels;` (if needed)
2. Replace `[ObservableProperty] private string _selectedFeature = "ThemePreview";` with:
   ```csharp
   [ObservableProperty]
   [NotifyPropertyChangedFor(nameof(IsThemePreviewActive))]
   [NotifyPropertyChangedFor(nameof(IsDesktopBackgroundActive))]
   [NotifyPropertyChangedFor(nameof(IsMouseClickActive))]
   [NotifyPropertyChangedFor(nameof(IsDesktopClockActive))]
   [NotifyPropertyChangedFor(nameof(IsPomodoroActive))]
   [NotifyPropertyChangedFor(nameof(IsAnniversaryActive))]
   [NotifyPropertyChangedFor(nameof(IsShutdownActive))]
   [NotifyPropertyChangedFor(nameof(IsBootRestartActive))]
   [NotifyPropertyChangedFor(nameof(IsScreenWakeActive))]
   [NotifyPropertyChangedFor(nameof(IsOpenAppActive))]
   private CreatorViewState _currentState = CreatorViewState.ThemePreview;
   ```

3. Replace all `IsXXXSelected` properties with `IsXXXActive`:
   ```csharp
   public bool IsThemePreviewActive => CurrentState == CreatorViewState.ThemePreview;
   public bool IsDesktopBackgroundActive => CurrentState == CreatorViewState.DesktopBackground;
   public bool IsMouseClickActive => CurrentState == CreatorViewState.MouseClick;
   public bool IsDesktopClockActive => CurrentState == CreatorViewState.DesktopClock;
   public bool IsPomodoroActive => CurrentState == CreatorViewState.Pomodoro;
   public bool IsAnniversaryActive => CurrentState == CreatorViewState.Anniversary;
   public bool IsShutdownActive => CurrentState == CreatorViewState.Shutdown;
   public bool IsBootRestartActive => CurrentState == CreatorViewState.BootRestart;
   public bool IsScreenWakeActive => CurrentState == CreatorViewState.ScreenWake;
   public bool IsOpenAppActive => CurrentState == CreatorViewState.OpenApp;
   ```

4. Update SelectFeature method to use enum:
   ```csharp
   [RelayCommand]
   private void SelectFeature(string featureName)
   {
       Debug.WriteLine($"[Navigation] Selecting feature: {featureName}");
       try
       {
           // Parse string to enum
           if (Enum.TryParse<CreatorViewState>(featureName, out var state))
           {
               CurrentState = state;
           }
           else
           {
               Debug.WriteLine($"[Navigation] ERROR: Unknown feature name: {featureName}");
               return;
           }
           
           // Collapse submenus for simple features and Theme Preview
           switch (featureName)
           {
               case "ThemePreview":
               case "OpenApp":
               case "DesktopClock":
               case "Pomodoro":
               case "Anniversary":
                   CollapseAllNavExcept();
                   break;
           }
           
           LoadFeatureContent(featureName);
       }
       catch (Exception ex)
       {
           NavigationMonitorService.LogNavigation(featureName, null, ex);
       }
   }
   ```

5. Update all references to `SelectedFeature` to use `CurrentState`:
   - In OnIsXXXExpandedChanged methods: compare to `CreatorViewState.XXX.ToString()`
   - In SelectScheme method: use `CurrentState.ToString()`
   - Or better: pass enum values instead of strings throughout

IMPORTANT: Keep backward compatibility where possible - SelectFeature still accepts string for CommandParameter binding, but internally converts to enum.
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>Build succeeds with no errors, CreatorViewState enum exists, CreatorViewModel has CurrentState property and IsXXXActive computed properties</done>
</task>

<task type="auto" tdd="true">
  <name>Task 2: Update CreatorView.xaml bindings to use new enum-based properties</name>
  <files>Views/CreatorView.xaml</files>
  <behavior>
    - Test 1: All Button Style bindings reference IsXXXActive (not IsXXXSelected)
    - Test 2: Theme Preview button uses IsThemePreviewActive
    - Test 3: Desktop Clock button uses IsDesktopClockActive
    - Test 4: Pomodoro button uses IsPomodoroActive
    - Test 5: Anniversary button uses IsAnniversaryActive
    - Test 6: Open App button uses IsOpenAppActive
  </behavior>
  <action>
Update all button bindings in `CreatorView.xaml`:

1. Theme Preview button (line ~214):
   ```xml
   <Button ... Style="{Binding IsThemePreviewActive, Converter={StaticResource BooleanToFeatureButtonConverter}}">
   ```

2. Open App button (line ~616):
   ```xml
   <Button ... Style="{Binding IsOpenAppActive, Converter={StaticResource BooleanToFeatureButtonConverter}}">
   ```

3. Desktop Clock button (line ~627):
   ```xml
   <Button ... Style="{Binding IsDesktopClockActive, Converter={StaticResource BooleanToFeatureButtonConverter}}">
   ```

4. Pomodoro button (line ~638):
   ```xml
   <Button ... Style="{Binding IsPomodoroActive, Converter={StaticResource BooleanToFeatureButtonConverter}}">
   ```

5. Anniversary button (line ~649):
   ```xml
   <Button ... Style="{Binding IsAnniversaryActive, Converter={StaticResource BooleanToFeatureButtonConverter}}">
   ```

Also update the expandable header highlight bindings if needed - the `IsXXXHeaderHighlighted` properties should continue to work since they check scheme selection state which is separate from CurrentState.

CRITICAL: The expandable button IsChecked bindings remain as-is (binding to IsXXXExpanded) - those control expansion state, not selection state.
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>All simple button Style bindings updated to use IsXXXActive properties, build succeeds</done>
</task>

<task type="auto" tdd="true">
  <name>Task 3: Fix ContentControl - switch to ContentPresenter with explicit template resolution</name>
  <files>Views/CreatorView.xaml</files>
  <behavior>
    - Test 1: ContentPresenter is used instead of ContentControl
    - Test 2: ContentPresenter binds to ConfigurationContent
    - Test 3: ContentPresenter has ContentTemplate="{x:Null}" to enable DataTemplate lookup
    - Test 4: Build succeeds and runs without XAML errors
  </behavior>
  <action>
Fix the content display issue in `CreatorView.xaml` (around line 777).

Current code:
```xml
<ContentControl Content="{Binding ConfigurationContent}"/>
```

Replace with:
```xml
<ContentPresenter Content="{Binding ConfigurationContent}"
                  ContentTemplate="{x:Null}"/>
```

The key fix is:
1. Use ContentPresenter instead of ContentControl (better DataTemplate resolution)
2. Explicitly set `ContentTemplate="{x:Null}"` to force WPF to look up DataTemplate by type

This ensures WPF will search App.xaml resources for a DataTemplate with DataType matching the runtime type of ConfigurationContent (e.g., DesktopBackgroundViewModel → DesktopBackgroundView).

Alternative approach (if ContentPresenter doesn't work):
```xml
<ContentControl Content="{Binding ConfigurationContent}"
                ContentTemplate="{x:Null}"/>
```

Try ContentPresenter first as it's the recommended approach for view-first scenarios.

Also verify the DataTemplates in App.xaml are correct - they should already be there from previous phases.
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>ContentControl replaced with ContentPresenter, ContentTemplate="{x:Null}" set, build succeeds</done>
</task>

<task type="auto" tdd="true">
  <name>Task 4: Create StringToImageSourceConverter</name>
  <files>Converters/StringToImageSourceConverter.cs, App.xaml</files>
  <behavior>
    - Test 1: Convert returns UnsetValue for null input
    - Test 2: Convert returns UnsetValue for empty string input
    - Test 3: Convert returns UnsetValue for invalid path
    - Test 4: Convert returns BitmapImage for valid relative path
    - Test 5: Convert returns BitmapImage for valid absolute path
    - Test 6: Converter registered in App.xaml with key "StringToImageSourceConverter"
  </behavior>
  <action>
Create `Converters/StringToImageSourceConverter.cs`:
```csharp
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Converts a string file path to an ImageSource.
    /// Returns UnsetValue for null, empty, or invalid paths to allow fallback.
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    // Freeze for performance
                    if (bitmap.CanFreeze)
                    {
                        bitmap.Freeze();
                    }
                    
                    return bitmap;
                }
                catch
                {
                    // Invalid path - return UnsetValue to use fallback
                    return DependencyProperty.UnsetValue;
                }
            }
            
            // Null or empty - return UnsetValue to use fallback
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

Register in `App.xaml` (add to Converters section):
```xml
<converters:StringToImageSourceConverter x:Key="StringToImageSourceConverter"/>
```
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>StringToImageSourceConverter created and registered in App.xaml, build succeeds</done>
</task>

<task type="auto">
  <name>Task 5: Apply converter to Image bindings in feature views</name>
  <files>Views/DesktopClockView.xaml, Views/PomodoroView.xaml, Views/AnniversaryView.xaml</files>
  <action>
Update Image bindings to use StringToImageSourceConverter in:

1. `DesktopClockView.xaml` (around line 120):
   ```xml
   <Image Source="{Binding PreviewImagePath, Converter={StaticResource StringToImageSourceConverter}}"
          ... />
   ```

2. `PomodoroView.xaml` (around line 173):
   ```xml
   <Image Source="{Binding PreviewImagePath, Converter={StaticResource StringToImageSourceConverter}}"
          ... />
   ```

3. `AnniversaryView.xaml` (around line 252):
   ```xml
   <Image Source="{Binding PreviewImagePath, Converter={StaticResource StringToImageSourceConverter}}"
          ... />
   ```

The DataTrigger that checks for empty PreviewImagePath can remain as-is - it will handle the empty case with fallback content, and the converter will prevent the binding error when the trigger is not active.
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>All Image Source bindings in DesktopClockView, PomodoroView, and AnniversaryView use StringToImageSourceConverter, build succeeds</done>
</task>

</tasks>

<verification>
1. Build succeeds with 0 errors
2. Run application
3. Open Debug output window (View > Output, select "Debug" in dropdown)
4. Navigate to Creator view
5. Click "Desktop Background" - verify log shows SUCCESS and content displays
6. Click "Desktop Clock" - verify log shows SUCCESS and content displays
7. Click "Pomodoro" - verify log shows SUCCESS and content displays
8. Click "Anniversary" - verify log shows SUCCESS and content displays
9. Check Output window - no ImageSource binding errors
10. Verify only ONE navigation button is highlighted at a time
</verification>

<success_criteria>
- [ ] All 9 features display content when clicked (not just log SUCCESS)
- [ ] ContentPresenter displays ViewModels via DataTemplate lookup
- [ ] Single CurrentState enum controls navigation
- [ ] Only one navigation button highlighted at any time
- [ ] No ImageSource binding errors in Output window
- [ ] Build succeeds with 0 errors
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v3/01-fix-v3-01-SUMMARY.md`
</output>
