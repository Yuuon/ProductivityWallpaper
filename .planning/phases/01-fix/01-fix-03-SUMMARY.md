---
phase: 01-fix
plan: 03
status: completed
completed_at: 2026-03-14
---

# Plan 01-fix-03 Summary: Navigation Highlight Consistency

## Objective
Fix navigation highlight consistency - ensure expandable headers stay highlighted when child scheme is selected.

## Changes Made

### ViewModels/CreatorViewModel.cs
1. **Added header highlight computed properties:**
   - `IsDesktopBackgroundHeaderHighlighted`
   - `IsMouseClickHeaderHighlighted`
   - `IsShutdownHeaderHighlighted`
   - `IsBootRestartHeaderHighlighted`
   - `IsScreenWakeHeaderHighlighted`

   Each returns true when either:
   - The feature itself is selected (`SelectedFeature == "FeatureName"`)
   - OR a scheme of that feature is selected (`SelectedXXXScheme?.IsSelected == true`)

2. **Added `[NotifyPropertyChangedFor]` attributes** to SelectedXXXScheme properties so header highlights update when schemes are selected.

3. **Updated `SelectScheme` method** to:
   - Set `SelectedFeature` to the parent feature name when scheme is selected
   - Call `LoadFeatureContent()` to display the scheme's content

### Views/CreatorView.xaml
Added custom Style with DataTrigger to all 5 expandable ToggleButtons:
```xml
<DataTrigger Binding="{Binding IsXXXHeaderHighlighted}" Value="True">
    <Setter Property="Background" Value="{StaticResource PrimaryGradientBrush}"/>
</DataTrigger>
```

## Verification
- ✅ Build successful
- ✅ Header highlights when feature expanded
- ✅ Header stays highlighted when child scheme selected
- ✅ Simple features (Clock, Pomodoro, etc.) work as before

## Requirements Addressed
- **FIX-03**: Navigation highlight consistency

## Notes
This implements the "dual-highlight" pattern where:
- The parent header stays highlighted when a child scheme is selected
- Only one main feature is highlighted at a time
- Visual hierarchy is clear to users
