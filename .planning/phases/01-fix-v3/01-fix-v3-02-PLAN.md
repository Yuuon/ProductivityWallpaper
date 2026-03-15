---
phase: 01-fix-v3
plan: 02
type: execute
wave: 2
depends_on:
  - 01-fix-v3-01
files_modified:
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-V3-004
  - FIX-V3-005
must_haves:
  truths:
    - Scheme items show AccentBrush background when IsSelected=true
    - Arrow icons stay vertically centered in buttons
    - ScrollViewer padding reserves space for scrollbar
    - All navigation buttons maintain proper layout
  artifacts:
    - path: Views/CreatorView.xaml
      provides: SchemeItemTemplate with IsSelected trigger working
      pattern: DataTrigger.*IsSelected.*SchemeSelectedBrush
    - path: Views/CreatorView.xaml
      provides: Arrow Grid containers with VerticalAlignment="Center"
      pattern: Grid.*Width="16".*Height="16".*VerticalAlignment="Center"
    - path: Views/CreatorView.xaml
      provides: ScrollViewer with right padding
      pattern: ScrollViewer.*Padding="0,0,12,0"
  key_links:
    - from: SchemeModel.IsSelected
      to: SchemeItemTemplate background
      via: DataTrigger binding
    - from: Theme.xaml SchemeSelectedBrush
      to: SchemeItemTemplate trigger
      via: StaticResource reference
---

<objective>
Complete UI polish for Creator View - verify and fix scheme item highlight, ensure arrow positioning is correct, and confirm scrollbar doesn't compress buttons. These are visual refinements that depend on the navigation state being correct (from Plan 01).

Purpose: Polish the UI to match design intent with proper visual feedback.
Output: Working scheme highlights, properly positioned arrows, uncrowded navigation.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/phases/01-fix-v3/01-CONTEXT.md
@.planning/phases/01-fix-v3/01-fix-v3-01-PLAN.md

@E:/Projects/ProductivityWallpaper/Views/CreatorView.xaml
@E:/Projects/ProductivityWallpaper/Resources/Theme.xaml

<interfaces>
From CreatorView.xaml (existing SchemeItemTemplate):
```xml
<DataTemplate x:Key="SchemeItemTemplate" DataType="{x:Type models:SchemeModel}">
    <Button Command="{Binding DataContext.SelectSchemeCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            CommandParameter="{Binding}"
            Background="Transparent"
            ...>
        <Button.Style>
            <Style TargetType="Button">
                ...
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsSelected}" Value="True">
                        <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/ >
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Button.Style>
        <TextBlock Text="{Binding Name}" ... />
    </Button>
</DataTemplate>
```

From Theme.xaml (needs to verify exists):
```xml
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="..." />
<!-- OR -->
<Brush x:Key="SchemeSelectedBrush">...accent color...</Brush>
```

Current arrow markup (line 256):
```xml
<Grid Grid.Column="2" Width="16" Height="16" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9" VerticalAlignment="Center" ... />
</Grid>
```

Current ScrollViewer (line 206):
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,12,0">
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Verify SchemeSelectedBrush resource exists in Theme.xaml</name>
  <files>Resources/Theme.xaml</files>
  <action>
Check `Resources/Theme.xaml` for the `SchemeSelectedBrush` resource.

The SchemeItemTemplate references it:
```xml
<DataTrigger Binding="{Binding IsSelected}" Value="True">
    <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/ >
</DataTrigger>
```

If `SchemeSelectedBrush` does NOT exist, add it:
```xml
<!-- Scheme Selection Highlight Brush -->
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="#4A90D9" />
```

Use an accent color from your theme (check existing brushes like AccentBrush, PrimaryBrush, etc. and use something similar but slightly different for selection state).

If it already exists, verify it's appropriate for highlighting selected scheme items.
  </action>
  <verify>
    <automated>grep -n "SchemeSelectedBrush" Resources/Theme.xaml</automated>
  </verify>
  <done>SchemeSelectedBrush resource exists in Theme.xaml and is appropriate for selection highlighting</done>
</task>

<task type="auto">
  <name>Task 2: Verify scheme item highlight trigger is working correctly</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Verify the SchemeItemTemplate in `CreatorView.xaml` has the correct DataTrigger for IsSelected.

The template is around line 13-57. It should already have:
```xml
<Style.Triggers>
    <!-- Mouse hover -->
    <Trigger Property="IsMouseOver" Value="True">
        <Setter Property="Background" Value="{StaticResource BackgroundHoverBrush}"/ >
    </Trigger>
    
    <!-- IsSelected = viewing/editing (accent highlight) -->
    <DataTrigger Binding="{Binding IsSelected}" Value="True">
        <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/ >
    </DataTrigger>
</Style.Triggers>
```

Verify:
1. DataTrigger Binding="{Binding IsSelected}" uses correct property name
2. Value="True" (boolean true, not string "True")
3. Setter Property="Background" (not another property)
4. StaticResource SchemeSelectedBrush exists

If all looks correct, the trigger should work. The highlighting issue might have been a side effect of the navigation state problems fixed in Plan 01.

OPTIONAL: Add EnterActions/ExitActions for smooth transitions:
```xml
<DataTrigger Binding="{Binding IsSelected}" Value="True">
    <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/ >
    <DataTrigger.EnterActions>
        <BeginStoryboard>
            <Storyboard>
                <ColorAnimation Storyboard.TargetProperty="Background.Color" 
                               To="#4A90D9" Duration="0:0:0.15"/>
            </Storyboard>
        </BeginStoryboard>
    </DataTrigger.EnterActions>
</DataTrigger>
```

But keep it simple for now - instant change is fine.
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>SchemeItemTemplate DataTrigger for IsSelected is verified correct, build succeeds</done>
</task>

<task type="auto">
  <name>Task 3: Verify arrow vertical alignment is correct</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Verify all 5 arrow containers have VerticalAlignment="Center".

Each expandable feature (DesktopBackground, MouseClick, Shutdown, BootRestart, ScreenWake) has an arrow in its ToggleButton.

Check each arrow Grid (around lines 256, 335, 413, 491, 569):
```xml
<Grid Grid.Column="2" Width="16" Height="16" VerticalAlignment="Center">
    <Path Data="M6 9L12 15L18 9" 
          Stroke="{StaticResource TextSecondaryBrush}"
          StrokeThickness="2"
          Stretch="Uniform"
          VerticalAlignment="Center"
          HorizontalAlignment="Center">
```

Verify:
1. Grid has VerticalAlignment="Center"
2. Path has VerticalAlignment="Center"
3. Path has HorizontalAlignment="Center"

If these are already correct (as they appear to be in the current file), the arrow positioning is already fixed from previous phases. Just verify the markup is correct.
  </action>
  <verify>
    <automated>grep -n "VerticalAlignment=\"Center\"" Views/CreatorView.xaml | grep -E "(Grid|Path).*16.*16" | wc -l</automated>
  </verify>
  <done>All 5 arrow containers verified to have VerticalAlignment="Center" (should see 10 matches - Grid and Path for each)</done>
</task>

<task type="auto">
  <name>Task 4: Verify ScrollViewer padding reserves scrollbar space</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Verify the ScrollViewer has correct padding to reserve space for scrollbar.

Around line 206:
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,12,0">
```

Verify:
1. Padding="0,0,12,0" (left=0, top=0, right=12, bottom=0)
2. Right padding of 12px reserves space for scrollbar
3. This prevents buttons from being compressed when scrollbar appears

If this is already correct, the scrollbar issue is already fixed from previous phases.

OPTIONAL: To make it even better, you could set:
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,16,0"
               ScrollViewer.CanContentScroll="True">
```

But 12px should be sufficient.
  </action>
  <verify>
    <automated>grep -A2 "ScrollViewer Grid.Row=\"1\"" Views/CreatorView.xaml | grep -q "Padding=\"0,0,12,0\"" && echo "OK" || echo "MISSING"</automated>
  </verify>
  <done>ScrollViewer has Padding="0,0,12,0" to reserve scrollbar space</done>
</task>

<task type="auto">
  <name>Task 5: Add MinWidth to navigation buttons to prevent compression</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Add MinWidth to navigation buttons to ensure they don't get compressed.

For simple buttons (ThemePreview, OpenApp, DesktopClock, Pomodoro, Anniversary) that use BooleanToFeatureButtonConverter, check if the style or the button itself has MinWidth.

The expandable ToggleButtons already have fixed Height="40" but might benefit from MinWidth.

Add MinWidth="200" (or appropriate value) to:
1. All simple buttons (around lines 212, 614, 625, 636, 647)
2. The StackPanel margin already accounts for scrollbar, but buttons need minimum size

Example for simple buttons:
```xml
<Button Command="{Binding SelectFeatureCommand}"
        CommandParameter="ThemePreview"
        Style="{Binding IsThemePreviewActive, Converter={StaticResource BooleanToFeatureButtonConverter}}"
        MinWidth="200">
```

For the ToggleButtons, they have explicit Height="40" in the style, which should be sufficient. But verify the Grid container doesn't compress them.
  </action>
  <verify>
    <automated>dotnet build --nologo -v q</automated>
  </verify>
  <done>Navigation buttons have MinWidth to prevent compression, build succeeds</done>
</task>

</tasks>

<verification>
1. Build succeeds with 0 errors
2. Run application
3. Navigate to Creator view
4. Click "Desktop Background" to expand
5. Click "+ 新建方案" to create a scheme
6. **Verify scheme item shows highlight background** (Accent color)
7. Click another feature
8. **Verify only one button is highlighted at a time**
9. Scroll through navigation if many schemes
10. **Verify scrollbar doesn't compress buttons** (buttons maintain width)
11. Expand/collapse features
12. **Verify arrow stays centered vertically**
</verification>

<success_criteria>
- [ ] Scheme items show accent background when selected
- [ ] Only one scheme highlighted at a time
- [ ] Navigation buttons maintain minimum width (not compressed by scrollbar)
- [ ] Arrow icons stay vertically centered when expanding/collapsing
- [ ] Build succeeds with 0 errors
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v3/01-fix-v3-02-SUMMARY.md`
</output>
