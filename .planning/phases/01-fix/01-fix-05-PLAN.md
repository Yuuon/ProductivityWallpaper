---
phase: 01-fix
plan: 05
type: execute
wave: 2
depends_on:
  - 01-fix-03
  - 01-fix-04
files_modified:
  - Views/CreatorView.xaml
  - ViewModels/CreatorViewModel.cs
  - App.xaml
  - App.xaml.cs
autonomous: true
requirements:
  - FIX-01
must_haves:
  truths:
    - Desktop Clock page displays clock styles, format toggles, and opacity controls
    - Pomodoro page displays timer styles, DND toggle, and duration settings
    - Anniversary page displays display styles and event management
    - Theme name only appears in Theme Preview page, not in feature pages
  artifacts:
    - path: "Views/CreatorView.xaml"
      provides: "Working ContentControl binding for ConfigurationContent"
    - path: "ViewModels/CreatorViewModel.cs"
      provides: "Correct ViewModel instantiation and assignment"
    - path: "App.xaml"
      provides: "DataTemplates for DesktopClock, Pomodoro, Anniversary ViewModels"
  key_links:
    - from: "CreatorViewModel.ConfigurationContent"
      to: "ContentControl.Content"
      via: "DataTemplate resolution in App.xaml"
    - from: "DesktopClockViewModel"
      to: "DesktopClockView"
      via: "DataTemplate DataType mapping"
---

<objective>
Fix the critical issue where Clock, Pomodoro, and Anniversary pages display nothing except theme name (which shouldn't appear there at all).

**Purpose:** Restore functionality - these feature pages worked in v1 but broke after fixes. Users need to configure these features.

**Output:** All three feature pages display their content correctly; theme name only appears in Theme Preview.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/phases/01-fix/01-CONTEXT.md
@Views/CreatorView.xaml
@ViewModels/CreatorViewModel.cs
@App.xaml
@App.xaml.cs

## Debug Analysis

**Current Code in CreatorViewModel.cs LoadFeatureContent:**

```csharp
case "DesktopClock":
    var clockVm = _desktopClockVmFactory();
    ConfigurationContent = clockVm;
    HasPreviewContent = false;
    break;
```

This looks correct - it creates the ViewModel and assigns it to `ConfigurationContent`.

**CreatorView.xaml ContentControl (line 752):**
```xml
<ContentControl Content="{Binding ConfigurationContent}"/>
```

**App.xaml DataTemplates (lines 67-77):**
```xml
<DataTemplate DataType="{x:Type vm:DesktopClockViewModel}">
    <views:DesktopClockView/>
</DataTemplate>
<DataTemplate DataType="{x:Type vm:PomodoroViewModel}">
    <views:PomodoroView/>
</DataTemplate>
<DataTemplate DataType="{x:Type vm:AnniversaryViewModel}">
    <views:AnniversaryView/>
</DataTemplate>
```

**DI Registrations in App.xaml.cs (lines 42-51):**
```csharp
services.AddTransient<DesktopClockViewModel>();
services.AddTransient<PomodoroViewModel>();
services.AddTransient<AnniversaryViewModel>();
// ...
services.AddTransient<DesktopClockView>();
services.AddTransient<PomodoroView>();
services.AddTransient<AnniversaryView>();
```

**Theme Name Display Issue:**
The theme name appears in the header (lines 643-705 in CreatorView.xaml), which is ALWAYS visible when `IsCreatingPage=true`. This is correct - the theme name should be shown as a header.

**But the problem statement says:** "showing theme name instead" - this suggests the ConfigurationContent is not being displayed, OR the View is showing the theme name instead of its own content.

**Hypothesis 1: DataContext Issue**
The Views (DesktopClockView, etc.) might have incorrect DataContext. They should use their own ViewModel as DataContext, not inherit from parent.

**Hypothesis 2: Binding Errors**
The Views might have binding errors causing them to not render properly.

**Hypothesis 3: View Initialization Issue**
The View XAML files might have issues that prevent proper rendering.

**Hypothesis 4: ContentControl Not Updating**
The ContentControl binding might not be triggering updates properly.

**Debug Steps from CONTEXT.md:**
1. ViewModel initialization - Verify constructors don't throw
2. Binding resolution - Check DataTemplates are found
3. Content loading - Verify ConfigurationContent assignment

## ViewModel Constructors Analysis

**DesktopClockViewModel.cs (lines 42-45):**
```csharp
public DesktopClockViewModel()
{
    InitializeDefaultStyles();
}
```
Clean constructor, no DI dependencies that could fail.

**PomodoroViewModel.cs (lines 47-50):**
```csharp
public PomodoroViewModel()
{
    InitializeDefaultStyles();
}
```
Same pattern.

**AnniversaryViewModel.cs (lines 64-67):**
```csharp
public AnniversaryViewModel()
{
    InitializeDefaultStyles();
}
```
Same pattern.

All constructors look clean and shouldn't throw exceptions.

## Key Debugging Strategy

1. Add debug logging to LoadFeatureContent to verify ViewModels are created
2. Check if DataTemplates are resolving correctly
3. Verify the ContentControl is actually receiving the ViewModel
4. Check DesktopClockView.xaml for binding issues
</context>

<tasks>

<task type="auto">
  <name>Debug and Fix Feature Page Display Issue</name>
  <files>Views/CreatorView.xaml, ViewModels/CreatorViewModel.cs, App.xaml</files>
  <action>
Fix the critical issue where Clock, Pomodoro, and Anniversary pages don't display their content.

**Debug Step 1: Verify ViewModels Are Being Created**

Add debug output to verify ViewModels are instantiated in `LoadFeatureContent`:

```csharp
case "DesktopClock":
    System.Diagnostics.Debug.WriteLine("Loading DesktopClock content...");
    var clockVm = _desktopClockVmFactory();
    System.Diagnostics.Debug.WriteLine($"DesktopClockViewModel created: {clockVm != null}");
    System.Diagnostics.Debug.WriteLine($"ClockStyles count: {clockVm.ClockStyles.Count}");
    ConfigurationContent = clockVm;
    System.Diagnostics.Debug.WriteLine($"ConfigurationContent set to: {ConfigurationContent?.GetType().Name}");
    HasPreviewContent = false;
    break;
```

Do the same for Pomodoro and Anniversary.

**Debug Step 2: Check DataTemplate Resolution**

In App.xaml, verify the DataTemplates are correct. The issue might be namespace mismatches:

Current (lines 67-77):
```xml
<DataTemplate DataType="{x:Type vm:DesktopClockViewModel}">
    <views:DesktopClockView/>
</DataTemplate>
```

Verify:
- `vm` namespace points to `ProductivityWallpaper.ViewModels`
- `views` namespace points to `ProductivityWallpaper.Views`
- The View files exist at `Views/DesktopClockView.xaml`, etc.

**Debug Step 3: Fix Potential ContentControl Binding Issue**

The ContentControl might not be updating when ConfigurationContent changes. Try:

1. Add explicit UpdateSourceTrigger or check if binding is working:
```xml
<ContentControl Content="{Binding ConfigurationContent, UpdateSourceTrigger=PropertyChanged}"/>
```

2. Ensure `ConfigurationContent` property has proper change notification (it should via `[ObservableProperty]`).

**Debug Step 4: Check View XAML Files**

Verify the View XAML files exist and have correct structure:
- `Views/DesktopClockView.xaml` - should have proper x:Class and DataContext
- `Views/PomodoroView.xaml` - same
- `Views/AnniversaryView.xaml` - same

If any View has `DataContext="{Binding}"` or inherits DataContext incorrectly, it might not display.

**Debug Step 5: Check for Binding Errors**

Common issues in the Views:
- Missing StaticResource references
- Incorrect binding paths
- Null DataContext

**Fix Strategy:**

If the Views exist and DataTemplates are correct, the issue might be that the ContentControl needs to be forced to refresh. Try:

1. In CreatorViewModel.cs, modify LoadFeatureContent to temporarily set ConfigurationContent to null, then set it to the actual ViewModel:

```csharp
case "DesktopClock":
    ConfigurationContent = null; // Force clear first
    var clockVm = _desktopClockVmFactory();
    ConfigurationContent = clockVm;
    HasPreviewContent = false;
    break;
```

2. Check if there's an issue with the ContentControl's DataContext inheritance. The ContentControl should NOT have an explicit DataContext - it should inherit from the parent (CreatorViewModel) so the binding to `ConfigurationContent` works.

**Debug Step 6: Check CreatorView.xaml ContentControl**

Line 752:
```xml
<ContentControl Content="{Binding ConfigurationContent}"/>
```

This looks correct. But verify there's no DataContext override that would break the binding.

**Alternative: Explicit View Assignment**

If DataTemplates aren't working, try explicit View assignment in code-behind or ViewModel:

```csharp
case "DesktopClock":
    var clockVm = _desktopClockVmFactory();
    var clockView = new DesktopClockView { DataContext = clockVm };
    ConfigurationContent = clockView; // Set the View instead of ViewModel
    HasPreviewContent = false;
    break;
```

But this breaks the MVVM pattern. Better to fix the DataTemplate resolution.

**Most Likely Fix:**

The issue might be that the Views (DesktopClockView.xaml, etc.) have issues. Check if they have proper constructors and XAML structure. Common issues:
1. Missing InitializeComponent() call
2. XAML errors preventing compilation
3. Incorrect x:Class namespace

If Views are fine, the issue might be in how the DataTemplates are being resolved. Check if there's a mismatch between the ViewModel type in the DataTemplate and the actual ViewModel type being created.

**Final Verification:**

After fixes, verify:
1. DesktopClock shows 4 clock styles
2. Pomodoro shows timer styles and DND toggle
3. Anniversary shows display styles and event list
4. Theme name ONLY appears in header (correct behavior), not as a replacement for content
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /C:"error" && exit 1 || exit 0</automated>
  </verify>
  <done>Clock, Pomodoro, and Anniversary pages display their content correctly; debug logging confirms ViewModels are created and DataTemplates resolve properly</done>
</task>

</tasks>

<verification>
Build verification: Project compiles without errors.
Runtime verification: Each feature page shows its specific content (styles, toggles, lists).
</verification>

<success_criteria>
- Desktop Clock page shows: 4 clock styles, 12h/24h toggle, opacity slider
- Pomodoro page shows: timer styles, DND toggle, work/break duration settings
- Anniversary page shows: display styles, event list with repeat modes
- Theme name appears only in header area (not in content area)
- No binding errors in output window
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix/01-fix-05-SUMMARY.md`
</output>
