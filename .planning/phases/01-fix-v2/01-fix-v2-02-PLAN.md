---
phase: 01-fix-v2
plan: 02
type: execute
wave: 1
depends_on: []
files_modified: 
  - ViewModels/CreatorViewModel.cs
autonomous: true
requirements:
  - FIX-V2-003
must_haves:
  truths:
    - "Expandable buttons clear highlight when collapsed (if feature not selected)"
    - "Only one button is highlighted at any time"
    - "Header remains highlighted when child scheme is selected"
    - "Collapsing an expandable removes its highlight unless it's the active feature"
  artifacts:
    - path: "ViewModels/CreatorViewModel.cs"
      provides: "Fixed expandable highlight logic in all OnIsXXXExpandedChanged partial methods"
      changes: "Add else branch to clear highlight when value=false and feature not selected"
  key_links:
    - from: "OnIsDesktopBackgroundExpandedChanged"
      to: "IsDesktopBackgroundHeaderHighlighted"
      via: "if (!value && SelectedFeature != 'DesktopBackground') clear selection"
    - from: "OnIsMouseClickExpandedChanged"
      to: "IsMouseClickHeaderHighlighted"
      via: "if (!value && SelectedFeature != 'MouseClick') clear selection"
    - from: "OnIsShutdownExpandedChanged"
      to: "IsShutdownHeaderHighlighted"
      via: "if (!value && SelectedFeature != 'Shutdown') clear selection"
    - from: "OnIsBootRestartExpandedChanged"
      to: "IsBootRestartHeaderHighlighted"
      via: "if (!value && SelectedFeature != 'BootRestart') clear selection"
    - from: "OnIsScreenWakeExpandedChanged"
      to: "IsScreenWakeHeaderHighlighted"
      via: "if (!value && SelectedFeature != 'ScreenWake') clear selection"
---

<objective>
Fix expandable button highlight persistence issue where buttons stay highlighted after collapsing.

Purpose: Ensure visual consistency - only selected features remain highlighted
Output: Updated OnIsXXXExpandedChanged methods with proper highlight cleanup logic
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/PROJECT.md
@.planning/phases/01-fix-v2/01-CONTEXT.md

## Current Implementation Analysis

From CreatorViewModel.cs:

**Problem:** Partial methods only handle expansion (value=true), not collapse (value=false)

```csharp
// Lines 45-62 - Desktop Background example
partial void OnIsDesktopBackgroundExpandedChanged(bool value)
{
    if (value)  // <-- Only handles expanding!
    {
        // Collapse others, auto-create scheme, select feature
        IsMouseClickExpanded = false;
        // ... etc
        SelectedFeature = "DesktopBackground";
        LoadFeatureContent("DesktopBackground");
    }
    // <-- Missing: what happens when collapsing (value=false)?
}
```

**Expected Behavior (from 01-CONTEXT.md):**
- When ToggleButton.IsChecked = false (collapsed):
  - If this feature is NOT selected → remove highlight
  - If this feature IS selected → keep highlight
- When ToggleButton.IsChecked = true (expanded):
  - Always highlight
  - Collapse all other expandables

**Header Highlight Properties (lines 261-295):**
```csharp
public bool IsDesktopBackgroundHeaderHighlighted => 
    SelectedFeature == "DesktopBackground" || 
    (SelectedDesktopBackgroundScheme?.IsSelected == true);
```

The highlight logic depends on SelectedFeature. When collapsing, if the feature is not the SelectedFeature, we need to ensure no scheme is selected for that feature.
</context>

<tasks>

<task type="auto" tdd="false">
  <name>Task 1: Fix DesktopBackground Expandable Highlight</name>
  <files>ViewModels/CreatorViewModel.cs</files>
  <action>
Update OnIsDesktopBackgroundExpandedChanged partial method (lines 45-62):

**Current code:**
```csharp
partial void OnIsDesktopBackgroundExpandedChanged(bool value)
{
    if (value)
    {
        IsMouseClickExpanded = false;
        IsShutdownExpanded = false;
        IsBootRestartExpanded = false;
        IsScreenWakeExpanded = false;
        EnsureDefaultScheme(FeatureType.DesktopBackground);
        SelectedFeature = "DesktopBackground";
        LoadFeatureContent("DesktopBackground");
    }
}
```

**New code:**
```csharp
partial void OnIsDesktopBackgroundExpandedChanged(bool value)
{
    if (value)
    {
        IsMouseClickExpanded = false;
        IsShutdownExpanded = false;
        IsBootRestartExpanded = false;
        IsScreenWakeExpanded = false;
        EnsureDefaultScheme(FeatureType.DesktopBackground);
        SelectedFeature = "DesktopBackground";
        LoadFeatureContent("DesktopBackground");
    }
    else
    {
        // Collapsed: clear highlight if this feature is not selected
        if (SelectedFeature != "DesktopBackground")
        {
            // Deselect any selected scheme for this feature
            if (SelectedDesktopBackgroundScheme != null)
            {
                SelectedDesktopBackgroundScheme.IsSelected = false;
                SelectedDesktopBackgroundScheme = null;
            }
        }
    }
}
```

**Logic explanation:**
- When expanded (value=true): Keep existing logic
- When collapsed (value=false): Check if DesktopBackground is the current SelectedFeature
  - If NOT selected: Deselect any scheme and clear SelectedDesktopBackgroundScheme
  - This causes IsDesktopBackgroundHeaderHighlighted to return false
  - If IS selected: Do nothing, highlight remains via SelectedFeature == "DesktopBackground"
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - OnIsDesktopBackgroundExpandedChanged has else branch for value=false
    - Else branch checks SelectedFeature != "DesktopBackground"
    - Else branch clears SelectedDesktopBackgroundScheme when appropriate
    - Existing expansion logic preserved
  </done>
</task>

<task type="auto" tdd="false">
  <name>Task 2: Fix MouseClick, Shutdown, BootRestart, ScreenWake Expandable Highlights</name>
  <files>ViewModels/CreatorViewModel.cs</files>
  <action>
Apply the same pattern to the remaining 4 expandable features:

**MouseClick (lines 67-84):**
```csharp
partial void OnIsMouseClickExpandedChanged(bool value)
{
    if (value)
    {
        IsDesktopBackgroundExpanded = false;
        IsShutdownExpanded = false;
        IsBootRestartExpanded = false;
        IsScreenWakeExpanded = false;
        EnsureDefaultScheme(FeatureType.MouseClick);
        SelectedFeature = "MouseClick";
        LoadFeatureContent("MouseClick");
    }
    else
    {
        if (SelectedFeature != "MouseClick")
        {
            if (SelectedMouseClickScheme != null)
            {
                SelectedMouseClickScheme.IsSelected = false;
                SelectedMouseClickScheme = null;
            }
        }
    }
}
```

**Shutdown (lines 89-106):**
```csharp
partial void OnIsShutdownExpandedChanged(bool value)
{
    if (value)
    {
        IsDesktopBackgroundExpanded = false;
        IsMouseClickExpanded = false;
        IsBootRestartExpanded = false;
        IsScreenWakeExpanded = false;
        EnsureDefaultScheme(FeatureType.Shutdown);
        SelectedFeature = "Shutdown";
        LoadFeatureContent("Shutdown");
    }
    else
    {
        if (SelectedFeature != "Shutdown")
        {
            if (SelectedShutdownScheme != null)
            {
                SelectedShutdownScheme.IsSelected = false;
                SelectedShutdownScheme = null;
            }
        }
    }
}
```

**BootRestart (lines 111-128):**
```csharp
partial void OnIsBootRestartExpandedChanged(bool value)
{
    if (value)
    {
        IsDesktopBackgroundExpanded = false;
        IsMouseClickExpanded = false;
        IsShutdownExpanded = false;
        IsScreenWakeExpanded = false;
        EnsureDefaultScheme(FeatureType.BootRestart);
        SelectedFeature = "BootRestart";
        LoadFeatureContent("BootRestart");
    }
    else
    {
        if (SelectedFeature != "BootRestart")
        {
            if (SelectedBootRestartScheme != null)
            {
                SelectedBootRestartScheme.IsSelected = false;
                SelectedBootRestartScheme = null;
            }
        }
    }
}
```

**ScreenWake (lines 133-150):**
```csharp
partial void OnIsScreenWakeExpandedChanged(bool value)
{
    if (value)
    {
        IsDesktopBackgroundExpanded = false;
        IsMouseClickExpanded = false;
        IsShutdownExpanded = false;
        IsBootRestartExpanded = false;
        EnsureDefaultScheme(FeatureType.ScreenWake);
        SelectedFeature = "ScreenWake";
        LoadFeatureContent("ScreenWake");
    }
    else
    {
        if (SelectedFeature != "ScreenWake")
        {
            if (SelectedScreenWakeScheme != null)
            {
                SelectedScreenWakeScheme.IsSelected = false;
                SelectedScreenWakeScheme = null;
            }
        }
    }
}
```

**Pattern to follow for each:**
1. Keep existing `if (value)` block exactly as-is
2. Add `else` block after it
3. In else: check `if (SelectedFeature != "FeatureName")`
4. Inside that: if SelectedXxxScheme != null, set IsSelected=false and set property to null
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - All 4 remaining partial methods updated with else branches
    - Each else branch follows the same pattern as DesktopBackground
    - Each checks SelectedFeature against its feature name
    - Each clears its SelectedXxxScheme property when appropriate
    - All 5 expandable features now have consistent behavior
  </done>
</task>

</tasks>

<verification>
Build verification:
```bash
dotnet build
```

Runtime verification:
1. Run application
2. Click Desktop Background to expand → header highlights
3. Click elsewhere to collapse → highlight removed
4. Click Desktop Background to expand → click a scheme → header stays highlighted
5. Click Shutdown to expand (Desktop Background auto-collapses) → only Shutdown highlighted
6. Repeat for all 5 expandable features
</verification>

<success_criteria>
- [ ] All 5 OnIsXXXExpandedChanged partial methods have else branches
- [ ] Else branches clear scheme selection when feature not selected
- [ ] Header highlight is removed when expandable collapses (unless feature selected)
- [ ] Build succeeds with 0 errors
- [ ] Only one expandable header highlighted at a time during normal navigation
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v2/01-fix-v2-02-SUMMARY.md`
</output>
