---
phase: 01-fix-v4
plan: 02
type: execute
wave: 2
depends_on:
  - 01-fix-v4-01
files_modified:
  - Resources/Theme.xaml
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-V4-003
  - FIX-V4-004
must_haves:
  truths:
    - "Scheme items show visible highlight with 35% opacity background"
    - "Selected scheme item text appears bold (SemiBold)"
    - "Arrows are vertically centered in both collapsed and expanded states"
    - "Scrollbar button compression fix is verified and working"
    - "Only one navigation button is highlighted at any time"
  artifacts:
    - path: "Resources/Theme.xaml"
      provides: "Enhanced SchemeSelectedBrush with 35% opacity"
      contains: "SchemeSelectedBrush Opacity=\"0.35\""
    - path: "Views/CreatorView.xaml"
      provides: "SchemeItemTemplate with bold text trigger"
      contains: "FontWeight=\"SemiBold\" in IsSelected DataTrigger"
    - path: "Views/CreatorView.xaml"
      provides: "Arrow containers with correct vertical centering"
      contains: "Grid Height=\"40\" for all 5 expandable feature arrows"
  key_links:
    - from: "SchemeItemTemplate DataTrigger"
      to: "SchemeSelectedBrush"
      via: "IsSelected binding"
      pattern: "DataTrigger Binding=\"{Binding IsSelected}\" Value=\"True\""
    - from: "Expandable feature ToggleButton"
      to: "Arrow Path"
      via: "Grid container with Height=40"
      pattern: "Grid Grid.Column=\"2\" Width=\"16\" Height=\"40\" VerticalAlignment=\"Center\""
---

<objective>
Fix UI polish issues: scheme selection highlight visibility, arrow vertical alignment, and verify navigation/scrollbar fixes.

**Purpose:** While Plan 01 fixes content display, this plan addresses the remaining visual issues: scheme selection needs better visibility (35% opacity + bold text), arrows need proper vertical centering when ToggleButton expands/collapses, and navigation/scrollbar fixes need final verification.

**Output:**
- Updated `Resources/Theme.xaml` with SchemeSelectedBrush Opacity="0.35"
- Updated `Views/CreatorView.xaml` with:
  - SchemeItemTemplate bold text when selected
  - Arrow Grid containers with Height="40" for all 5 expandable features
  - Verified navigation highlight and scrollbar button compression
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/ROADMAP.md
@.planning/phases/01-fix-v4/01-fix-v4-CONTEXT.md
@.planning/phases/01-fix-v4/01-fix-v4-01-SUMMARY.md

<!-- Key source files -->
@E:/Projects/ProductivityWallpaper/Resources/Theme.xaml
@E:/Projects/ProductivityWallpaper/Views/CreatorView.xaml
@E:/Projects/ProductivityWallpaper/ViewModels/CreatorViewModel.cs

<interfaces>
<!-- Current SchemeSelectedBrush in Theme.xaml (line 21) -->
```xml
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="#6F7CFF" Opacity="0.2"/>
```

<!-- Current SchemeItemTemplate structure (needs to be located in CreatorView.xaml) -->
Look for DataTemplate with x:Key="SchemeItemTemplate" - it should have:
- DataTrigger for IsSelected
- Currently sets Background to SchemeSelectedBrush
- Needs FontWeight setter added

<!-- Current Arrow structure (5 locations in CreatorView.xaml) -->
Lines ~257, ~336, ~414, ~492, ~570 (approximate)
```xml
<Grid Grid.Column="2" Width="16" Height="16" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9" ... />
</Grid>
```

<!-- Navigation state properties in CreatorViewModel.cs -->
- IsThemePreviewActive
- IsDesktopBackgroundActive
- IsMouseClickActive
- IsDesktopClockActive
- IsPomodoroActive
- IsAnniversaryActive
- IsShutdownActive
- IsBootRestartActive
- IsScreenWakeActive

- IsDesktopBackgroundHeaderHighlighted
- IsMouseClickHeaderHighlighted
- IsShutdownHeaderHighlighted
- IsBootRestartHeaderHighlighted
- IsScreenWakeHeaderHighlighted

<!-- ScrollViewer configuration (already implemented) -->
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,12,0">
    <StackPanel Margin="16,8,4,16">
        <Button MinWidth="200" ... />
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Update SchemeSelectedBrush opacity and SchemeItemTemplate bold text</name>
  <files>Resources/Theme.xaml, Views/CreatorView.xaml</files>
  <action>
1. **Update Theme.xaml** - Change SchemeSelectedBrush opacity from 0.2 to 0.35:

**Current (line 21):**
```xml
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="#6F7CFF" Opacity="0.2"/>
```

**New:**
```xml
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="#6F7CFF" Opacity="0.35"/>
```

2. **Update CreatorView.xaml** - Find SchemeItemTemplate and add FontWeight trigger:

Locate the DataTemplate with x:Key="SchemeItemTemplate". It should look something like:
```xml
<DataTemplate x:Key="SchemeItemTemplate">
    <Button Command="{Binding DataContext.SelectSchemeCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"
            CommandParameter="{Binding}"
            Style="{StaticResource SchemeItemButtonStyle}">
        <TextBlock Text="{Binding Name}" ... />
    </Button>
</DataTemplate>
```

If SchemeItemButtonStyle exists, update it to include FontWeight:
```xml
<Style x:Key="SchemeItemButtonStyle" TargetType="Button">
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

If the style doesn't exist and the trigger is inline in the template, update accordingly.

**Critical:**
- Only modify the IsSelected DataTrigger
- Keep IsMouseOver trigger unchanged
- FontWeight changes from Normal to SemiBold
- Background uses the updated SchemeSelectedBrush (now 35% opacity)
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - Theme.xaml SchemeSelectedBrush has Opacity="0.35"
    - CreatorView.xaml SchemeItemTemplate/style has FontWeight="SemiBold" in IsSelected trigger
    - Build succeeds with no errors
    - XAML designer loads without errors
  </done>
</task>

<task type="auto">
  <name>Task 2: Fix arrow vertical alignment in all 5 expandable features</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Update the arrow Grid containers for all 5 expandable features. Each has a Grid with the arrow Path inside.

**Current structure (5 locations):**
```xml
<Grid Grid.Column="2" Width="16" Height="16" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9" ... />
</Grid>
```

**New structure (change Height from 16 to 40):**
```xml
<Grid Grid.Column="2" Width="16" Height="40" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9"
          Width="16"
          Height="16"
          VerticalAlignment="Center"
          HorizontalAlignment="Center"
          Stretch="Uniform">
        <Path.RenderTransform>
            <RotateTransform Angle="0" CenterX="8" CenterY="8"/>
        </Path.RenderTransform>
        ... (existing Style with triggers)
    </Path>
</Grid>
```

**Locations to update (approximate line numbers, verify in file):**

1. **Desktop Background** (~line 257):
   - Change `Height="16"` to `Height="40"`
   - Ensure Path has Width="16" Height="16" VerticalAlignment="Center" HorizontalAlignment="Center"

2. **Mouse Click** (~line 336):
   - Same changes as above

3. **Shutdown** (~line 414):
   - Same changes as above

4. **Boot Restart** (~line 492):
   - Same changes as above

5. **Screen Wake** (~line 570):
   - Same changes as above

**Why Height="40"?**
- Button height is 40px (from FeatureButtonStyle)
- Grid container height of 40px matches button height
- Path inside is 16px and centered
- This ensures arrow rotates around visual center when expanded

**Important:** Don't modify the RotateTransform triggers - they should remain unchanged.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - All 5 expandable features have Grid Height="40" for arrow containers
    - All Path elements have explicit Width="16" Height="16" VerticalAlignment="Center" HorizontalAlignment="Center"
    - Build succeeds with no errors
    - XAML designer loads without errors
  </done>
</task>

<task type="auto">
  <name>Task 3: Verify navigation highlight logic and scrollbar implementation</name>
  <files>Views/CreatorView.xaml, ViewModels/CreatorViewModel.cs</files>
  <action>
**This is a verification task - no changes needed, just confirm existing implementation is correct.**

1. **Navigation Highlight Verification:**

In CreatorViewModel.cs, verify these properties exist (they should already be implemented):
```csharp
// Simple buttons use IsXXXActive
public bool IsThemePreviewActive => CurrentState == CreatorViewState.ThemePreview;
public bool IsDesktopClockActive => CurrentState == CreatorViewState.DesktopClock;
public bool IsPomodoroActive => CurrentState == CreatorViewState.Pomodoro;
public bool IsAnniversaryActive => CurrentState == CreatorViewState.Anniversary;

// Expandable headers use IsXXXHeaderHighlighted
public bool IsDesktopBackgroundHeaderHighlighted => 
    IsDesktopBackgroundActive || SelectedDesktopBackgroundScheme != null;
public bool IsMouseClickHeaderHighlighted => 
    IsMouseClickActive || SelectedMouseClickScheme != null;
// ... similar for Shutdown, BootRestart, ScreenWake
```

In CreatorView.xaml, verify buttons use these properties:
```xml
<!-- Simple button example -->
<Button Style="{Binding IsDesktopClockActive, Converter={StaticResource BooleanToFeatureButtonConverter}}"/>

<!-- Expandable header example -->
<ToggleButton>
    <ToggleButton.Style>
        <Style TargetType="ToggleButton" BasedOn="{StaticResource FeatureExpanderButtonStyle}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsDesktopBackgroundHeaderHighlighted}" Value="True">
                    <Setter Property="Background" Value="{StaticResource PrimaryGradientBrush}"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </ToggleButton.Style>
</ToggleButton>
```

2. **Scrollbar Verification:**

In CreatorView.xaml, verify ScrollViewer has correct configuration:
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,12,0">
    <StackPanel Margin="16,8,4,16">
        <!-- Buttons have MinWidth to prevent compression -->
        <Button MinWidth="200" ... />
```

**Verification checklist:**
- [ ] ScrollViewer has Padding="0,0,12,0" (reserves scrollbar space)
- [ ] All navigation buttons have MinWidth="200" or similar
- [ ] ScrollBar style in Theme.xaml has proper styling (Thumb with CornerRadius)
- [ ] Navigation uses unified CurrentState approach (not multiple booleans)

**If any issues found:**
- Document them in the SUMMARY.md
- Note that scrollbar was already implemented in previous fix rounds
- Note that navigation highlight was already implemented
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - CreatorViewModel.cs has IsXXXActive and IsXXXHeaderHighlighted properties
    - CreatorView.xaml uses BooleanToFeatureButtonConverter for simple buttons
    - CreatorView.xaml uses IsXXXHeaderHighlighted for expandable headers
    - ScrollViewer has Padding="0,0,12,0"
    - Navigation buttons have MinWidth to prevent compression
    - Build succeeds with no errors
  </done>
</task>

</tasks>

<verification>
After all tasks complete, verify:

1. **Build verification:**
   - `dotnet build` succeeds with no errors
   - No XAML designer errors

2. **Visual verification (manual):**
   Launch application and navigate to "创作主题" (Creator) page:

   **Scheme Highlight:**
   - Expand Desktop Background
   - Click on a scheme item
   - Verify: Background changes to visible blue (35% opacity)
   - Verify: Text becomes bold
   - Click another scheme
   - Verify: Only one scheme highlighted at a time

   **Arrow Position:**
   - Desktop Background arrow should be vertically centered
   - Click to expand
   - Verify: Arrow stays centered while rotating 180°
   - Click to collapse
   - Verify: Arrow returns to centered position
   - Repeat for Mouse Click, Shutdown, Boot Restart, Screen Wake

   **Navigation Highlight:**
   - Click Desktop Background
   - Verify: Desktop Background header is highlighted
   - Click Desktop Clock
   - Verify: Only Desktop Clock is highlighted
   - Verify: Desktop Background is no longer highlighted
   - Expand Mouse Click, click a scheme
   - Verify: Mouse Click header stays highlighted

   **Scrollbar:**
   - Resize window to make navigation panel shorter
   - Verify: Scrollbar appears when content overflows
   - Verify: Buttons don't compress (maintain MinWidth)
   - Verify: No layout shift when scrollbar appears/disappears

3. **Output window check:**
   - No binding errors
   - No DataTemplate warnings
</verification>

<success_criteria>
- [ ] Theme.xaml SchemeSelectedBrush has Opacity="0.35" (was 0.2)
- [ ] CreatorView.xaml SchemeItemTemplate has FontWeight="SemiBold" for IsSelected
- [ ] All 5 expandable features have arrow Grid Height="40"
- [ ] All arrow Path elements have explicit dimensions and center alignment
- [ ] Navigation highlight uses unified CurrentState approach (verified)
- [ ] Scrollbar has Padding="0,0,12,0" (verified)
- [ ] Build succeeds with no errors
- [ ] Scheme items show visible highlight (background + bold text)
- [ ] Arrows are vertically centered in both states
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v4/01-fix-v4-02-SUMMARY.md`
</output>
