---
phase: 01-foundation
plan: 01
name: Multi-Scheme Navigation for Creator View
subsystem: UI/Creator
completed: 2026-03-14
tags: [mvvm, wpf, navigation, scheme-management]
dependency_graph:
  requires: []
  provides: [scheme-management, expandable-navigation]
  affects: [Views/CreatorView.xaml, ViewModels/CreatorViewModel.cs]
tech_stack:
  added: []
  patterns: [CommunityToolkit.Mvvm, ObservableObject, DataBinding, Command Pattern]
key_files:
  created:
    - Models/SchemeModel.cs (153 lines)
  modified:
    - ViewModels/CreatorViewModel.cs (453 lines, +323/-40)
    - Views/CreatorView.xaml (740 lines, +496/-79)
    - Resources/LocalizationResources.xaml (166 lines, +13/-0)
decisions:
  - "Used ToggleButton for expandable features instead of Expander for better styling control"
  - "Auto-create default scheme when feature first expanded to ensure user always has at least one scheme"
  - "Single-feature buttons (OpenApp, DesktopClock, Pomodoro, Anniversary) remain simple without submenus"
  - "Use ObservableCollection per feature for efficient UI updates"
---

# Phase 01 Plan 01: Multi-Scheme Navigation Summary

**One-liner:** Redesigned CreatorView left navigation with expandable submenus supporting multiple schemes per feature.

## Overview

This plan implements multi-scheme management for the Creator view's left navigation panel. Five features (Desktop Background, Mouse Click, Shutdown, Boot Restart, Screen Wake) now support expandable submenus where users can create, select, and manage multiple configuration schemes. Four single-scheme features (Open App, Desktop Clock, Pomodoro, Anniversary) remain as simple buttons.

## What Was Built

### 1. Scheme Data Model (`Models/SchemeModel.cs`)
- **FeatureType enum**: 9 feature types (5 multi-scheme + 4 single-scheme)
- **SchemeModel class**: ObservableObject-based with properties:
  - `Id` (string, auto-generated GUID)
  - `Name` (string, display name)
  - `FeatureType` (enum)
  - `IsActive` (bool, for active state highlighting)
  - `CreatedAt` (DateTime, for sorting/tracking)
- Full XML documentation for all members

### 2. ViewModel Updates (`ViewModels/CreatorViewModel.cs`)
New scheme management capabilities:
- `SchemesByFeature` dictionary mapping FeatureType → ObservableCollection<SchemeModel>
- Convenience properties for each multi-scheme feature's collection:
  - `DesktopBackgroundSchemes`
  - `MouseClickSchemes`
  - `ShutdownSchemes`
  - `BootRestartSchemes`
  - `ScreenWakeSchemes`
- Expansion state properties: `Is{Feature}Expanded`
- Selected scheme properties: `Selected{Feature}Scheme`

**Commands:**
- `ToggleFeatureExpansionCommand`: Expands/collapses feature menus, auto-creates default scheme
- `CreateNewSchemeCommand`: Creates new scheme with auto-generated name (e.g., "Desktop Background 1")
- `SelectSchemeCommand`: Activates selected scheme (only one active per feature)

**Auto-naming logic:** Format "{FeatureName} {Count+1}"

### 3. Redesigned Navigation (`Views/CreatorView.xaml`)

**New Resources:**
- `SchemeItemTemplate`: DataTemplate for scheme list items with:
  - Click-to-select behavior
  - Active indicator (checkmark path icon)
  - Hover effects
- `NewSchemeButtonStyle`: Distinct button style for "New Scheme" action
- `FeatureExpanderButtonStyle`: ToggleButton style for feature headers

**Structure Changes:**
- 5 expandable features with ToggleButton headers
  - Feature name + count badge
  - Expand/collapse arrow (rotates 180° when expanded)
  - Click toggles expansion state
- Expanded submenu shows:
  - ItemsControl with SchemeItemTemplate for schemes list
  - "New Scheme" button at bottom
- 4 simple feature buttons (no expansion)
- ScrollViewer wrapping navigation (vertical scrollbar when needed)
- Back button fixed outside scroll area

### 4. Localization (`Resources/LocalizationResources.xaml`)
Added keys:
- `Creator_NewScheme` / `Creator_NewScheme_zh`: "New Scheme" / "新建方案"
- `Creator_ActiveScheme` / `Creator_ActiveScheme_zh`: "Active" / "当前激活"
- `Creator_DefaultSchemeName`: "{0} {1}" format for auto-naming
- `Creator_NoSchemes` / `Creator_NoSchemes_zh`: "No schemes yet" / "暂无方案"

## Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| ToggleButton over Expander | Better control over styling, animation, and behavior |
| ObservableCollection per feature | Efficient UI updates via INotifyCollectionChanged |
| Auto-create default scheme | Ensures user always has at least one scheme per feature |
| Only one active scheme per feature | Prevents configuration conflicts |
| Separate simple buttons for 4 features | These features don't need multi-scheme capability |

## Verification Results

- ✅ Build compiles without errors
- ✅ All 4 tasks completed with individual commits
- ✅ SchemeModel exports FeatureType and SchemeModel types
- ✅ ViewModel uses CommunityToolkit.Mvvm patterns (ObservableProperty, RelayCommand)
- ✅ XAML uses existing converters (BooleanToVisibilityConverter, CountToVisibilityConverter)
- ✅ Localization keys added for all new UI text

## Commits

| Hash | Task | Message |
|------|------|---------|
| 1d387e8 | Task 1 | feat(01-01): create SchemeModel with FeatureType enum |
| 8b1f2f0 | Task 2 | feat(01-01): extend CreatorViewModel with multi-scheme management |
| 043750b | Task 3 | feat(01-01): redesign left navigation with expandable submenus |
| 0bf49b3 | Task 4 | feat(01-01): add localization strings for multi-scheme management |

## Deviations from Plan

**None** - plan executed exactly as written.

## Files Modified

```
Models/
  └── SchemeModel.cs (created, 153 lines)

ViewModels/
  └── CreatorViewModel.cs (+323/-40 lines)

Views/
  └── CreatorView.xaml (+496/-79 lines)

Resources/
  └── LocalizationResources.xaml (+13 lines)
```

## Next Steps

The scheme management infrastructure is now in place. Future plans can:
- Add scheme persistence (save/load from disk)
- Implement scheme deletion and renaming
- Add scheme duplication (clone existing configuration)
- Create scheme configuration UI (per-feature settings panels)

## Self-Check: PASSED

- ✅ SchemeModel.cs exists and compiles
- ✅ CreatorViewModel.cs compiles with new properties
- ✅ CreatorView.xaml compiles with new structure
- ✅ LocalizationResources.xaml includes new keys
- ✅ All commits created with proper messages
