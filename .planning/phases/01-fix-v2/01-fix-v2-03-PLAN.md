---
phase: 01-fix-v2
plan: 03
type: execute
wave: 2
depends_on: ["01-fix-v2-01", "01-fix-v2-02"]
files_modified: 
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-V2-004
must_haves:
  truths:
    - "Scheme items show only highlight, no checkmark icon"
    - "Expand arrow stays vertically centered when expanded"
    - "ScrollViewer padding prevents button compression"
    - "UI is clean and consistent with design intent"
  artifacts:
    - path: "Views/CreatorView.xaml"
      provides: "Polished UI without checkmarks, centered arrows, proper scrollbar spacing"
      changes: "Remove checkmark Path, verify arrow centering, confirm scrollbar padding"
  key_links:
    - from: "SchemeItemTemplate"
      to: "SchemeModel.IsSelected"
      via: "Background highlight brush (no checkmark visibility)"
    - from: "ToggleButton expand arrow Path"
      to: "VerticalAlignment"
      via: "Center alignment in both expanded/collapsed states"
---

<objective>
Polish UI elements: remove checkmark from scheme items, ensure arrow stays centered, verify scrollbar doesn't compress buttons.

Purpose: Clean up visual inconsistencies identified in 01-CONTEXT.md
Output: Updated CreatorView.xaml with removed checkmark and verified arrow positioning
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/PROJECT.md
@.planning/phases/01-fix-v2/01-CONTEXT.md

## Current Implementation Analysis

**1. Checkmark Removal (Line 63-73 in CreatorView.xaml):**
```xml
<!-- Active Indicator Checkmark - shows when IsActive=true -->
<Path Grid.Column="1"
       Data="M4.5 8.5L7 11L11.5 6.5"
       Stroke="{StaticResource AccentBrush}"
       StrokeThickness="2"
       Width="16"
       Height="16"
       Stretch="Uniform"
       VerticalAlignment="Center"
       Margin="8,0,0,0"
       Visibility="{Binding IsActive, Converter={StaticResource BooleanToVisibilityConverter}}"/>
```
This Path must be removed per user decision: "Remove checkmark completely from scheme items"

**2. Arrow Vertical Centering (Lines 275-296):**
```xml
<Grid Grid.Column="2" Width="16" Height="16">
    <Path Data="M6 9L12 15L18 9"
          Stroke="{StaticResource TextSecondaryBrush}"
          StrokeThickness="2"
          Stretch="Uniform"
          VerticalAlignment="Center"
          HorizontalAlignment="Center">
```
The Path already has VerticalAlignment="Center", but the issue might be:
- The Grid container might not be centering properly
- The IsChecked trigger might affect layout

**3. Scrollbar Padding (Line 227):**
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,12,0">
```
This is already implemented in a previous fix - need to verify it's still correct.

## User Decisions from 01-CONTEXT.md

**Checkmark Removal:**
- Decision: Remove checkmark completely from scheme items
- Only use highlight color to indicate selection
- Active/Enabled state will be handled separately in future phase

**Arrow Position:**
- Decision: Arrow must stay vertically centered
- Current issue: Arrow shifts down when expanded
- Fix: Ensure Path.VerticalAlignment="Center" in both states

**Scrollbar Layout:**
- Decision: Reserve space for scrollbar to prevent button compression  
- Set ScrollViewer.Padding="0,0,12,0" (reserve 12px on right)
- Already done - verify still correct
</context>

<tasks>

<task type="auto" tdd="false">
  <name>Task 1: Remove Checkmark from Scheme Items</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Remove the checkmark Path from SchemeItemTemplate (lines 63-73).

**Current SchemeItemTemplate structure (lines 13-76):**
```xml
<DataTemplate x:Key="SchemeItemTemplate" DataType="{x:Type models:SchemeModel}">
    <Button ...>
        ...
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Grid.Column="0" Text="{Binding Name}" ... />
            
            <!-- REMOVE THIS ENTIRE PATH (lines 63-73) -->
            <Path Grid.Column="1"
                   Data="M4.5 8.5L7 11L11.5 6.5"
                   Stroke="{StaticResource AccentBrush}"
                   StrokeThickness="2"
                   Width="16"
                   Height="16"
                   Stretch="Uniform"
                   VerticalAlignment="Center"
                   Margin="8,0,0,0"
                   Visibility="{Binding IsActive, Converter={StaticResource BooleanToVisibilityConverter}}"/>
        </Grid>
    </Button>
</DataTemplate>
```

**Changes to make:**
1. Remove the entire Path element (lines 63-73)
2. Since we no longer need Column 1, we can simplify the Grid:
   - Remove Grid.ColumnDefinitions entirely
   - Remove Grid.Column="0" from TextBlock
   - Keep just the TextBlock inside the Grid

**Simplified structure:**
```xml
<DataTemplate x:Key="SchemeItemTemplate" DataType="{x:Type models:SchemeModel}">
    <Button ...>
        ...
        <TextBlock Text="{Binding Name}"
                   Foreground="{StaticResource TextPrimaryBrush}"
                   FontSize="13"
                   VerticalAlignment="Center"
                   TextTrimming="CharacterEllipsis"/>
    </Button>
</DataTemplate>
```

**Why:** The user explicitly decided to remove checkmarks. Selection is indicated by the IsSelected background highlight (SchemeSelectedBrush) applied via the Button's Style trigger on line 43-45.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - Checkmark Path removed from SchemeItemTemplate
    - Grid simplified (ColumnDefinitions removed if no longer needed)
    - TextBlock remains for scheme name display
    - IsSelected background highlight still applies correctly
  </done>
</task>

<task type="auto" tdd="false">
  <name>Task 2: Fix Arrow Vertical Centering</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Ensure the expand/collapse arrow stays vertically centered in all expandable buttons.

**Current implementation analysis (lines 275-296 for Desktop Background):**
```xml
<Grid Grid.Column="2" Width="16" Height="16">
    <Path Data="M6 9L12 15L18 9"
          Stroke="{StaticResource TextSecondaryBrush}"
          StrokeThickness="2"
          Stretch="Uniform"
          VerticalAlignment="Center"
          HorizontalAlignment="Center">
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

**Issue:** The Grid has fixed Width="16" Height="16", but the Path.VerticalAlignment="Center" should work. However, the problem might be:

1. The ToggleButton has Height="40" and VerticalContentAlignment="Center" (line 113)
2. But the Grid containing the arrow might not be centering in the available space

**Fix:** Add VerticalAlignment="Center" to the Grid container:

```xml
<Grid Grid.Column="2" Width="16" Height="16" VerticalAlignment="Center">
    <Path ... />
</Grid>
```

**Apply this fix to all 5 expandable features:**
1. Desktop Background (around line 275)
2. Mouse Click (around line 354)
3. Shutdown (around line 432)
4. Boot/Restart (around line 510)
5. Screen Wake (around line 588)

**Verify the parent Grid structure:**
The ToggleButton content Grid (line 254-297 for Desktop Background) should ensure proper vertical alignment:
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>  <!-- Spacing -->
        <ColumnDefinition Width="Auto"/>  <!-- Arrow -->
    </Grid.ColumnDefinitions>
    <!-- Text content in Column 0 -->
    <!-- Arrow Grid in Column 2 with VerticalAlignment="Center" -->
</Grid>
```

**Also verify:** The parent ToggleButton has:
- Height="40" ✓ (line 112)
- VerticalContentAlignment="Center" ✓ (line 113)
- Padding="16,0" ✓ (line 111)

The issue might be the missing VerticalAlignment on the arrow Grid container.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - All 5 arrow Grid containers have VerticalAlignment="Center"
    - Arrow Path keeps VerticalAlignment="Center" 
    - Parent ToggleButton maintains Height="40" and VerticalContentAlignment="Center"
    - Build succeeds with 0 errors
  </done>
</task>

<task type="auto" tdd="false">
  <name>Task 3: Verify Scrollbar Padding</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Verify the ScrollViewer scrollbar padding is correctly configured.

**Current implementation (line 225-227):**
```xml
<ScrollViewer Grid.Row="1" 
               VerticalScrollBarVisibility="Auto"
               Padding="0,0,12,0">
```

**What to verify:**
1. Padding="0,0,12,0" exists (12px reserved on right for scrollbar)
2. VerticalScrollBarVisibility="Auto" is set
3. StackPanel inside has appropriate margins

**StackPanel check (line 228):**
```xml
<StackPanel Margin="16,8,4,16">
```
This is correct - the right margin of 4px plus the ScrollViewer padding of 12px = 16px total right spacing, matching the left margin.

**If any issues found:**
- Ensure Padding="0,0,12,0" is present
- Ensure StackPanel doesn't have excessive right margin that would cause double-spacing

**This task is primarily verification** - the fix was applied in a previous phase. Just confirm the values are still correct.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - ScrollViewer has Padding="0,0,12,0"
    - VerticalScrollBarVisibility="Auto"
    - StackPanel Margin is appropriate
    - No button compression occurs when scrollbar appears
  </done>
</task>

</tasks>

<verification>
Build verification:
```bash
dotnet build
```

Visual verification (after execution):
1. Run application
2. Navigate to Creator view
3. Verify scheme items show only highlight (no checkmark) when selected
4. Expand Desktop Background → verify arrow is vertically centered
5. Collapse and expand again → verify arrow stays centered
6. Add many schemes to trigger scrollbar → verify buttons don't compress
7. Repeat for all 5 expandable features
</verification>

<success_criteria>
- [ ] Checkmark Path removed from SchemeItemTemplate
- [ ] Arrow Grid containers have VerticalAlignment="Center"
- [ ] ScrollViewer has 12px right padding reserved
- [ ] Build succeeds with 0 errors
- [ ] Scheme items display correctly without checkmark
- [ ] Arrows appear vertically centered in both states
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v2/01-fix-v2-03-SUMMARY.md`
</output>
