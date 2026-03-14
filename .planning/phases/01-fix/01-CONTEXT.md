# Phase 01-fix: Creator View Final Fixes - Context

**Gathered:** 2026-03-14  
**Status:** Ready for planning  
**Attempts:** 3rd fix round (preceded by Phase 2-fix and Phase 3-fix-v2)

---

<domain>
## Phase Boundary

Fix persistent Creator View UI/UX issues that have remained unresolved through two previous fix attempts. Focus on navigation interaction consistency, scheme state visualization clarity, and feature page display reliability.

**Scope:** Navigation behavior fixes, scheme selection visualization, arrow positioning, ViewModel/content loading fixes for Clock/Pomodoro/Anniversary pages. No new capabilities.

**In-Scope:**
- Navigation single-highlight logic (expandable + simple buttons unified)
- Single-expand submenu behavior
- Scheme selection highlight vs active checkmark distinction
- Arrow vertical alignment in expandable buttons
- Clock/Pomodoro/Anniversary page display restoration

**Out-of-Scope:**
- New features or capabilities
- Performance optimization
- Theme system changes
- Backend functionality beyond UI binding

</domain>

---

<decisions>
## Implementation Decisions

### 1. Verification Approach
- Automated UI testing if technically feasible with WPF test frameworks
- Manual verification required regardless of automation
- Visual checklist to confirm fixes before marking complete

### 2. Scheme State Visualization

**Two distinct states:**
- **IsActive (Checkmark)**: "Enabled solution" for the feature. Only one scheme per feature can be active at a time. This controls the actual wallpaper behavior.
- **IsSelected (Highlight)**: Currently being viewed/edited in the right panel. UI state only.

**Visual Design:**
- Selected highlight color: Theme color (AccentBrush), slightly lighter variant
- Checkmark: Can coexist with highlight (scheme can be both active AND selected)
- If checkmark implementation causes confusion during development, it can be removed temporarily
- Checkmark appears as additional indicator on the scheme button, not replacing highlight

**Behavior:**
- Clicking scheme sets IsSelected = true
- Activating scheme (via separate activation mechanism) sets IsActive = true
- UI must show both states independently

### 3. Navigation Interaction Model

**Unified highlighting:** All navigation buttons (expandable headers + simple buttons) share single-highlight logic. Only one button highlighted at any time.

**Four interaction cases:**

**a. Click Non-Expandable Button (Theme Preview, Open App, Clock, Pomodoro, Anniversary):**
- Collapse all expanded submenus
- Highlight the clicked button
- Show that feature's page in right panel

**b. Click Expandable Header (Desktop Background, Mouse Click, Shutdown, Boot Restart, Screen Wake):**
- Expand the submenu
- Highlight the header button
- If no schemes exist: auto-create default scheme
- Show the first/default scheme's page in right panel

**c. Click Scheme Item within Expanded Menu:**
- Keep submenu expanded
- Highlight the scheme item (submenu button)
- Keep parent header highlighted as well
- Show selected scheme's configuration page

**d. Selected Scheme State:**
- When a scheme is selected, both the scheme item AND its parent expandable header remain highlighted
- This provides visual context of "where you are" in the navigation hierarchy

### 4. Arrow Position Specification

**Current Issue:** When ToggleButton is checked (expanded), arrow appears bottom-aligned relative to text.

**Expected Behavior:** Arrow should remain vertically centered in both collapsed and expanded states.

**Visual Details:**
- Button height: 40px
- Arrow size: 16x16
- In collapsed state: arrow centered, aligned with text baseline
- In expanded state: arrow should rotate 180° but stay centered (currently it shifts down)

**Root Cause Hypothesis:** The Path element might be shifting when ToggleButton IsChecked trigger applies background change. Possible Grid layout or VerticalAlignment issue in the expanded state.

**Fix Direction:** Ensure Path maintains VerticalAlignment="Center" and doesn't get pushed down by expanded state styling.

### 5. Feature Page Display (Clock, Pomodoro, Anniversary)

**Current State:** Nothing displayed except theme name (which shouldn't show there at all - theme name belongs only in Preview page).

**Historical Context:** Pages were working in first version, broken after first fix attempt.

**Debug Approach:**
1. Verify ViewModel initialization:
   - Check DesktopClockViewModel, PomodoroViewModel, AnniversaryViewModel constructors
   - Ensure no null reference exceptions during creation
   - Verify property initialization

2. Check binding resolution:
   - Verify DataTemplates registered in App.xaml
   - Check ContentControl binding in CreatorView.xaml
   - Look for binding errors in output window

3. Trace content loading:
   - Add debug logging to LoadFeatureContent method
   - Verify ConfigurationContent assignment
   - Check if View is being created but not displayed

4. Check for regression:
   - Compare current implementation with working first version
   - Identify what changed in ViewModel instantiation

**Key Clue:** Theme name appearing suggests the binding might be falling back to a default template or the wrong ViewModel is being instantiated.

</decisions>

---

<code_context>
## Existing Code Insights

### Reusable Assets
- **SchemeModel**: Has `IsActive` (bool) and `IsSelected` (bool) properties - both exist but may not be properly distinguished in UI
- **CreatorViewModel**: 
  - `CollapseAllNavExcept()` method exists
  - `OnIsXXXExpandedChanged` partial methods exist for single-expand
  - `SelectedFeature` property with computed selection bools exists
  - `LoadFeatureContent()` method handles page loading
- **DataTemplates**: All 9 feature DataTemplates registered in App.xaml (lines 59-89)
- **DI Registrations**: Views and ViewModels registered in App.xaml.cs (lines 40-61)

### Established Patterns
- **CommunityToolkit.Mvvm**: Partial methods for property change handling
- **MVVM**: DataContext bindings, Command bindings
- **Navigation**: FeatureType enum identifies expandable features
- **Styling**: FeatureExpanderButtonStyle (ToggleButton), SchemeItemTemplate (DataTemplate)

### Integration Points
- **CreatorView.xaml**: Main container with left navigation and right content panel
- **CreatorViewModel.cs**: Central coordinator for navigation state
- **App.xaml**: DataTemplate resources
- **App.xaml.cs**: Dependency injection registrations

### Known Working Components
- Desktop Background page display (after DI registration fix)
- Mouse Click page display (after DI registration fix)
- Navigation expansion/collapse mechanism (logic exists)
- Scheme selection command binding

### Suspected Problem Areas
1. **Clock/Pomodoro/Anniversary ViewModels**: May have initialization issues different from DesktopBackground/MouseClick
2. **Arrow Position**: Path alignment within ToggleButton Grid when IsChecked=true
3. **Highlight Consistency**: Expandable header highlight when child scheme is selected
4. **Scheme Auto-Creation**: Ensure default scheme created when expanding empty feature

</code_context>

---

<specifics>
## Specific Requirements

### Arrow Position Fix
**Current:** Arrow shifts down when ToggleButton IsChecked (expanded)  
**Expected:** Arrow stays vertically centered in both states  
**Button Specs:** Height=40px, Padding=16,0 (horizontal only)

### Scheme Highlight Color
**Color:** Based on AccentBrush, slightly lighter  
**Application:** Background of selected scheme button in submenu  
**Coexistence:** Checkmark (IsActive) can appear on same button alongside highlight

### Theme Name Display
**Location:** Should ONLY appear in Theme Preview page  
**Issue:** Currently appearing in other feature pages (indicates wrong content loaded)  
**Fix:** Ensure LoadFeatureContent creates correct ViewModel for simple features

### Auto-Create Scheme
**Trigger:** When expanding a feature with no schemes  
**Action:** Create default scheme named "{FeatureName} 1"  
**Display:** Immediately show the new scheme's page

</specifics>

---

<deferred>
## Deferred Ideas

- **Checkmark/Active State UI**: Full implementation of active scheme indicator with toggle capability  
- **Automated UI Testing Framework**: Comprehensive test suite for WPF UI interactions  
- **Performance Optimization**: Virtualization for large scheme lists  
- **Animation**: Smooth transitions for expand/collapse and highlight changes  

</deferred>

---

<implementation_notes>
## Implementation Notes for Planner

### Priority Order (based on user impact):
1. **Feature Page Display** - Critical, currently broken
2. **Arrow Position** - Visual polish, noticeable issue
3. **Navigation Highlight Consistency** - UX clarity
4. **Scheme Auto-Creation** - Convenience feature
5. **Checkmark/Active State** - Can be deferred if problematic

### Risk Areas:
- **Regression**: Previous fixes may have introduced new issues - verify existing functionality
- **State Management**: Multiple boolean flags (IsExpanded, IsSelected, IsActive) need careful coordination
- **Binding Complexity**: WPF bindings can fail silently - add debug logging

### Testing Checklist:
- [ ] Clock page displays correctly
- [ ] Pomodoro page displays correctly  
- [ ] Anniversary page displays correctly
- [ ] Arrow centered in expanded state
- [ ] Only one nav button highlighted at a time
- [ ] Single submenu expands at a time
- [ ] Scheme highlight visible when selected
- [ ] Expandable header stays highlighted when child scheme selected
- [ ] Auto-creation works for empty features

</implementation_notes>

---

*Phase: 01-fix*  
*Context gathered: 2026-03-14*
