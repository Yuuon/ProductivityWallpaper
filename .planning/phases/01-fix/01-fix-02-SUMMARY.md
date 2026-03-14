---
phase: 01-fix
plan: 02
status: completed
completed_at: 2026-03-14
---

# Plan 01-fix-02 Summary: Scheme Auto-Creation

## Objective
Implement scheme auto-creation when expanding an empty feature.

## Changes Made

### ViewModels/CreatorViewModel.cs
Updated all 5 `OnIsXXXExpandedChanged` partial methods to:
1. Collapse other expandable features (single-expand behavior)
2. Call `EnsureDefaultScheme()` to create default scheme if none exist
3. Set `SelectedFeature` to the expanded feature name
4. Call `LoadFeatureContent()` to display the feature's content

**Affected methods:**
- `OnIsDesktopBackgroundExpandedChanged`
- `OnIsMouseClickExpandedChanged`
- `OnIsShutdownExpandedChanged`
- `OnIsBootRestartExpandedChanged`
- `OnIsScreenWakeExpandedChanged`

## Verification
- ✅ Build successful
- ✅ Auto-creation triggers when expanding empty feature
- ✅ New scheme named "{FeatureName} 1" 
- ✅ Scheme is immediately selected and displayed

## Requirements Addressed
- **FIX-04**: Scheme auto-creation when expanding empty feature

## Notes
The existing `EnsureDefaultScheme()` method was already implemented and works correctly:
- Creates scheme with proper name
- Sets IsActive = true and IsSelected = true
- Adds to the feature's scheme collection
- Updates the SelectedXXXScheme property
