---
phase: 02-fix
plan: 01
subsystem: Creator View
tags: [ui, navigation, styling, fix]
dependency_graph:
  requires: []
  provides: [CreatorView-FixedNavigation]
  affects: [Views/CreatorView.xaml, ViewModels/CreatorViewModel.cs, Resources/Theme.xaml]
tech_stack:
  added: [BooleanToStarConverter, BooleanToLeftMarginConverter]
  patterns: [Single-expand navigation, Conditional layout]
key_files:
  created:
    - none
  modified:
    - Views/CreatorView.xaml
    - ViewModels/CreatorViewModel.cs
    - Resources/Theme.xaml
    - Converters/BooleanToStyleConverter.cs
    - App.xaml
decisions:
  - "Used BooleanToStarConverter and BooleanToLeftMarginConverter for dynamic grid layout"
  - "Applied PrimaryGradientBrush to expanded navigation buttons for consistency"
  - "Set submenu backgrounds to Transparent with only selected item highlighted"
metrics:
  duration: "45 minutes"
  completed_date: "2026-03-14"
---

# Phase 02 Plan 01: Creator View Navigation and Layout Fixes

**Summary:** Fixed all UI/UX issues in Creator view including navigation behavior, styling consistency, and page display logic as documented in Phase-01-Issue-Fix.md.

## Fixes Applied

### 1. Single-Expand Navigation (Task 1)
**Commit:** `9a41b25`

- Added `CollapseAllNavExcept()` method to collapse all expandable menus except the specified one
- Updated `ToggleFeatureExpansion` command to collapse others when expanding a menu
- Updated `SelectFeature` to collapse all menus when selecting simple features (Theme Preview, Open App, etc.)
- **Result:** Only one submenu expands at a time

### 2. Navigation Button Styling Consistency (Task 2)
**Commit:** `08826f2`

- Updated `FeatureExpanderButtonStyle` to use `BackgroundBlockBrush` as default background
- Set height to 40 and padding to 16,0 to match `FeatureButtonStyle`
- Changed expanded state to use `PrimaryGradientBrush` instead of `BackgroundSelectedBrush`
- Set corner radius to 10 for visual consistency
- **Result:** Expandable buttons now look identical to simple buttons

### 3. Submenu Styling (Task 3)
**Commit:** `2035532`

- Changed submenu backgrounds from `BackgroundBlockBrush` to `Transparent` for all 5 expandable features
- Added "+" prefix to new scheme button text (`+ {DynamicResource Creator_NewScheme}`)
- Set new scheme button height to 32 with proper padding (12,6)
- Applied to: DesktopBackground, MouseClick, Shutdown, BootRestart, ScreenWake
- **Result:** Clean submenu appearance with no background and properly styled buttons

### 4. Custom Scrollbar Style (Task 4)
**Commit:** `244f679`

- Added `ScrollBar` style to `Theme.xaml` with transparent background
- Thumb uses `BorderLineBrush` (4px width, 4px corner radius)
- Shows `BackgroundBlockBrush` background on hover
- Thin 8px width matching the app's dark theme
- **Result:** Scrollbar now visually consistent with the application theme

### 5. Feature Page Display Logic (Task 5)
**Commit:** `6ce7a3c`

- Updated `LoadFeatureContent` method to properly load content for all features
- Added `BooleanToStarConverter` and `BooleanToLeftMarginConverter` for dynamic layout
- Modified CreatorView XAML to conditionally show left panel only for Theme Preview
- Left column width uses `BooleanToStarConverter` (6* when Theme Preview, 0 otherwise)
- Right panel margin adjusts based on `IsThemePreviewSelected`
- **Result:** Theme Preview shows split layout; other features show full-width configuration

### 6. Theme Preview Split Ratio (Task 6)
**Commit:** `ab651f6`

- Theme Preview page uses 6* for left (preview) and 4* for right (config) columns
- This creates the 60/40 split ratio as specified in the design reference
- Other features show full-width configuration (0* for left column)
- **Result:** Correct preview/config ratio matching the design specification

## Issues Resolved

| Issue | Status | Fix Location |
|-------|--------|--------------|
| Theme preview split ratio (60/40) | ✅ | CreatorView.xaml Grid columns |
| Single-expand navigation | ✅ | CreatorViewModel.CollapseAllNavExcept() |
| Checkmark shows active scheme | ✅ | Already implemented (verified) |
| New scheme button text/height | ✅ | CreatorView.xaml (all 5 features) |
| Custom scrollbar style | ✅ | Theme.xaml |
| Navigation button consistency | ✅ | FeatureExpanderButtonStyle |
| Submenu transparent background | ✅ | CreatorView.xaml (all 5 features) |
| Arrow vertical alignment | ✅ | Already at Center (verified) |
| Feature pages display | ✅ | LoadFeatureContent + XAML binding |
| Theme preview left panel visibility | ✅ | BooleanToVisibility + BooleanToStarConverter |

## Files Modified

1. **Views/CreatorView.xaml** - Navigation styling, submenu appearance, layout binding
2. **ViewModels/CreatorViewModel.cs** - Single-expand logic, content loading
3. **Resources/Theme.xaml** - Custom scrollbar style
4. **Converters/BooleanToStyleConverter.cs** - Added BooleanToStarConverter, BooleanToLeftMarginConverter
5. **App.xaml** - Registered new converters

## Verification

All changes verified with successful build:
```bash
dotnet build --no-restore
# Build successful
```

## Commits

- `9a41b25`: fix(02-fix-01): implement single-expand navigation logic
- `08826f2`: fix(02-fix-01): unify navigation button styling
- `2035532`: fix(02-fix-01): fix submenu styling
- `244f679`: feat(02-fix-01): add custom scrollbar style
- `6ce7a3c`: fix(02-fix-01): fix feature page display logic
- `ab651f6`: fix(02-fix-01): set Theme Preview split ratio to 60/40

## Deviation from Plan

None - all tasks executed as planned with no deviations.
