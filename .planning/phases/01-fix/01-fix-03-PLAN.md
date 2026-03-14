---
phase: 01-fix
plan: 03
type: execute
wave: 1
depends_on: []
files_modified:
  - ViewModels/CreatorViewModel.cs
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-03
must_haves:
  truths:
    - Only one navigation button is highlighted at a time (single-highlight)
    - When scheme selected, BOTH scheme AND parent header are highlighted
    - Simple feature clicks collapse all submenus and highlight the feature
  artifacts:
    - path: "ViewModels/CreatorViewModel.cs"
      provides: "Dual-highlight logic for parent+child selection"
      section: "SelectScheme method and new navigation state properties"
    - path: "Views/CreatorView.xaml"
      provides: "DataTrigger bindings for parent header highlight"
  key_links:
    - from: "SchemeModel.IsSelected"
      to: "Parent Feature header background"
      pattern: "Scheme selection → highlight parent via computed property"
---

<objective>
Fix navigation highlight consistency - ensure expandable headers stay highlighted when child scheme is selected.

**Purpose:** UX clarity - users need to see their current location in the navigation hierarchy. When viewing a scheme, both the scheme item AND its parent feature header should be highlighted.

**Output:** Unified single-highlight with dual-highlight for parent+child relationships.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/phases/01-fix/01-CONTEXT.md
@ViewModels/CreatorViewModel.cs
@Views/CreatorView.xaml

## Current State Analysis

**Current Highlight Logic:**
- `SelectedFeature` property determines which feature button is highlighted
- `BooleanToFeatureButtonConverter` selects button style based on `IsXXXSelected` computed properties
- Single-highlight: only one `IsXXXSelected` is true at a time

**Problem:**
When a scheme is selected, the parent feature header loses highlight because `SelectedFeature` changes to something like "DesktopBackground", but the highlight logic doesn't account for scheme selection state.

**Required Behavior (from CONTEXT.md):**
1. **Non-expandable click**: Collapse all + highlight button + show page
2. **Expandable header click**: Expand + highlight header + show default scheme
3. **Scheme item click**: Keep expanded + highlight scheme + highlight parent header + show scheme page
4. **Dual-highlight**: When scheme selected, both scheme AND parent header highlighted

**Key Insight:**
The expandable header should stay highlighted whenever:
- The header itself is clicked (direct selection)
- OR any of its child schemes is selected

This requires computed properties like:
- `IsDesktopBackgroundHeaderHighlighted` = `SelectedFeature == "DesktopBackground"` OR `SelectedDesktopBackgroundScheme?.IsSelected == true`
</context>

<tasks>

<task type="auto">
  <name>Implement Dual-Highlight Navigation Logic</name>
  <files>ViewModels/CreatorViewModel.cs, Views/CreatorView.xaml</files>
  <action>
Implement navigation highlight consistency with dual-highlight for parent headers when child scheme is selected.

**Step 1: Add Computed Highlight Properties to CreatorViewModel.cs**

Add computed properties that return true when the expandable header should be highlighted (either directly selected OR has selected child):

```csharp
/// <summary>
/// Gets whether Desktop Background header should be highlighted.
/// True when directly selected or when any of its schemes is selected.
/// </summary>
public bool IsDesktopBackgroundHeaderHighlighted => 
    SelectedFeature == "DesktopBackground" || 
    (SelectedDesktopBackgroundScheme?.IsSelected == true);

public bool IsMouseClickHeaderHighlighted => 
    SelectedFeature == "MouseClick" || 
    (SelectedMouseClickScheme?.IsSelected == true);

public bool IsShutdownHeaderHighlighted => 
    SelectedFeature == "Shutdown" || 
    (SelectedShutdownScheme?.IsSelected == true);

public bool IsBootRestartHeaderHighlighted => 
    SelectedFeature == "BootRestart" || 
    (SelectedBootRestartScheme?.IsSelected == true);

public bool IsScreenWakeHeaderHighlighted => 
    SelectedFeature == "ScreenWake" || 
    (SelectedScreenWakeScheme?.IsSelected == true);
```

Add `[NotifyPropertyChangedFor]` attributes to the SelectedXXXScheme properties so they trigger updates for the highlight properties.

**Step 2: Modify SelectScheme Method**

Update the `SelectScheme` method (lines 472-513) to:
1. Set scheme.IsSelected = true
2. Set SelectedFeature to the parent feature name (so header gets highlighted)
3. Update SelectedXXXScheme property
4. Load the feature content

Current code clears IsSelected on all schemes - modify to keep the selected one highlighted:

```csharp
[RelayCommand]
private void SelectScheme(SchemeModel? scheme)
{
    if (scheme == null)
        return;

    var featureType = scheme.FeatureType;

    // Deactivate all schemes for this feature (IsActive = false)
    // BUT don't clear IsSelected - we're selecting a new one
    if (_schemesByFeature.ContainsKey(featureType))
    {
        foreach (var existingScheme in _schemesByFeature[featureType])
        {
            existingScheme.IsActive = false;
            existingScheme.IsSelected = false;  // Clear previous selection
        }
    }

    // Activate and select the selected scheme
    scheme.IsActive = true;
    scheme.IsSelected = true;

    // Set the parent feature as selected (for header highlighting)
    SelectedFeature = featureType.ToString();  // "DesktopBackground", etc.

    // Update the selected scheme property and load content
    switch (featureType)
    {
        case FeatureType.DesktopBackground:
            SelectedDesktopBackgroundScheme = scheme;
            LoadFeatureContent("DesktopBackground");
            break;
        // ... etc for other features
    }
}
```

**Step 3: Update CreatorView.xaml ToggleButton Bindings**

Change the ToggleButton style bindings to use the new header highlight properties:

For Desktop Background ToggleButton (line 240-241), add a DataTrigger:

```xml
<ToggleButton Style="{StaticResource FeatureExpanderButtonStyle}"
              IsChecked="{Binding IsDesktopBackgroundExpanded, Mode=TwoWay}">
    <ToggleButton.Style>
        <Style TargetType="ToggleButton" BasedOn="{StaticResource FeatureExpanderButtonStyle}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsDesktopBackgroundHeaderHighlighted}" Value="True">
                    <Setter Property="Background" Value="{StaticResource PrimaryGradientBrush}"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </ToggleButton.Style>
    <!-- ... rest of content -->
</ToggleButton>
```

Actually, since FeatureExpanderButtonStyle already has an IsChecked trigger that sets PrimaryGradientBrush, we need to modify it to also trigger on the highlight property. The cleaner approach:

Create a new style that ORs the conditions, or modify the binding to use a multi-trigger:

```xml
<DataTrigger Binding="{Binding IsDesktopBackgroundHeaderHighlighted}" Value="True">
    <Setter Property="Background" Value="{StaticResource PrimaryGradientBrush}"/>
</DataTrigger>
```

Remove the IsChecked trigger from FeatureExpanderButtonStyle and handle all highlighting via the ViewModel properties.

**Simpler approach:**
Just add the DataTrigger for header highlighting. The IsChecked trigger will handle the expanded state, the new trigger will handle the "has selected child" state. Both can set the same background.

Apply this to all 5 expandable features.

**Step 4: Update Simple Feature Buttons**

For simple features (Clock, Pomodoro, Anniversary, OpenApp), they should:
1. Collapse all expanded submenus
2. Set SelectedFeature to themselves
3. Clear all scheme selections (IsSelected = false for all schemes)

Update `SelectFeature` method to clear scheme selections when selecting simple features.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /C:"error" && exit 1 || exit 0</automated>
  </verify>
  <done>Navigation highlight shows both parent header and selected scheme when scheme is selected; simple features clear all selections when clicked</done>
</task>

</tasks>

<verification>
Build verification: Project compiles without errors.
</verification>

<success_criteria>
- Only one "main" button highlighted at a time (single-highlight for simple features)
- When scheme selected: both scheme item AND parent header highlighted
- Clicking simple feature (Clock, etc.) collapses all submenus and highlights that feature
- Navigation state is visually clear at all times
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix/01-fix-03-SUMMARY.md`
</output>
