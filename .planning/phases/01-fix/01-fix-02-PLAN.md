---
phase: 01-fix
plan: 02
type: execute
wave: 1
depends_on: []
files_modified:
  - ViewModels/CreatorViewModel.cs
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-04
must_haves:
  truths:
    - Expanding empty feature auto-creates "{FeatureName} 1" scheme
    - New scheme is immediately shown in the right panel
    - Auto-creation only triggers when expanding, not on every expand
  artifacts:
    - path: "ViewModels/CreatorViewModel.cs"
      provides: "EnsureDefaultScheme integration with ToggleButton expansion"
      section: "Expansion property setters and OnIsXXXExpandedChanged partial methods"
    - path: "Views/CreatorView.xaml"
      provides: "ToggleButton bindings to trigger auto-creation"
  key_links:
    - from: "ToggleButton.IsChecked"
      to: "EnsureDefaultScheme()"
      pattern: "Expanding feature → create default scheme if empty"
---

<objective>
Implement scheme auto-creation when expanding an empty feature.

**Purpose:** User convenience - when a user expands a feature that has no schemes, automatically create a default scheme named "{FeatureName} 1" and immediately display it.

**Output:** Seamless scheme creation without requiring explicit "New Scheme" button click for the first scheme.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/phases/01-fix/01-CONTEXT.md
@ViewModels/CreatorViewModel.cs
@Views/CreatorView.xaml

## Current State Analysis

**CreatorViewModel.cs already has:**
1. `EnsureDefaultScheme(FeatureType featureType)` method (lines 522-558) - creates default scheme if none exist
2. `OnIsXXXExpandedChanged` partial methods (lines 45-110) - handle single-expand logic
3. `SelectScheme(SchemeModel)` method - handles scheme selection with IsActive/IsSelected flags

**Current ToggleButton bindings:**
```xml
<ToggleButton Style="{StaticResource FeatureExpanderButtonStyle}"
              IsChecked="{Binding IsDesktopBackgroundExpanded, Mode=TwoWay}">
```

**Current Flow Issue:**
The `EnsureDefaultScheme` method exists but is NOT being called when the ToggleButton is expanded. The ToggleButton click only sets the expansion state, it doesn't trigger auto-creation.

**Requirements from CONTEXT.md:**
- When expanding a feature with no schemes: auto-create "{FeatureName} 1"
- Immediately show the new scheme's page
- Only create when expanding (not on every expand check)
</context>

<tasks>

<task type="auto">
  <name>Integrate Auto-Scheme Creation with ToggleButton Expansion</name>
  <files>ViewModels/CreatorViewModel.cs, Views/CreatorView.xaml</files>
  <action>
Implement scheme auto-creation when expanding a feature that has no schemes.

**Step 1: Modify CreatorViewModel.cs**

Update the `OnIsXXXExpandedChanged` partial methods to call `EnsureDefaultScheme` when expanding AND select/show the scheme:

For each expandable feature (DesktopBackground, MouseClick, Shutdown, BootRestart, ScreenWake), update the partial method:

```csharp
partial void OnIsDesktopBackgroundExpandedChanged(bool value)
{
    if (value)
    {
        // Single-expand: collapse others
        IsMouseClickExpanded = false;
        IsShutdownExpanded = false;
        IsBootRestartExpanded = false;
        IsScreenWakeExpanded = false;
        
        // Auto-create scheme if none exist
        EnsureDefaultScheme(FeatureType.DesktopBackground);
        
        // Select the feature and show content
        SelectedFeature = "DesktopBackground";
        LoadFeatureContent("DesktopBackground");
    }
}
```

Repeat this pattern for all 5 expandable features:
- OnIsMouseClickExpandedChanged → FeatureType.MouseClick → "MouseClick"
- OnIsShutdownExpandedChanged → FeatureType.Shutdown → "Shutdown"
- OnIsBootRestartExpandedChanged → FeatureType.BootRestart → "BootRestart"
- OnIsScreenWakeExpandedChanged → FeatureType.ScreenWake → "ScreenWake"

**Step 2: Modify CreatorView.xaml**

Change the ToggleButton bindings from `Mode=TwoWay` to `Mode=OneWayToSource` OR add a Click handler. Actually, since we're handling everything in the partial methods, the existing TwoWay binding should work - BUT we need to ensure the ToggleButton doesn't also trigger the command.

Actually, the simpler approach:
- Keep the existing `IsChecked="{Binding IsXXXExpanded, Mode=TwoWay}"` 
- The partial methods will handle auto-creation and content loading
- Remove any Click handlers that might conflict

**Step 3: Verify EnsureDefaultScheme works correctly**

The `EnsureDefaultScheme` method (lines 522-558) should:
1. Check if schemes collection is empty
2. Create "{FeatureName} 1" scheme
3. Set IsActive = true, IsSelected = true
4. Add to collection
5. Update the SelectedXXXScheme property
6. Return the created scheme (or void)

Verify that after calling `EnsureDefaultScheme`, the `SelectedXXXScheme` property is set (it is in the current code).

**Step 4: Ensure LoadFeatureContent loads the selected scheme**

In `LoadFeatureContent`, for expandable features (lines 601-621), the code should use `SelectedXXXScheme` to set the SchemeName on the ViewModel. Verify this is working correctly.

Current code for DesktopBackground (lines 601-610):
```csharp
case "DesktopBackground":
    var desktopBgVm = _desktopBackgroundVmFactory();
    if (SelectedDesktopBackgroundScheme != null)
    {
        desktopBgVm.SchemeName = SelectedDesktopBackgroundScheme.Name;
    }
    ConfigurationContent = desktopBgVm;
    HasPreviewContent = false;
    break;
```

This should work if `SelectedDesktopBackgroundScheme` is set by `EnsureDefaultScheme`.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /C:"error" && exit 1 || exit 0</automated>
  </verify>
  <done>Auto-creation is triggered when expanding any empty feature, and the new scheme's content is immediately displayed</done>
</task>

</tasks>

<verification>
Build verification: Project compiles without errors.
</verification>

<success_criteria>
- Expanding a feature with no schemes auto-creates "{FeatureName} 1"
- The new scheme is immediately shown in the right panel
- The scheme appears in the submenu list
- Feature header is highlighted when expanded
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix/01-fix-02-SUMMARY.md`
</output>
