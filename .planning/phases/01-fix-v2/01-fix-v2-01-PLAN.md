---
phase: 01-fix-v2
plan: 01
type: execute
wave: 1
depends_on: []
files_modified: 
  - Services/NavigationMonitorService.cs
  - ViewModels/CreatorViewModel.cs
autonomous: true
requirements:
  - FIX-V2-001
  - FIX-V2-002
must_haves:
  truths:
    - "All 9 navigation buttons can be clicked without silent failures"
    - "Navigation attempts are logged with success/failure status"
    - "ViewModel instantiation exceptions are caught and logged"
    - "ContentControl displays content for all features (not just Theme Preview)"
  artifacts:
    - path: "Services/NavigationMonitorService.cs"
      provides: "Runtime navigation monitoring and logging"
      exports: ["LogNavigation", "GetNavigationReport", "AllNavigationsSuccessful"]
    - path: "ViewModels/CreatorViewModel.cs"
      provides: "Error handling around all ViewModel factory calls"
      min_changes: 8 factory calls wrapped in try-catch
  key_links:
    - from: "CreatorViewModel.SelectFeature"
      to: "NavigationMonitorService.LogNavigation"
      via: "service call with feature name and ViewModel type"
    - from: "CreatorViewModel.LoadFeatureContent"
      to: "NavigationMonitorService"
      via: "try-catch blocks around all _xxxVmFactory() calls"
---

<objective>
Fix the critical page display issue where only Theme Preview works. Add comprehensive error handling and runtime navigation monitoring to catch silent failures.

Purpose: Enable all 9 feature pages to display correctly and provide debugging visibility
Output: NavigationMonitorService, wrapped factory calls with try-catch, detailed logging
</objective>

<execution_context>
@C:/Users/MA Huan/.config/opencode/get-shit-done/workflows/execute-plan.md
@C:/Users/MA Huan/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/PROJECT.md
@.planning/ROADMAP.md
@.planning/phases/01-fix-v2/01-CONTEXT.md

## Critical Code Analysis

From CreatorViewModel.cs LoadFeatureContent() (lines 680-789):
- Line 683-684: `ConfigurationContent = null;` happens BEFORE any error handling
- Lines 697, 708, 720, 729, 738, 746, 757, 768: Factory calls have no try-catch
- Silent exceptions cause ContentControl to stay empty

From App.xaml (lines 59-89):
- DataTemplates are correctly registered for all ViewModels
- Type mapping: vm:DesktopBackgroundViewModel -> views:DesktopBackgroundView

From App.xaml.cs (lines 40-61):
- DI registrations exist for all ViewModels as Transient
- Factories injected into CreatorViewModel constructor
</context>

<tasks>

<task type="auto" tdd="false">
  <name>Task 1: Create NavigationMonitorService</name>
  <files>Services/NavigationMonitorService.cs</files>
  <action>
Create a singleton service for runtime navigation monitoring:

1. Create NavigationLogEntry class with:
   - Timestamp (DateTime)
   - FeatureName (string)
   - ViewModelType (string)
   - Success (bool)
   - ErrorMessage (string?)
   - StackTrace (string?)

2. Create NavigationMonitorService class:
   - Private static List&lt;NavigationLogEntry&gt; _logs
   - Static method LogNavigation(string feature, object? viewModel, Exception? error = null)
   - Static method GetLogs() returning IEnumerable&lt;NavigationLogEntry&gt;
   - Static method GetNavigationReport() returning formatted string summary
   - Static method AllNavigationsSuccessful() returning bool
   - Static method ClearLogs()
   - Static event OnNavigationFailed (for UI notification)

3. Add thread-safety with lock(_logs) for all operations

4. Add Debug.WriteLine output for immediate debugging visibility

**Why this design:** Static service = accessible from anywhere without DI changes. Thread-safe for UI callbacks. Event allows future UI notification of failures.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - NavigationMonitorService.cs compiles without errors
    - All 5 static methods are accessible
    - Thread-safety implemented with lock statements
  </done>
</task>

<task type="auto" tdd="false">
  <name>Task 2: Wrap ViewModel Factory Calls with Error Handling</name>
  <files>ViewModels/CreatorViewModel.cs</files>
  <action>
Modify LoadFeatureContent() method to add try-catch around ALL factory calls:

**Current problematic pattern (line 697):**
```csharp
var desktopBgVm = _desktopBackgroundVmFactory();
ConfigurationContent = desktopBgVm;
```

**New pattern for ALL 8 features:**
```csharp
case "DesktopBackground":
    try
    {
        var desktopBgVm = _desktopBackgroundVmFactory();
        if (SelectedDesktopBackgroundScheme != null)
        {
            desktopBgVm.SchemeName = SelectedDesktopBackgroundScheme.Name;
        }
        ConfigurationContent = desktopBgVm;
        NavigationMonitorService.LogNavigation("DesktopBackground", desktopBgVm);
        HasPreviewContent = false;
    }
    catch (Exception ex)
    {
        NavigationMonitorService.LogNavigation("DesktopBackground", null, ex);
        ConfigurationContent = null;
        HasPreviewContent = false;
        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to create DesktopBackgroundViewModel: {ex.Message}");
    }
    break;
```

**Features to wrap:**
1. DesktopBackground (line 695-704)
2. MouseClick (line 706-715)
3. DesktopClock (line 717-724) - already has some Debug.WriteLine, add try-catch
4. Pomodoro (line 726-733) - already has some Debug.WriteLine, add try-catch
5. Anniversary (line 735-742) - already has some Debug.WriteLine, add try-catch
6. Shutdown (line 744-753)
7. BootRestart (line 755-764)
8. ScreenWake (line 766-775)

**Also update SelectFeature command (line 421-441):**
Add try-catch around LoadFeatureContent call and log to NavigationMonitorService.

**Critical:** Remove the early `ConfigurationContent = null;` at lines 683-684 that happens before the switch statement. Set it to null only inside catch blocks or at the start of each case.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - All 8 factory calls wrapped in try-catch blocks
    - NavigationMonitorService.LogNavigation called in all success paths
    - NavigationMonitorService.LogNavigation called with exception in all catch blocks
    - Early ConfigurationContent = null at lines 683-684 removed
    - Debug.WriteLine outputs exception details in all catch blocks
  </done>
</task>

<task type="auto" tdd="false">
  <name>Task 3: Add Navigation Diagnostics to SelectFeature</name>
  <files>ViewModels/CreatorViewModel.cs</files>
  <action>
Enhance SelectFeature method to add comprehensive logging:

**Current method (lines 421-441):**
```csharp
[RelayCommand]
private void SelectFeature(string featureName)
{
    SelectedFeature = featureName;
    // ... collapse logic ...
    LoadFeatureContent(featureName);
}
```

**Enhanced version:**
```csharp
[RelayCommand]
private void SelectFeature(string featureName)
{
    System.Diagnostics.Debug.WriteLine($"[Navigation] Selecting feature: {featureName}");
    
    try
    {
        SelectedFeature = featureName;
        
        // Collapse submenus for simple features and Theme Preview
        switch (featureName)
        {
            case "ThemePreview":
            case "OpenApp":
            case "DesktopClock":
            case "Pomodoro":
            case "Anniversary":
                CollapseAllNavExcept();
                break;
        }
        
        System.Diagnostics.Debug.WriteLine($"[Navigation] Loading content for: {featureName}");
        LoadFeatureContent(featureName);
        
        // Verify content was loaded
        if (ConfigurationContent != null)
        {
            System.Diagnostics.Debug.WriteLine($"[Navigation] SUCCESS: {featureName} loaded, Content type: {ConfigurationContent.GetType().Name}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[Navigation] WARNING: {featureName} loaded but ConfigurationContent is null");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[Navigation] FAILED: {featureName} - {ex.Message}");
        NavigationMonitorService.LogNavigation(featureName, null, ex);
    }
}
```

This provides visibility into the entire navigation flow for debugging.
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | findstr /i "error" || echo "Build successful"</automated>
  </verify>
  <done>
    - SelectFeature has Debug.WriteLine at entry point
    - SelectFeature has try-catch wrapping entire method body
    - Post-load verification checks ConfigurationContent null state
    - All paths log success/failure/warning to Debug output
  </done>
</task>

</tasks>

<verification>
Build verification:
```bash
dotnet build
```

Expected: 0 errors, 0 warnings

Runtime verification (after execution):
1. Run application with Debug output window visible
2. Click each of 9 navigation buttons
3. Check Debug output shows:
   - "[Navigation] Selecting feature: X"
   - "[Navigation] Loading content for: X"
   - "[Navigation] SUCCESS: X loaded, Content type: Y"
4. Check NavigationMonitorService.GetNavigationReport() shows 100% success rate
</verification>

<success_criteria>
- [ ] NavigationMonitorService.cs created with logging capabilities
- [ ] All 8 ViewModel factory calls wrapped in try-catch
- [ ] SelectFeature has comprehensive debug logging
- [ ] Build succeeds with 0 errors
- [ ] Navigation can be monitored via NavigationMonitorService.GetNavigationReport()
- [ ] All exceptions are logged with full stack traces
</success_criteria>

<output>
After completion, create `.planning/phases/01-fix-v2/01-fix-v2-01-SUMMARY.md`
</output>
