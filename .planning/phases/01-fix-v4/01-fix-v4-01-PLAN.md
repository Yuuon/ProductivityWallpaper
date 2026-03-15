---
phase: 01-fix-v4
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Converters/CreatorViewTemplateSelector.cs
  - App.xaml
  - App.xaml.cs
  - Views/CreatorView.xaml
autonomous: true
requirements:
  - FIX-V4-001
  - FIX-V4-002
must_haves:
  truths:
    - "All 9 feature pages display their content correctly in the right panel"
    - "ContentTemplateSelector logs template selection for debugging"
    - "No binding errors appear in Visual Studio Output window"
    - "Template selection is explicit and testable via factory pattern"
  artifacts:
    - path: "Converters/CreatorViewTemplateSelector.cs"
      provides: "Factory-based DataTemplateSelector for explicit ViewModel-to-View mapping"
      exports: ["IViewModelTemplateSelector", "ViewModelTemplateSelectorFactory", "CreatorViewTemplateSelector"]
    - path: "App.xaml"
      provides: "TemplateSelector resource registration"
      contains: "CreatorViewTemplateSelector as StaticResource"
    - path: "App.xaml.cs"
      provides: "DI registration for template selector"
      contains: "AddSingleton<IViewModelTemplateSelector> and AddSingleton<CreatorViewTemplateSelector>"
    - path: "Views/CreatorView.xaml"
      provides: "ContentPresenter with TemplateSelector binding"
      contains: "ContentTemplateSelector={StaticResource CreatorViewTemplateSelector}"
  key_links:
    - from: "Views/CreatorView.xaml ContentPresenter"
      to: "Converters/CreatorViewTemplateSelector"
      via: "ContentTemplateSelector binding"
      pattern: "ContentTemplateSelector=\"{StaticResource CreatorViewTemplateSelector}\""
    - from: "ViewModelTemplateSelectorFactory"
      to: "Application.Current.Resources"
      via: "Resource key lookup by ViewModel type name"
      pattern: "Application.Current.Resources[resourceKey] as DataTemplate"
---

<objective>
Implement factory-pattern ContentTemplateSelector to fix WPF DataTemplate resolution issues for CreatorView feature pages.

**Purpose:** After 5 previous fix attempts, the root cause is identified: ViewModels are created successfully (proven by logs) but WPF's implicit DataTemplate lookup fails silently. This plan implements an explicit factory-pattern TemplateSelector to reliably map ViewModels to their DataTemplates.

**Output:**
- `Converters/CreatorViewTemplateSelector.cs` with IViewModelTemplateSelector interface and factory implementation
- Updated `App.xaml` with TemplateSelector resource registration
- Updated `App.xaml.cs` with DI registration for the selector
- Updated `Views/CreatorView.xaml` using ContentTemplateSelector instead of ContentTemplate="{x:Null}"
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/ROADMAP.md
@.planning/phases/01-fix-v4/01-fix-v4-CONTEXT.md

<!-- Key source files for reference -->
@E:/Projects/ProductivityWallpaper/App.xaml
@E:/Projects/ProductivityWallpaper/App.xaml.cs
@E:/Projects/ProductivityWallpaper/Views/CreatorView.xaml
@E:/Projects/ProductivityWallpaper/Resources/Theme.xaml

<!-- Relevant ViewModels that need template mapping -->
@E:/Projects/ProductivityWallpaper/ViewModels/DesktopBackgroundViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/MouseClickViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/DesktopClockViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/PomodoroViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/AnniversaryViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/ShutdownViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/BootRestartViewModel.cs
@E:/Projects/ProductivityWallpaper/ViewModels/ScreenWakeViewModel.cs

<interfaces>
<!-- Current ContentPresenter structure in CreatorView.xaml (line 782-783) -->
```xml
<ContentPresenter Content="{Binding ConfigurationContent}"
                  ContentTemplate="{x:Null}"/>
```

<!-- DataTemplates defined in App.xaml -->
The App.xaml contains implicit DataTemplates for all ViewModels using DataType:
- DesktopBackgroundViewModel → DesktopBackgroundView
- MouseClickViewModel → MouseClickView
- DesktopClockViewModel → DesktopClockView
- PomodoroViewModel → PomodoroView
- AnniversaryViewModel → AnniversaryView
- ShutdownViewModel → ShutdownView
- BootRestartViewModel → BootRestartView
- ScreenWakeViewModel → ScreenWakeView

<!-- Current DI setup in App.xaml.cs -->
Services are configured with:
- ViewModels registered as Transient (AddTransient<T>)
- Views registered as Transient (AddTransient<T>)
- CreatorViewModel registered as Singleton with factory injection

<!-- Required interface design -->
```csharp
public interface IViewModelTemplateSelector
{
    DataTemplate SelectTemplate(object viewModel, FrameworkElement container);
}
```
</interfaces>
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Create IViewModelTemplateSelector interface and factory implementation</name>
  <files>Converters/CreatorViewTemplateSelector.cs</files>
  <behavior>
    - Test 1: SelectTemplate returns correct DataTemplate for DesktopBackgroundViewModel
    - Test 2: SelectTemplate returns correct DataTemplate for all 8 feature ViewModels
    - Test 3: SelectTemplate caches results to avoid repeated resource lookups
    - Test 4: SelectTemplate returns null gracefully for unknown types
    - Test 5: Resource key is derived as "{TypeName}Template" (e.g., "DesktopBackgroundTemplate")
  </behavior>
  <action>
Create new file `Converters/CreatorViewTemplateSelector.cs` with:

1. **Interface definition:**
```csharp
public interface IViewModelTemplateSelector
{
    DataTemplate SelectTemplate(object viewModel, FrameworkElement container);
}
```

2. **Factory implementation:**
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
            // Build resource key from type name (e.g., "DesktopBackgroundViewModel" → "DesktopBackgroundTemplate")
            var resourceKey = vmType.Name.Replace("ViewModel", "Template");
            template = Application.Current.Resources[resourceKey] as DataTemplate;
            
            if (template == null)
            {
                // Fallback: look for exact type name + Template suffix
                var fullKey = $"{vmType.Name}Template";
                template = Application.Current.Resources[fullKey] as DataTemplate;
                
                System.Diagnostics.Debug.WriteLine($"[TemplateSelector] Warning: Template not found for {vmType.Name}, tried keys: {resourceKey}, {fullKey}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TemplateSelector] Found template for {vmType.Name} using key: {resourceKey}");
            }
            
            _templateCache[vmType] = template;
        }
        
        return template;
    }
}
```

3. **WPF DataTemplateSelector wrapper:**
```csharp
public class CreatorViewTemplateSelector : DataTemplateSelector
{
    private readonly IViewModelTemplateSelector _selector;
    
    public CreatorViewTemplateSelector(IViewModelTemplateSelector selector)
    {
        _selector = selector;
    }
    
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var template = _selector?.SelectTemplate(item, container as FrameworkElement);
        System.Diagnostics.Debug.WriteLine($"[CreatorViewTemplateSelector] SelectTemplate for {item?.GetType()?.Name}: {(template != null ? "FOUND" : "NOT FOUND")}");
        return template;
    }
}
```

**Critical notes:**
- Add extensive Debug.WriteLine logging for troubleshooting
- Use caching to avoid repeated resource dictionary lookups
- Resource key naming must match App.xaml DataTemplate keys (e.g., "DesktopBackgroundTemplate")
- The wrapper class inherits from DataTemplateSelector for WPF integration
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - File Converters/CreatorViewTemplateSelector.cs exists with 3 types defined
    - Interface IViewModelTemplateSelector is exported
    - ViewModelTemplateSelectorFactory implements the interface with caching
    - CreatorViewTemplateSelector inherits from DataTemplateSelector
    - Build succeeds with no errors
  </done>
</task>

<task type="auto">
  <name>Task 2: Register TemplateSelector in DI and XAML resources</name>
  <files>App.xaml.cs, App.xaml</files>
  <action>
1. **Update App.xaml.cs** - Add DI registrations in ConfigureServices method (before CreatorViewModel registration):

```csharp
// Template Selector (before CreatorViewModel)
services.AddSingleton<IViewModelTemplateSelector, ViewModelTemplateSelectorFactory>();
services.AddSingleton<CreatorViewTemplateSelector>();

// Existing CreatorViewModel registration (keep unchanged)
services.AddSingleton<CreatorViewModel>(serviceProvider =>
{
    return new CreatorViewModel(
        () => serviceProvider.GetRequiredService<DesktopBackgroundViewModel>(),
        () => serviceProvider.GetRequiredService<MouseClickViewModel>(),
        () => serviceProvider.GetRequiredService<DesktopClockViewModel>(),
        () => serviceProvider.GetRequiredService<PomodoroViewModel>(),
        () => serviceProvider.GetRequiredService<AnniversaryViewModel>(),
        () => serviceProvider.GetRequiredService<ShutdownViewModel>(),
        () => serviceProvider.GetRequiredService<BootRestartViewModel>(),
        () => serviceProvider.GetRequiredService<ScreenWakeViewModel>());
});
```

2. **Update App.xaml** - Add the TemplateSelector as a resource (after converters, before DataTemplates):

```xml
<!-- Converters (existing) -->
<converters:BooleanToBrushConverter x:Key="BooleanToBrushConverter"/>
... (existing converters)
<converters:StringToImageSourceConverter x:Key="StringToImageSourceConverter"/>

<!-- Template Selector -->
<converters:CreatorViewTemplateSelector x:Key="CreatorViewTemplateSelector"/>

<!-- View-ViewModel Mappings (existing) -->
<DataTemplate DataType="{x:Type vm:WallpaperViewModel}">
    <views:WallpaperView/>
</DataTemplate>
... (existing DataTemplates)
```

**Important:**
- The x:Key must be exactly "CreatorViewTemplateSelector" to match the reference in CreatorView.xaml
- Place it after converters but before DataTemplates
- Ensure xmlns:converters is already defined (it should be)
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - App.xaml.cs has AddSingleton<IViewModelTemplateSelector> and AddSingleton<CreatorViewTemplateSelector>
    - App.xaml has <converters:CreatorViewTemplateSelector x:Key="CreatorViewTemplateSelector"/> resource
    - Build succeeds with no errors
  </done>
</task>

<task type="auto">
  <name>Task 3: Update CreatorView.xaml ContentPresenter to use TemplateSelector</name>
  <files>Views/CreatorView.xaml</files>
  <action>
Update the ContentPresenter in CreatorView.xaml (around line 782-783):

**Current code:**
```xml
<ContentPresenter Content="{Binding ConfigurationContent}"
                  ContentTemplate="{x:Null}"/>
```

**New code:**
```xml
<ContentPresenter Content="{Binding ConfigurationContent}"
                  ContentTemplateSelector="{StaticResource CreatorViewTemplateSelector}"/>
```

This is the critical change that switches from implicit DataTemplate lookup (which fails) to explicit TemplateSelector lookup (which will work).

**Verification points:**
- Line should be in the right panel's Border (Configuration section)
- Remove ContentTemplate="{x:Null}" completely
- Use ContentTemplateSelector with StaticResource binding
- No other changes needed to this ContentPresenter
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - CreatorView.xaml line 783 uses ContentTemplateSelector="{StaticResource CreatorViewTemplateSelector}"
    - ContentTemplate="{x:Null}" has been removed
    - Build succeeds with no errors
    - XAML designer loads without errors
  </done>
</task>

</tasks>

<verification>
After all tasks complete, verify:

1. **Build verification:**
   - `dotnet build` succeeds with no errors
   - No XAML designer errors in Views/CreatorView.xaml

2. **Runtime verification (manual check in Output window):**
   - Launch application
   - Navigate to "创作主题" (Creator) page
   - Click Desktop Background button
   - Check Visual Studio Output window for:
     - `[TemplateSelector] Found template for DesktopBackgroundViewModel using key: DesktopBackgroundTemplate`
     - `[CreatorViewTemplateSelector] SelectTemplate for DesktopBackgroundViewModel: FOUND`
   - Click Desktop Clock, Pomodoro, Anniversary buttons
   - Verify each shows corresponding template selection log

3. **Content display verification:**
   - Desktop Background → should show upload/edit UI (not just theme name)
   - Desktop Clock → should show clock style selection
   - Pomodoro → should show timer settings
   - Anniversary → should show event list

4. **Error check:**
   - No binding errors in Output window
   - No DataTemplate resolution warnings
</verification>

<success_criteria>
- [ ] Converters/CreatorViewTemplateSelector.cs created with all 3 types
- [ ] IViewModelTemplateSelector interface defined
- [ ] ViewModelTemplateSelectorFactory with caching implemented
- [ ] CreatorViewTemplateSelector inherits from DataTemplateSelector
- [ ] DI registration added in App.xaml.cs (before CreatorViewModel)
- [ ] Resource registration added in App.xaml
- [ ] CreatorView.xaml uses ContentTemplateSelector (not ContentTemplate="{x:Null}")
- [ ] Build succeeds with no errors
- [ ] Runtime logs show template selection for all 9 features
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v4/01-fix-v4-01-SUMMARY.md`
</output>
