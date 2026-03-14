---
phase: 01-fix
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-02
must_haves:
  truths:
    - Arrow stays vertically centered when ToggleButton is expanded
    - Arrow rotates 180 degrees but does not shift down
  artifacts:
    - path: "Views/CreatorView.xaml"
      provides: "Fixed ToggleButton arrow positioning"
      section: "FeatureExpanderButtonStyle and Path elements"
  key_links:
    - from: "ToggleButton.IsChecked trigger"
      to: "Path.VerticalAlignment"
      pattern: "RenderTransform does not affect layout position"
---

<objective>
Fix the arrow vertical alignment in expandable navigation buttons.

**Purpose:** Visual polish - the expand/collapse arrow currently shifts downward when the ToggleButton is checked (expanded), appearing bottom-aligned instead of centered.

**Output:** Arrow remains vertically centered in both collapsed and expanded states.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/phases/01-fix/01-CONTEXT.md
@Views/CreatorView.xaml

## Current Issue Analysis

In CreatorView.xaml, the expand/collapse arrow is implemented as a Path inside a Grid within each ToggleButton:

```xml
<Path Grid.Column="2"
      Data="M6 9L12 15L18 9"
      Stroke="{StaticResource TextSecondaryBrush}"
      StrokeThickness="2"
      Width="16"
      Height="16"
      Stretch="Uniform"
      VerticalAlignment="Center">
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
```

**Root Cause:** The issue is likely that the `VerticalAlignment="Center"` is on the Path, but when the ToggleButton's `IsChecked` trigger fires, it changes the Background to `PrimaryGradientBrush`. The Grid layout might be recalculating the Path position.

**Button Specs:**
- Height: 40px
- Padding: 16,0 (horizontal only, no vertical padding)
- Arrow size: 16x16

**Expected Fix:** Ensure Path maintains vertical center alignment regardless of ToggleButton state. The issue might be the Grid column definition or the content alignment within the ToggleButton template.
</context>

<tasks>

<task type="auto">
  <name>Fix Arrow Vertical Alignment in ToggleButton</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Fix the arrow vertical alignment issue in all 5 expandable feature ToggleButtons (Desktop Background, Mouse Click, Shutdown, Boot Restart, Screen Wake).

**Problem Analysis:**
The arrow currently shifts down when ToggleButton IsChecked=true (expanded). The VerticalAlignment="Center" is set on the Path, but the issue is likely that:
1. The Grid containing the text and arrow doesn't constrain the arrow properly
2. The ToggleButton's ControlTemplate or ContentPresenter alignment affects child positioning

**Fix Approach:**

1. **Update the Path in each ToggleButton** (lines 263-284, 334-355, 404-425, 474-495, 544-565):
   - Add `HorizontalAlignment="Center"` to the Path
   - Ensure the Path is properly sized within its Grid cell
   - The RotateTransform should only rotate, not affect position

2. **Alternative Fix - Wrap Path in a fixed-size container:**
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

3. **Verify the Grid.ColumnDefinitions** for each ToggleButton:
   - Column 0: Text and count (star width)
   - Column 1: Spacer (if needed)
   - Column 2: Arrow (auto width, fixed size container)

Apply this fix to all 5 expandable features:
- Desktop Background (line 263)
- Mouse Click (line 334)
- Shutdown (line 404)
- Boot/Restart (line 474)
- Screen Wake (line 544)

**Important:** Do NOT change the arrow rotation logic - only fix the vertical positioning.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /C:"error" && exit 1 || exit 0</automated>
  </verify>
  <done>All 5 ToggleButton arrows have fixed vertical positioning that stays centered when expanded</done>
</task>

</tasks>

<verification>
Build verification: Project compiles without errors.
</verification>

<success_criteria>
- All 5 expandable feature buttons have arrows that stay vertically centered
- Arrow rotates 180 degrees when expanded (existing behavior preserved)
- No build errors introduced
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix/01-fix-01-SUMMARY.md`
</output>
