# Phase 01-fix-v3: Creator View Root Cause Fixes - Context

**Gathered:** 2026-03-15  
**Status:** Ready for planning  
**Attempts:** 5th fix round - **ROOT CAUSE ANALYSIS COMPLETE**

---

<domain>
## Phase Boundary

**This is the definitive fix attempt.** Based on runtime log analysis, ViewModels ARE being created successfully, but:
1. ContentControl not displaying content despite ViewModel assignment
2. Navigation selection logic is fundamentally broken (multiple highlights, inconsistent state)
3. Image binding errors preventing View rendering

**Scope:**
- Fix unified navigation selection (single source of truth)
- Fix ContentControl/ContentPresenter content display
- Fix ImageSource binding errors
- Add scheme item highlight
- Fix arrow position
- Fix scrollbar button compression

**In-Scope:**
- Navigation state management rewrite
- Content display mechanism fix
- DataTemplate verification
- Image binding converter
- UI polish (highlight, arrow, scrollbar)

**Out-of-Scope:**
- New features
- Major architecture changes
- Performance optimization

</domain>

---

<decisions>
## Implementation Decisions

### 1. ContentControl Not Displaying (CRITICAL - P0)

**Evidence from Logs:**
```
[NavigationMonitor] SUCCESS: DesktopBackground -> DesktopBackgroundViewModel
[NavigationMonitor] SUCCESS: DesktopClock -> DesktopClockViewModel
```
ViewModels ARE created successfully, but user sees nothing.

**Root Cause Hypotheses:**
1. **ContentControl.ContentTemplate not set** - WPF uses DataTemplate from resources automatically for ContentControl.Content, but only if ContentTemplate is null. If ContentTemplate is set but wrong, DataTemplate won't apply.

2. **DataTemplate DataType mismatch** - DataTemplate DataType="{x:Type vm:DesktopBackgroundViewModel}" must match exact type including namespace.

3. **ContentPresenter vs ContentControl** - CreatorView.xaml uses ContentControl, but if DataTemplates aren't resolving, we may need explicit ContentTemplate or switch to ContentPresenter.

**Decision:**
```xml
<!-- Fix 1: Ensure DataTemplates have exact type matches -->
<DataTemplate DataType="{x:Type vm:DesktopBackgroundViewModel}">
    <views:DesktopBackgroundView/>
</DataTemplate>

<!-- Fix 2: Use ContentPresenter instead of ContentControl (better DataTemplate resolution) -->
<ContentPresenter Content="{Binding ConfigurationContent}"
                  ContentTemplateSelector="{StaticResource ViewModelTemplateSelector}"/ >

<!-- Fix 3: OR explicitly set ContentTemplate to null to allow DataTemplate lookup -->
<ContentControl Content="{Binding ConfigurationContent}"
                ContentTemplate="{x:Null}"/ >
```

### 2. Unified Navigation Selection (CRITICAL - P0)

**Current Problem:**
- Multiple properties track selection: `SelectedFeature`, `IsXXXSelected`, `IsXXXExpanded`, `SelectedXXXScheme`
- No single source of truth
- Expandable buttons and simple buttons use different logic

**New Design - Single Enum State:**
```csharp
public enum CreatorViewState
{
    ThemePreview,
    DesktopBackground,
    MouseClick,
    DesktopClock,
    Pomodoro,
    Anniversary,
    Shutdown,
    BootRestart,
    ScreenWake,
    OpenApp
}

[ObservableProperty]
private CreatorViewState _currentState = CreatorViewState.ThemePreview;

// Computed properties for XAML binding
public bool IsThemePreviewActive => CurrentState == CreatorViewState.ThemePreview;
public bool IsDesktopBackgroundActive => CurrentState == CreatorViewState.DesktopBackground;
// etc.
```

**Highlight Logic:**
- ALL navigation buttons bind to `IsXXXActive` (single computed property)
- Expandable headers: `IsXXXActive || (IsXXXExpanded && SelectedXXXScheme != null)`
- Scheme items: `SelectedXXXScheme == scheme`

**State Transitions:**
```
Click Simple Button → Set CurrentState → LoadContent → Clear all scheme selections
Click Expandable Header → Set CurrentState → Expand → LoadContent → Select default scheme if none
Click Scheme Item → Set CurrentState → Keep expanded → LoadContent → Set scheme selection
Collapse Expandable → Clear scheme selection if feature not active
```

### 3. ImageSource Binding Errors (P1)

**Error from Log:**
```
Cannot convert '' from type 'String' to type 'System.Windows.Media.ImageSource'
```

**Root Cause:** ClockStyleModel.PreviewImagePath is empty string, Image tries to bind directly to string.

**Fix:** Add StringToImageSourceConverter
```csharp
public class StringToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            try {
                return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            } catch {
                return DependencyProperty.UnsetValue;
            }
        }
        return DependencyProperty.UnsetValue; // Use fallback
    }
    // ...
}
```

### 4. Scheme Item Highlight (P1)

**Fix:** Add Background trigger to SchemeItemTemplate
```xml
<Style TargetType="Button" x:Key="SchemeItemButtonStyle">
    <Setter Property="Background" Value="Transparent"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding IsSelected}" Value="True">
            <Setter Property="Background" 
                    Value="{StaticResource AccentBrush}"/ >
        </DataTrigger>
    </Style.Triggers>
</Style>
```

### 5. Arrow Position (P2)

**Issue:** Arrow shifts down when ToggleButton IsChecked (expanded)

**Fix:** Ensure Grid container maintains center alignment
```xml
<Grid VerticalAlignment="Center">  <!-- Add this to arrow container -->
    <Path VerticalAlignment="Center" ... />
</Grid>
```

### 6. Scrollbar Button Compression (P2)

**Fix:** Set MinWidth on navigation buttons and ensure ScrollViewer.Padding
```xml
<ScrollViewer Padding="0,0,12,0">  <!-- Reserve scrollbar space -->
    <Button MinWidth="200" ... />  <!-- Prevent compression -->
</ScrollViewer>
```

</decisions>

---

<code_context>
## Code Analysis

### Current Problem Areas:

**1. ContentControl in CreatorView.xaml (line ~796):**
```xml
<ContentControl Content="{Binding ConfigurationContent}"/ >
```
- No explicit ContentTemplate
- Relies on implicit DataTemplate lookup
- May not be resolving correctly

**2. LoadFeatureContent in CreatorViewModel.cs:**
- Successfully creates ViewModels (per log)
- Sets ConfigurationContent = viewModel
- But user sees nothing

**3. DataTemplates in App.xaml:**
```xml
<DataTemplate DataType="{x:Type vm:DesktopBackgroundViewModel}">
    <views:DesktopBackgroundView/>
</DataTemplate>
```
- Appears correct
- But may have namespace/type mismatch

**4. Navigation State Management:**
- Too many boolean flags
- No single source of truth
- Inconsistent highlighting logic

### Working Components:
- ViewModel factories ✓
- NavigationMonitor logging ✓
- Scheme collections ✓
- Basic XAML structure ✓

### Required New Components:
1. **CreatorViewState enum** - Single selection state
2. **StringToImageSourceConverter** - Fix image binding
3. **ViewModelTemplateSelector** (optional) - Explicit template selection
4. **Scheme highlight style** - Visual feedback

</code_context>

---

<specifics>
## Specific Requirements

### P0: Content Display Fix
**Diagnostic Steps:**
1. Add explicit `ContentTemplate="{x:Null}"` to ContentControl
2. Verify DataTemplate DataType matches exactly
3. Try switching to ContentPresenter
4. Check Output window for binding errors on ContentControl
5. Add Debug.WriteLine after ConfigurationContent assignment to verify type

**Expected Result:**
Click Desktop Background → ContentControl displays DesktopBackgroundView

### P0: Unified Navigation State
**Implementation:**
1. Create `CreatorViewState` enum with 9 values
2. Replace all boolean flags with single `CurrentState` property
3. Add computed `IsXXXActive` properties for binding
4. Update all button bindings to use `IsXXXActive`
5. Ensure ONLY ONE `IsXXXActive` is true at any time

**State Machine:**
```
CurrentState = DesktopBackground
→ IsThemePreviewActive = false
→ IsDesktopBackgroundActive = true  
→ All others = false
```

### P1: Image Binding Fix
**Converter Implementation:**
- Handle null/empty strings
- Handle invalid paths
- Return UnsetValue for fallback
- Register converter in App.xaml

### P1: Scheme Highlight
**Visual Design:**
- Selected: AccentBrush background
- Unselected: Transparent
- Transition: Instant (no animation for now)

### P2: Arrow & Scrollbar
**Arrow:** Center vertically in 40px button
**Scrollbar:** Reserve 12px right padding

</specifics>

---

<deferred>
## Deferred Ideas

- Animation for state transitions
- Advanced error recovery
- Alternative navigation patterns (accordion, tabs)
- Performance optimization (virtualization)

</deferred>

---

<implementation_notes>
## Implementation Notes for Planner

### This is the 5th Attempt - Must Succeed:

**Previous attempts failed because:**
1. Didn't realize ViewModels were already being created successfully
2. Focused on wrong layer (ViewModel creation vs content display)
3. Navigation state was too complex with multiple booleans

**This attempt focuses on:**
1. ContentControl/ContentPresenter display mechanism
2. Single enum state (not multiple booleans)
3. Image binding converter
4. Scheme item highlight style

### Testing Strategy:
1. Build and run
2. Open Debug output window
3. Click Desktop Background
4. Verify log shows SUCCESS
5. **CRITICAL:** Verify actual content displays in right panel
6. Check only one nav button is highlighted
7. Test all 9 buttons

### Success Criteria:
- [ ] All 9 features display content (not just log SUCCESS)
- [ ] Only one navigation button highlighted at a time
- [ ] Scheme items show highlight when selected
- [ ] No ImageSource binding errors in Output
- [ ] Arrow centered vertically
- [ ] Scrollbar doesn't compress buttons

### If This Fails:
- The issue is deeper (WPF airspace, HwndHost, etc.)
- May need to rewrite CreatorView from scratch
- Consider switching to different UI framework

</implementation_notes>

---

*Phase: 01-fix-v3*  
*Context gathered: 2026-03-15*  
*Root cause: ContentControl not displaying despite ViewModel creation*
