# Phase 01-fix-v2: Creator View Critical Fixes - Context

**Gathered:** 2026-03-14  
**Status:** Ready for planning  
**Attempts:** 4th fix round (preceded by Phase 2-fix, Phase 3-fix-v2, and Phase 01-fix planning)

---

<domain>
## Phase Boundary

Fix critical Creator View issues that have persisted through three previous attempts. This is an emergency fix phase focused on making the basic functionality work:
- Page display for ALL features (not just Theme Preview)
- Navigation highlighting consistency
- Self-testing mechanism to prevent regression

**Scope:** 
- Fix page display mechanism (all 9 features must display)
- Fix expandable button highlight persistence
- Implement testing/monitoring for navigation
- Fix scrollbar not compressing navigation buttons
- Fix arrow position in expandable buttons
- Remove checkmark, use highlight only

**In-Scope:**
- Debugging and fixing ContentControl/ViewModel binding issues
- Navigation state management fixes
- Self-testing infrastructure
- UI polish (arrow, scrollbar)

**Out-of-Scope:**
- New features
- Major refactoring
- Performance optimization
- Animation enhancements

</domain>

---

<decisions>
## Implementation Decisions

### 1. Page Display Issue (CRITICAL - P0)

**Current State:** Only Theme Preview displays. All other pages (Desktop Background, Mouse Click, Clock, Pomodoro, Anniversary, Shutdown, Boot Restart, Screen Wake) show nothing.

**Evidence from Codebase:**
- DataTemplates are registered in App.xaml (lines 59-89) ✓
- LoadFeatureContent() in CreatorViewModel.cs creates ViewModels and assigns to ConfigurationContent ✓
- CreatorView.xaml has ContentControl bound to ConfigurationContent (line 796) ✓
- DI factories are set up with fallback to parameterless constructors ✓

**Root Cause Hypotheses:**
1. ViewModel instantiation is throwing exceptions (silent failure)
2. DataTemplate lookup is failing (namespace/type mismatch)
3. Binding is failing but not throwing (ContentControl stays empty)
4. Views have XAML errors preventing instantiation

**Decision:** 
- **Debug approach:** Add try-catch with logging around ViewModel creation
- **Verification:** Add runtime check that ConfigurationContent type matches expected ViewModel type
- **Fallback:** If DataTemplate fails, create View manually and set as Content

### 2. Self-Testing/Monitoring Mechanism (NEW REQUIREMENT)

**Requirement:** Ensure CreateView navigation buttons correctly load associated pages

**Decision:**
Implement **runtime monitoring** (not unit tests) because:
- WPF UI testing is complex and heavy
- Runtime monitoring catches issues during actual usage
- Can be implemented quickly without external frameworks

**Implementation:**
```csharp
// NavigationMonitorService - singleton
- Track: Which button clicked → What ViewModel created → Success/Failure
- Log: All navigation attempts with timestamps
- Verify: ContentControl.Content type matches expected View
- Report: Summary of navigation success rates
```

**Testing Checklist (Manual but Systematic):**
1. Click each of 9 navigation buttons
2. Verify ContentControl displays content (not empty)
3. Verify content type matches expected ViewModel
4. Verify no exceptions in output window

### 3. Navigation Highlighting

**Decision: Unified Single Highlight**

**Rules:**
- Only ONE button highlighted at any time (expandable OR simple)
- Simple button clicked: highlight it, collapse all expandable
- Expandable header clicked: highlight it, expand menu, show default scheme
- Scheme item clicked: highlight scheme item AND keep parent header highlighted
- **NEW:** When expandable collapses, remove highlight from header

**State Management:**
- Track `SelectedFeature` (string) - which feature is selected
- Track `ExpandedFeature` (FeatureType?) - which expandable is open
- Track `SelectedScheme` (SchemeModel) - which scheme is selected
- Highlight logic: 
  - Simple button: `SelectedFeature == buttonName`
  - Expandable header: `SelectedFeature == featureName || (ExpandedFeature == featureType && SelectedScheme?.FeatureType == featureType)`
  - Scheme item: `SelectedScheme == scheme`

### 4. Checkmark Removal

**Decision:** Remove checkmark completely from scheme items
- Only use highlight color to indicate selection
- Simplifies UI and reduces confusion
- Active/Enabled state will be handled separately in future phase

**Visual:**
- Selected scheme: AccentBrush background highlight
- Unselected scheme: Transparent background

### 5. Scrollbar Layout

**Decision:** Reserve space for scrollbar to prevent button compression
- Set ScrollViewer.Padding="0,0,12,0" (reserve 12px on right)
- Scrollbar visibility: Auto (only show when needed)
- Button width: Fixed or MinWidth to prevent compression

### 6. Arrow Position

**Decision:** Arrow must stay vertically centered
- Current issue: Arrow shifts down when expanded
- Fix: Ensure Path.VerticalAlignment="Center" in both states
- Check if IsChecked trigger affects layout

</decisions>

---

<code_context>
## Existing Code Analysis

### DataTemplate Setup (Working)
```xml
<!-- App.xaml lines 59-89 -->
<DataTemplate DataType="{x:Type vm:DesktopBackgroundViewModel}">
    <views:DesktopBackgroundView/>
</DataTemplate>
<!-- ... 8 more DataTemplates ... -->
```

### Content Loading (Needs Debug)
```csharp
// CreatorViewModel.cs LoadFeatureContent()
case "DesktopBackground":
    var desktopBgVm = _desktopBackgroundVmFactory();
    ConfigurationContent = null; // Force refresh
    ConfigurationContent = desktopBgVm;
    break;
```

### Current Issues Identified:
1. **Line 684:** `ConfigurationContent = null;` happens BEFORE try-catch
2. **No error handling:** If factory throws, exception is silent
3. **No verification:** Content set but never verified it displays

### Critical Fix Needed:
Add error handling and logging:
```csharp
try {
    var viewModel = _desktopBackgroundVmFactory();
    ConfigurationContent = viewModel;
    NavigationMonitor?.LogSuccess("DesktopBackground", viewModel.GetType());
} catch (Exception ex) {
    NavigationMonitor?.LogFailure("DesktopBackground", ex);
    ConfigurationContent = null;
}
```

### Highlight Logic Issue:
CreatorViewModel.cs uses `SelectedFeature` property with computed bools, but expandable buttons don't clear highlight when collapsed.

**Fix needed:**
When `IsXXXExpanded` changes to false, check if that feature is still selected. If not, ensure highlight is removed.

</code_context>

---

<specifics>
## Specific Requirements

### P0: Page Display Fix
**Must debug:**
1. Add try-catch around ALL ViewModel factory calls in LoadFeatureContent
2. Add Debug.WriteLine or logging to trace execution
3. Verify DataTemplate lookup works (check type names match exactly)
4. Check if Views have XAML errors preventing load
5. Test: Click Desktop Background → should show DesktopBackgroundView

**Verification:**
```csharp
// After setting ConfigurationContent
if (ConfigurationContent is DesktopBackgroundViewModel) {
    Debug.WriteLine("✓ DesktopBackground ViewModel created");
} else {
    Debug.WriteLine("✗ Wrong type: " + ConfigurationContent?.GetType()?.Name);
}
```

### P1: Expandable Button Highlight Fix
**Behavior:**
- When ToggleButton.IsChecked = false (collapsed):
  - If this feature is NOT selected → remove highlight
  - If this feature IS selected → keep highlight
- When ToggleButton.IsChecked = true (expanded):
  - Always highlight
  - Collapse all other expandables

### P2: Navigation Monitor
**Minimal Implementation:**
```csharp
public static class NavigationMonitor {
    public static void LogNavigation(string feature, object viewModel, Exception error = null);
    public static List<NavigationLog> GetLogs();
    public static bool AllNavigationsSuccessful();
}
```

**Usage in CreatorViewModel:**
```csharp
[RelayCommand]
private void SelectFeature(string featureName) {
    try {
        SelectedFeature = featureName;
        LoadFeatureContent(featureName);
        NavigationMonitor.LogNavigation(featureName, ConfigurationContent);
    } catch (Exception ex) {
        NavigationMonitor.LogNavigation(featureName, null, ex);
    }
}
```

### P3: UI Polish
- **Scrollbar:** Reserve 12px right padding
- **Arrow:** Center vertically in expanded state
- **Checkmark:** Remove from SchemeItemTemplate

</specifics>

---

<deferred>
## Deferred Ideas

- **Full Checkmark/Active State:** Complete implementation with toggle UI
- **Automated UI Testing:** Framework like FlaUI or Microsoft UI Automation
- **Performance:** Virtualization, lazy loading
- **Animations:** Smooth transitions for expand/collapse
- **Error Recovery:** Automatic retry for failed page loads

</deferred>

---

<implementation_notes>
## Implementation Notes for Planner

### Priority (Must be in this order):
1. **P0:** Page display debugging and fixing - CRITICAL
2. **P1:** Expandable button highlight - HIGH  
3. **P2:** Navigation monitoring - HIGH (enables verification)
4. **P3:** Scrollbar/Arrow/Checkmark - MEDIUM

### Risk Mitigation:
- **Regression:** Previous fixes may have broken working parts - verify Theme Preview still works
- **Silent Failures:** Add extensive logging/monitoring
- **Type Mismatches:** Verify all DataTemplate DataType strings match ViewModel namespaces exactly

### Testing Strategy:
1. Run app with Debug output window open
2. Click each navigation button
3. Check logs for success/failure
4. Verify ContentControl displays something (not empty)
5. Check that only one button highlighted at a time
6. Check expandable collapse removes highlight

### Success Criteria:
- [ ] All 9 features display their pages
- [ ] Navigation monitoring shows 100% success rate
- [ ] Only one button highlighted at a time
- [ ] Expandable buttons clear highlight when collapsed (if not selected)
- [ ] Scrollbar doesn't compress buttons
- [ ] Arrow centered in expanded state
- [ ] No checkmarks on scheme items

</implementation_notes>

---

*Phase: 01-fix-v2*  
*Context gathered: 2026-03-14*
