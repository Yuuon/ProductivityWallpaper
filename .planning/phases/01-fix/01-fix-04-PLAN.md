---
phase: 01-fix
plan: 04
type: execute
wave: 2
depends_on:
  - 01-fix-03
files_modified:
  - ViewModels/CreatorViewModel.cs
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-05
must_haves:
  truths:
    - Scheme items show highlight when IsSelected=true
    - Scheme items show checkmark when IsActive=true
    - Both states can coexist on same scheme (highlighted with checkmark)
    - Visual distinction between "viewing" (highlight) and "enabled for wallpaper" (checkmark)
  artifacts:
    - path: "Views/CreatorView.xaml"
      provides: "Scheme item template with IsSelected highlight and IsActive checkmark"
      section: "SchemeItemTemplate DataTemplate"
    - path: "ViewModels/CreatorViewModel.cs"
      provides: "Proper IsActive/IsSelected state management"
  key_links:
    - from: "SchemeModel.IsSelected"
      to: "Scheme button background"
      via: "DataTrigger in SchemeItemTemplate"
    - from: "SchemeModel.IsActive"
      to: "Checkmark Path visibility"
      via: "BooleanToVisibilityConverter"
---

<objective>
Fix scheme selection highlight vs active checkmark distinction.

**Purpose:** UX clarity - users need to distinguish between "viewing/editing this scheme" (highlight) and "this scheme is enabled for wallpaper" (checkmark). Both states can coexist.

**Output:** Clear visual distinction: accent-colored highlight for selected, checkmark icon for active.
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/phases/01-fix/01-CONTEXT.md
@Views/CreatorView.xaml
@ViewModels/CreatorViewModel.cs

## Current State Analysis

**SchemeItemTemplate in CreatorView.xaml (lines 13-72):**

```xml
<DataTemplate x:Key="SchemeItemTemplate" DataType="{x:Type models:SchemeModel}">
    <Button ...>
        <Button.Style>
            <Style TargetType="Button">
                ...
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsSelected}" Value="True">
                        <Setter Property="Background" Value="{StaticResource BackgroundSelectedBrush}"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding IsActive}" Value="True">
                        <Setter Property="Background" Value="{StaticResource BackgroundSelectedBrush}"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Button.Style>
        <Grid>
            ...
            <!-- Active Indicator Checkmark -->
            <Path Grid.Column="1"
                  Data="M4.5 8.5L7 11L11.5 6.5"
                  Stroke="{StaticResource AccentBrush}"
                  ...
                  Visibility="{Binding IsActive, Converter={StaticResource BooleanToVisibilityConverter}}"/>
        </Grid>
    </Button>
</DataTemplate>
```

**Current Issues:**
1. Both IsSelected and IsActive set the SAME background color (`BackgroundSelectedBrush`)
2. This makes them visually indistinguishable
3. Checkmark shows for IsActive, but the background doesn't differentiate the states

**Requirements from CONTEXT.md:**
- **IsSelected** = highlight (viewing/editing) - use theme accent color, slightly lighter
- **IsActive** = checkmark (enabled for wallpaper) - no background change, just the checkmark
- Both can coexist: a scheme can be both selected (highlighted) AND active (has checkmark)
- Only one scheme per feature can be active
- Multiple schemes can be selected (viewed) across different features, but only one "current" scheme in navigation

**Color Strategy:**
- IsSelected background: `{StaticResource AccentBrush}` at ~20-30% opacity, or a dedicated `SchemeSelectedBrush`
- IsActive: No background change, just the checkmark visible
</context>

<tasks>

<task type="auto">
  <name>Fix Scheme Highlight vs Active Checkmark Distinction</name>
  <files>Views/CreatorView.xaml, ViewModels/CreatorViewModel.cs</files>
  <action>
Fix the visual distinction between scheme selection (IsSelected) and active state (IsActive).

**Step 1: Add New Brush Resource (if needed)**

In Resources/Theme.xaml (or use existing if available), add a brush for scheme selection:

```xml
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="{StaticResource AccentColor}" Opacity="0.2"/>
<!-- Or use an existing lighter accent brush -->
```

If the project doesn't have a `SchemeSelectedBrush`, create one with the accent color at low opacity, OR use `{StaticResource AccentBrush}` with a converter to reduce opacity.

**Step 2: Update SchemeItemTemplate in CreatorView.xaml**

Modify the DataTemplate (lines 13-72):

```xml
<DataTemplate x:Key="SchemeItemTemplate" DataType="{x:Type models:SchemeModel}">
    <Button Command="{Binding DataContext.SelectSchemeCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            CommandParameter="{Binding}"
            Background="Transparent"
            BorderThickness="0"
            Padding="12,8"
            HorizontalContentAlignment="Stretch">
        <Button.Style>
            <Style TargetType="Button">
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="Button">
                            <Border Background="{TemplateBinding Background}"
                                    BorderBrush="{TemplateBinding BorderBrush}"
                                    BorderThickness="{TemplateBinding BorderThickness}"
                                    Padding="{TemplateBinding Padding}"
                                    CornerRadius="6">
                                <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                                  VerticalAlignment="Center"/>
                            </Border>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Triggers>
                    <!-- Mouse hover -->
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="{StaticResource BackgroundHoverBrush}"/>
                    </Trigger>
                    
                    <!-- IsSelected = viewing/editing (accent highlight) -->
                    <DataTrigger Binding="{Binding IsSelected}" Value="True">
                        <Setter Property="Background" Value="{StaticResource SchemeSelectedBrush}"/>
                        <!-- Or use AccentBrush with opacity if SchemeSelectedBrush doesn't exist -->
                    </DataTrigger>
                    
                    <!-- IsActive = enabled for wallpaper (NO background change, just checkmark) -->
                    <!-- Note: Background stays as set by IsSelected or default -->
                </Style.Triggers>
            </Style>
        </Button.Style>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Text="{Binding Name}"
                       Foreground="{StaticResource TextPrimaryBrush}"
                       FontSize="13"
                       VerticalAlignment="Center"
                       TextTrimming="CharacterEllipsis"/>
            
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
        </Grid>
    </Button>
</DataTemplate>
```

**Key Changes:**
1. IsSelected now uses a distinct brush (lighter accent) instead of the same as IsActive
2. IsActive NO LONGER changes background - it only shows the checkmark
3. Added CornerRadius="6" to the Border for better aesthetics
4. Checkmark has Margin="8,0,0,0" for spacing from text

**Step 3: Handle the "Both States" Case**

When a scheme is both IsSelected AND IsActive:
- Background should be the selected highlight color
- Checkmark should be visible

The XAML above handles this correctly - IsSelected sets the background, and the checkmark visibility is independent.

**Step 4: Update ViewModel State Management**

In `SelectScheme` method (CreatorViewModel.cs lines 472-513), ensure:
1. When selecting a scheme: `scheme.IsSelected = true`
2. Activation is a SEPARATE concern - maybe add an `ActivateScheme` command
3. Currently, SelectScheme sets both IsActive and IsSelected - we may need to separate these

Current code sets both:
```csharp
scheme.IsActive = true;   // This is for "enabled for wallpaper"
scheme.IsSelected = true; // This is for "currently viewing"
```

**Decision needed:** Should SelectScheme also activate? According to CONTEXT.md:
- "IsActive (Checkmark): 'Enabled solution' - only one per feature, controls actual wallpaper behavior"
- "IsSelected (Highlight): Currently being viewed/edited in right panel"

**Recommendation:** Keep current behavior where SelectScheme sets both IsSelected=true AND IsActive=true. This means "when you select a scheme to edit, it becomes the active one". If users want a different scheme active, they'd need an explicit "Activate" mechanism later.

**Step 5: Verify Resources Exist**

Ensure these resources exist in Resources/Theme.xaml:
- `SchemeSelectedBrush` - OR create it
- `BackgroundHoverBrush` - should exist
- `TextPrimaryBrush` - should exist
- `AccentBrush` - should exist
- `BooleanToVisibilityConverter` - exists in App.xaml

If `SchemeSelectedBrush` doesn't exist, add it to Theme.xaml:
```xml
<SolidColorBrush x:Key="SchemeSelectedBrush" Color="{StaticResource AccentColor}" Opacity="0.15"/>
```

Or use the existing `BackgroundSelectedBrush` for hover and create a new `AccentSelectedBrush` for scheme selection.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /C:"error" && exit 1 || exit 0</automated>
  </verify>
  <done>Scheme items show distinct visual states: IsSelected shows accent background, IsActive shows checkmark, both can coexist</done>
</task>

</tasks>

<verification>
Build verification: Project compiles without errors.
</verification>

<success_criteria>
- IsSelected=true: Scheme button has accent-colored background (lighter/transparent)
- IsActive=true: Scheme button shows checkmark icon
- Both=true: Scheme has accent background AND checkmark
- Visual distinction is clear and intuitive
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix/01-fix-04-SUMMARY.md`
</output>
