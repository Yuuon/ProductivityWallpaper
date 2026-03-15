---
phase: 01-fix-v2
plan: 02
subsystem: CreatorView
name: Fix Expandable Button Highlight Persistence
type: bugfix
wave: 1
depends_on: []
requirements:
  - FIX-V2-003
commits:
  - hash: b7fb05a
    message: "fix(01-fix-v2-02): fix DesktopBackground expandable highlight persistence"
  - hash: d6e259e
    message: "fix(01-fix-v2-02): fix remaining 4 expandable highlight persistence"
key_files:
  created: []
  modified:
    - ViewModels/CreatorViewModel.cs
deviations: []
decisions: []
tech_stack:
  added: []
  patterns:
    - "ObservableProperty partial methods with else branches for cleanup"
metrics:
  duration_minutes: 15
  tasks_completed: 2
  files_modified: 1
  lines_added: 77
  lines_removed: 10
completed_at: 2026-03-15
---

# Phase 01-fix-v2 Plan 02: Fix Expandable Button Highlight Persistence

## Summary

Fixed expandable button highlight persistence issue where buttons stayed highlighted after collapsing. Added else branches to all 5 `OnIsXXXExpandedChanged` partial methods to clear scheme selection when the expandable collapses and the feature is not currently selected.

## One-Liner

Fixed expandable button highlight persistence by adding cleanup logic to all 5 `OnIsXXXExpandedChanged` partial methods that clears scheme selection when collapsing unselected features.

## Changes Made

### ViewModels/CreatorViewModel.cs

Updated all 5 expandable feature partial methods with consistent cleanup logic:

1. **OnIsDesktopBackgroundExpandedChanged** (lines 45-76)
2. **OnIsMouseClickExpandedChanged** (lines 81-112)
3. **OnIsShutdownExpandedChanged** (lines 117-148)
4. **OnIsBootRestartExpandedChanged** (lines 153-184)
5. **OnIsScreenWakeExpandedChanged** (lines 189-220)

Each method now follows this pattern:

```csharp
partial void OnIsXXXExpandedChanged(bool value)
{
    if (value)
    {
        // Existing expansion logic preserved
        // - Collapse other expandables
        // - Ensure default scheme exists
        // - Set SelectedFeature
        // - Load feature content
    }
    else
    {
        // NEW: Clear highlight if feature not selected
        if (SelectedFeature != "XXX")
        {
            if (SelectedXXXScheme != null)
            {
                SelectedXXXScheme.IsSelected = false;
                SelectedXXXScheme = null;
            }
        }
    }
}
```

## Logic Explanation

The header highlight properties (e.g., `IsDesktopBackgroundHeaderHighlighted`) depend on two conditions:

```csharp
public bool IsDesktopBackgroundHeaderHighlighted => 
    SelectedFeature == "DesktopBackground" || 
    (SelectedDesktopBackgroundScheme?.IsSelected == true);
```

**Before the fix:**
- When an expandable collapsed, the scheme could remain selected
- This caused the header to stay highlighted even when not the active feature
- Multiple headers could appear highlighted simultaneously

**After the fix:**
- When an expandable collapses (`value = false`)
- If the feature is NOT the current `SelectedFeature`, we:
  1. Set `IsSelected = false` on the scheme
  2. Clear the `SelectedXXXScheme` property
- This causes the highlight property to return `false`
- If the feature IS selected, we leave the scheme selected (parent highlighting)

## Verification

- [x] Build succeeds with 0 errors
- [x] All 5 partial methods have else branches
- [x] Else branches clear scheme selection when feature not selected
- [x] Existing expansion logic preserved unchanged
- [x] Consistent pattern applied across all expandable features

## Commits

| Hash | Type | Description |
|------|------|-------------|
| `b7fb05a` | fix | DesktopBackground expandable highlight persistence |
| `d6e259e` | fix | MouseClick, Shutdown, BootRestart, ScreenWake expandable highlights |

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- [x] Modified file exists: `ViewModels/CreatorViewModel.cs`
- [x] Commits exist: `b7fb05a`, `d6e259e`
- [x] Build successful
- [x] No errors introduced
