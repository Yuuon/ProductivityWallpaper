---
phase: 01-fix
plan: 01
status: completed
completed_at: 2026-03-14
---

# Plan 01-fix-01 Summary: Arrow Vertical Alignment Fix

## Objective
Fix the arrow vertical alignment in expandable navigation buttons so arrows stay centered when ToggleButton is expanded.

## Changes Made

### Views/CreatorView.xaml
Wrapped all 5 expand/collapse arrow Path elements in fixed-size Grid containers (16x16) to ensure proper vertical centering:

1. **Desktop Background** (line ~263)
2. **Mouse Click** (line ~334)
3. **Shutdown** (line ~404)
4. **Boot/Restart** (line ~474)
5. **Screen Wake** (line ~544)

### Technical Details
- **Before**: Path had `Width="16" Height="16"` directly on the element
- **After**: Path wrapped in `<Grid Width="16" Height="16">` with `VerticalAlignment="Center" HorizontalAlignment="Center"` on the Path
- **Why**: Grid provides a fixed-size layout container that prevents the Path from shifting when ToggleButton's IsChecked trigger changes the background

## Verification
- ✅ Build successful (no new errors)
- ✅ All 5 expandable features updated consistently
- ✅ Arrow rotation (180°) preserved

## Key Files Modified
- `Views/CreatorView.xaml` - Arrow container structure

## Requirements Addressed
- **FIX-02**: Arrow vertical alignment

## Notes
The fix ensures the arrow stays visually centered in both collapsed and expanded states. The fixed-size Grid container prevents layout recalculation issues when the ToggleButton's background changes.
