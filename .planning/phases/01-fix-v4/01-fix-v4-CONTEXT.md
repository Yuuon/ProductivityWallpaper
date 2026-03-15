# Phase 01-fix-v4: Creator View Final Fixes - Context

**Gathered:** 2026-03-15  
**Status:** Ready for planning (6th fix round)  
**Previous Attempts:** 5 fix rounds, root cause partially identified

---

<domain>
## Phase Boundary

**This is the 6th and final fix attempt.** After 5 previous rounds, we've identified that:
1. ViewModels ARE created successfully (proven by logs)
2. DataTemplates ARE defined in App.xaml
3. **Content still doesn't display** - this is a WPF DataTemplate resolution issue
4. UI polish issues persist (highlight, arrows, scrollbar)

**Scope:**
- Fix ContentTemplateSelector for reliable content display
- Finalize navigation highlight logic
- Fix scheme selection highlight visibility
- Fix arrow vertical alignment
- Verify scrollbar button compression fix

**In-Scope:**
- ContentTemplateSelector implementation (factory pattern)
- Navigation state verification (IsXXXActive properties)
- Scheme item highlight style enhancement
- Arrow position adjustment
- Build verification

**Out-of-Scope:**
- IsActive vs IsSelected distinction for schemes (deferred to data structure phase)
- Performance optimization
- New features
- Major architecture changes

</domain>

---

<decisions>
## Implementation Decisions

### 1. Content Display - ContentTemplateSelector (CRITICAL - P0)

**Problem:** All feature pages except Theme Preview don't display despite:
- ViewModels created successfully (NavigationMonitor logs show SUCCESS)
- DataTemplates defined in App.xaml
- ContentPresenter with ContentTemplate="{x:Null}"

**Root Cause Analysis:**
Previous 5 attempts focused on ViewModel creation (which works) but the issue is WPF's implicit DataTemplate lookup. ContentPresenter with `ContentTemplate="{x:Null}"` relies on implicit template resolution which may fail silently.

**Decision: Use Factory Pattern ContentTemplateSelector**

```csharp
public interface IContentTemplateSelectorFactory
{
    DataTemplate SelectTemplate(object item, DependencyObject container);
}

public class CreatorViewTemplateSelector : DataTemplateSelector
{
    private readonly Dictionary<Type, DataTemplate> _templates;
    
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item == null) return null;
        
        var itemType = item.GetType();
        if (_templates.TryGetValue(itemType, out var template))
        {
            return template;
        }
        
        // Fallback: try base types
        foreach (var kvp in _templates)
        {
            if (kvp.Key.IsAssignableFrom(itemType))
            {
                return kvp.Value;
            }
        }
        
        return null;
    }
}
```

**XAML Usage:**
```xml
<ContentPresenter Content="{Binding ConfigurationContent}"
                  ContentTemplateSelector="{StaticResource CreatorViewTemplateSelector}"/>
```

**Benefits:**
- Explicit mapping avoids implicit lookup failures
- Testable through factory injection
- Clear error messages when template not found
- Extensible for new ViewModels

---

### 2. Navigation Highlight Logic - Unified Toggle Approach

**Current Issue:** Simple buttons and expandable buttons use different highlight logic, causing inconsistency.

**Decision: Unified Navigation State**

**Principle:** All navigation items are logically in a single group - only one can be "active" at a time.

**Implementation:**

```csharp
// All buttons use IsXXXActive (computed from CurrentState)
public bool IsThemePreviewActive => CurrentState == CreatorViewState.ThemePreview;
public bool IsDesktopBackgroundActive => CurrentState == CreatorViewState.DesktopBackground;
// ... etc for all 9 features

// Expandable headers have additional highlight condition
public bool IsDesktopBackgroundHeaderHighlighted => 
    IsDesktopBackgroundActive || SelectedDesktopBackgroundScheme != null;
```

**Behavior Rules:**

1. **Simple Button Click:**
   - Set CurrentState to that feature
   - Collapse ALL expandable menus (CollapseAllNavExcept)
   - Load content

2. **Expandable Header Click:**
   - Expand this menu, collapse others
   - Set CurrentState
   - Auto-create default scheme if none exists
   - Load content with first/default scheme

3. **Scheme Item Click:**
   - Keep parent expanded
   - Set CurrentState to parent feature
   - Set SelectedXXXScheme
   - Load content with selected scheme

4. **Click Outside (or Other Button):**
   - Collapse expandable menus (except if current feature is expandable)
   - Clear scheme selection if feature not active

**XAML:**
```xml
<!-- Simple Button -->
<Button Style="{Binding IsDesktopClockActive, Converter={StaticResource BooleanToFeatureButtonConverter}}"/>

<!-- Expandable Header -->
<ToggleButton Style="{Binding IsDesktopBackgroundHeaderHighlighted, Converter={StaticResource BooleanToFeatureButtonConverter}}"/>
```

---

### 3. Scheme Selection Highlight - Enhanced Visibility

**Problem:** User reports "视觉上完全没变化" (visually no change) for submenu selection.

**Current:** SchemeSelectedBrush with 20% opacity + IsSelected DataTrigger

**Decision: Enhanced Highlight Style**

```xml
<Style x:Key="SchemeItemButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="FontWeight" Value="Normal"/>
    
    <Style.Triggers>
        <!-- Hover -->
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{StaticResource BackgroundHoverBrush}"/>
        </Trigger>
        
        <!-- Selected - Enhanced visibility -->
        <DataTrigger Binding="{Binding IsSelected}" Value="True">
            <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

**Theme.xaml Updates:**
```xml
<!-- Increase opacity from 20% to 35% for better visibility -->
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="#6F7CFF" Opacity="0.35"/>
```

**Requirements:**
- Background color change (35% opacity accent)
- Font weight: Normal → SemiBold when selected
- No animation (immediate transition)
- IsActive distinction deferred to data structure phase

---

### 4. Arrow Vertical Alignment - Centered Positioning

**Problem:** Arrow shifts down when ToggleButton is expanded (IsChecked). Measured to be below text baseline.

**Current Structure:**
```xml
<Grid Grid.Column="2" Width="16" Height="16" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9" VerticalAlignment="Center" ...>
        <Path.Style>
            <Style TargetType="Path">
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsDesktopBackgroundExpanded}" Value="True">
                        <Setter Property="RenderTransform">
                            <Setter.Value>
                                <RotateTransform Angle="180" CenterX="8" CenterY="8"/>
                            </Setter.Value>
                        </Setter>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Path.Style>
    </Path>
</Grid>
```

**Decision: Refined Vertical Centering**

**Analysis:**
- Button Height: 40px
- TextBlock: 14px font, Center vertical alignment
- Arrow Grid: 16x16, VerticalAlignment="Center"
- Path: VerticalAlignment="Center"

**Issue:** The 40px button height means the visual center is at 20px. But the text and arrow containers may have different effective heights.

**Fix:** Ensure consistent vertical center calculation

```xml
<!-- Use Canvas for precise positioning, or Grid with exact centering -->
<Grid Grid.Column="2" Width="16" Height="40" VerticalAlignment="Center">
    <!-- Height matches button, Path centered within -->
    <Path Data="M6 9L12 15L18 9" 
          Width="16" Height="16"
          VerticalAlignment="Center"
          HorizontalAlignment="Center"
          Stretch="Uniform">
        <Path.RenderTransform>
            <RotateTransform Angle="0" CenterX="8" CenterY="8"/>
        </Path.RenderTransform>
        <Path.Style>
            <Style TargetType="Path">
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsDesktopBackgroundExpanded}" Value="True">
                        <Setter Property="RenderTransform">
                            <Setter.Value>
                                <RotateTransform Angle="180" CenterX="8" CenterY="8"/>
                            </Setter.Value>
                        </Setter>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Path.Style>
    </Path>
</Grid>
```

**Alternative:** Use LayoutTransform instead of RenderTransform for better layout calculation, but this may affect animation.

**Verification:**
- Measure button center: 20px from top
- Arrow center when not rotated: 8px from top of its container
- When Grid is 40px tall and centered: arrow should be at visual center

---

### 5. Scrollbar Button Compression - Verified Fix

**Decision: Current implementation is correct**

**Current Implementation:**
```xml
<ScrollViewer Grid.Row="1" 
              VerticalScrollBarVisibility="Auto"
              Padding="0,0,12,0">
    <StackPanel Margin="16,8,4,16">
        <Button MinWidth="200" ... />
        <!-- ... other buttons -->
    </StackPanel>
</ScrollViewer>
```

**Verification:**
- ✅ Padding="0,0,12,0" reserves scrollbar space
- ✅ MinWidth="200" prevents button compression
- ✅ ScrollViewer only shows scrollbar when needed
- ✅ No width changes when scrollbar appears/disappears

**Status:** Already implemented and verified in previous fix rounds. No changes needed.

---

</decisions>

---

<code_context>
## Code Context

### Files to Modify:

1. **New File: `Converters/CreatorViewTemplateSelector.cs`**
   - Factory-based DataTemplateSelector
   - Maps ViewModel types to DataTemplates
   - Diagnostic logging

2. **Modified: `App.xaml`**
   - Register CreatorViewTemplateSelector as resource
   - Remove ContentTemplate="{x:Null}" from DataTemplates (if present)

3. **Modified: `Views/CreatorView.xaml`**
   - Line 782-783: Change ContentPresenter to use TemplateSelector
   - Verify SchemeItemTemplate DataTrigger
   - Verify arrow Grid structure

4. **Modified: `Resources/Theme.xaml`**
   - Line 21: Increase SchemeSelectedBrush opacity (0.2 → 0.35)
   - Verify FeatureButtonStyle and ActiveFeatureButtonStyle exist

5. **Verification: `ViewModels/CreatorViewModel.cs`**
   - Lines 276-321: Verify IsXXXActive properties
   - Lines 334-368: Verify IsXXXHeaderHighlighted logic
   - Lines 47-217: Verify OnIsXXXExpandedChanged logic

### Key Implementation Points:

**ContentTemplateSelector Integration:**
```csharp
// In App.xaml.cs or startup
services.AddSingleton<ICreatorViewTemplateSelectorFactory, CreatorViewTemplateSelectorFactory>();

// In CreatorViewTemplateSelector
public class CreatorViewTemplateSelectorFactory : ICreatorViewTemplateSelectorFactory
{
    private readonly Dictionary<Type, DataTemplate> _templates = new()
    {
        { typeof(DesktopBackgroundViewModel), Application.Current.Resources["DesktopBackgroundTemplate"] as DataTemplate },
        // ... other mappings
    };
    
    public DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        // Implementation
    }
}
```

**Scheme Highlight Verification:**
- Check SchemeModel.IsSelected is set in SelectScheme (line 681 in CreatorViewModel.cs)
- Verify DataTrigger binding path: `{Binding IsSelected}`
- Verify SchemeSelectedBrush resource exists and is applied

**Arrow Position Verification:**
- All 5 expandable features (DesktopBackground, MouseClick, Shutdown, BootRestart, ScreenWake)
- Each has Grid with Width="16" Height="40" (or 16) VerticalAlignment="Center"
- Each has Path with VerticalAlignment="Center"
- RotateTransform CenterX="8" CenterY="8" (for 16x16 arrow)

### Testing Checklist:

**Content Display:**
- [ ] Click Desktop Background → shows DesktopBackgroundView
- [ ] Click Desktop Clock → shows DesktopClockView
- [ ] Click Pomodoro → shows PomodoroView
- [ ] Click Anniversary → shows AnniversaryView
- [ ] Click all 9 buttons → each shows correct content

**Navigation Highlight:**
- [ ] Only one button highlighted at any time
- [ ] Expandable header highlighted when expanded
- [ ] Expandable header highlighted when child scheme selected
- [ ] Click simple button collapses all expandable menus

**Scheme Highlight:**
- [ ] Click scheme item → background changes (visible)
- [ ] Click scheme item → text becomes bold
- [ ] Only one scheme highlighted per feature

**Arrow Position:**
- [ ] Arrow vertically centered in collapsed state
- [ ] Arrow vertically centered in expanded state
- [ ] Arrow rotates correctly (180°)

**Scrollbar:**
- [ ] Scrollbar appears when content overflows
- [ ] Buttons don't compress when scrollbar appears
- [ ] Scrollbar space reserved (no layout shift)

</code_context>

---

<specifics>
## Specific Implementation Details

### P0: ContentTemplateSelector

**Interface Definition:**
```csharp
public interface IViewModelTemplateSelector
{
    DataTemplate SelectTemplate(object viewModel, FrameworkElement container);
}

public class CreatorViewTemplateSelector : DataTemplateSelector
{
    private readonly IViewModelTemplateSelector _selector;
    
    public CreatorViewTemplateSelector(IViewModelTemplateSelector selector)
    {
        _selector = selector;
    }
    
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return _selector?.SelectTemplate(item, container as FrameworkElement);
    }
}
```

**Factory Implementation:**
```csharp
public class ViewModelTemplateSelectorFactory : IViewModelTemplateSelector
{
    private readonly Dictionary<Type, DataTemplate> _templateCache = new();
    
    public DataTemplate SelectTemplate(object viewModel, FrameworkElement container)
    {
        if (viewModel == null) return null;
        
        var vmType = viewModel.GetType();
        
        if (!_templateCache.TryGetValue(vmType, out var template))
        {
            // Build resource key from type name
            var resourceKey = vmType.Name.Replace("ViewModel", "Template");
            template = Application.Current.Resources[resourceKey] as DataTemplate;
            
            if (template == null)
            {
                // Fallback: look for type-based template
                var fullKey = $"{vmType.Name}Template";
                template = Application.Current.Resources[fullKey] as DataTemplate;
            }
            
            _templateCache[vmType] = template;
        }
        
        return template;
    }
}
```

**DI Registration:**
```csharp
// In App.xaml.cs ConfigureServices
services.AddSingleton<IViewModelTemplateSelector, ViewModelTemplateSelectorFactory>();
services.AddSingleton<CreatorViewTemplateSelector>();
```

### P1: Scheme Highlight

**Style Enhancement:**
```xml
<!-- In Theme.xaml -->
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="#6F7CFF" Opacity="0.35"/>

<!-- In CreatorView.xaml -->
<Style TargetType="Button" x:Key="SchemeItemButtonStyle">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
    <Setter Property="FontWeight" Value="Normal"/>
    <Setter Property="FontSize" Value="13"/>
    
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{StaticResource BackgroundHoverBrush}"/>
        </Trigger>
        <DataTrigger Binding="{Binding IsSelected}" Value="True">
            <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

**Template Application:**
```xml
<ItemsControl ItemsSource="{Binding DesktopBackgroundSchemes}"
              ItemTemplate="{StaticResource SchemeItemTemplate}"
              ItemContainerStyle="{StaticResource SchemeItemButtonStyle}"/>
```

### P2: Arrow Position

**Container Height Adjustment:**
```xml
<!-- Change from 16 to 40 to match button height -->
<Grid Grid.Column="2" Width="16" Height="40" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9"
          Width="16" 
          Height="16"
          VerticalAlignment="Center"
          HorizontalAlignment="Center"
          Stretch="Uniform">
        <!-- RotateTransform as before -->
    </Path>
</Grid>
```

**Apply to all 5 expandable features:**
1. DesktopBackground (line ~257)
2. MouseClick (line ~336)
3. Shutdown (line ~414)
4. BootRestart (line ~492)
5. ScreenWake (line ~570)

</specifics>

---

<deferred>
## Deferred Ideas

**IsActive vs IsSelected for Schemes:**
- IsActive = scheme is enabled for wallpaper (checkmark)
- IsSelected = scheme is currently being edited (highlight)
- **Deferred to:** Phase 02 (Data Structure)
- **Reason:** Requires theme persistence and active scheme tracking

**Animation:**
- Smooth transitions for highlight changes
- Arrow rotation animation
- **Deferred to:** Quality phase (Phase 3)

**Alternative Navigation Patterns:**
- Accordion style (only one expanded at a time)
- Tab-based navigation
- **Deferred to:** Future enhancement

**Advanced Error Recovery:**
- Automatic ViewModel recreation on failure
- Fallback views for missing content
- **Deferred to:** Quality phase

</deferred>

---

<implementation_notes>
## Implementation Notes for Planner

### This is the 6th Attempt - Must Succeed

**Critical Success Factors:**
1. **ContentTemplateSelector** - This is the primary fix. Previous attempts failed because they relied on WPF's implicit template lookup which may fail silently.

2. **Factory Pattern** - Enables testing and clear error messages. Without this, debugging template resolution is nearly impossible.

3. **Verification** - Must verify ACTUAL content displays, not just logs. Logs show ViewModel creation works; we need to see the View.

### Build Order:
1. Create IViewModelTemplateSelector interface
2. Implement ViewModelTemplateSelectorFactory
3. Create CreatorViewTemplateSelector wrapper
4. Register in DI (App.xaml.cs)
5. Update CreatorView.xaml to use TemplateSelector
6. Update Theme.xaml (SchemeSelectedBrush opacity)
7. Update CreatorView.xaml (SchemeItemTemplate bold text)
8. Fix arrow positions in all 5 expandable features
9. Build and test all 9 buttons

### Risk Mitigation:
**Risk:** TemplateSelector doesn't work either
- **Mitigation:** Add extensive logging to trace template selection
- **Fallback:** Hardcode ContentTemplate in XAML (explicit template per feature)

**Risk:** Arrow position still wrong after fix
- **Mitigation:** Use Snoop or similar tool to inspect visual tree
- **Fallback:** Use Canvas with absolute positioning

### Success Criteria:
- [ ] All 9 buttons show content in right panel
- [ ] TemplateSelector logs show template selection
- [ ] No binding errors in Output window
- [ ] Only one navigation button highlighted
- [ ] Scheme items show visible highlight (background + bold)
- [ ] Arrows vertically centered in both states
- [ ] Scrollbar doesn't compress buttons

### If This Fails:
- Revert to explicit ContentTemplate assignment per feature
- Consider rewriting CreatorView from scratch
- Evaluate if WPF airspace/HwndHost issues are involved

</implementation_notes>

---

*Phase: 01-fix-v4*  
*Context gathered: 2026-03-15*  
*Attempts: 6 (final)*  
*Primary fix: ContentTemplateSelector with factory pattern*
