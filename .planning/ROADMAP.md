# Development Roadmap

**Project:** ProductivityWallpaper  
**Version:** 1.1  
**Date:** 2026-03-14

---

## Phase Overview

| Phase | Name | Focus | Duration |
|-------|------|-------|----------|
| 1 | Foundation | Creator Theme UI Framework | 2-3 weeks |
| 2 | System Awareness | Event triggers + stability | 1-2 weeks |
| 3 | Quality | Testing + refactoring | 2-3 weeks |

---

## Phase 1: Foundation - Creator Theme UI

**Goal:** Complete the "创作主题" (Creator Theme) page UI framework with all feature pages

**Source:** SVG2XAML_GUIDE_2.md

### Phase 1 Plans

| Plan | Objective | Files | Wave |
|------|-----------|-------|------|
| 01-01 | Left Navigation with Multi-Scheme Support | CreatorView.xaml, SchemeModel.cs | 1 |
| 01-02 | Desktop Background Pages (Upload + Edit) | DesktopBackgroundView.xaml, MediaItemModel.cs | 2 |
| 01-03 | Mouse Click Page with Region Editor | MouseClickView.xaml, ClickRegionModel.cs | 2 |
| 01-04 | Shutdown/Boot Restart/Screen Wake Pages | *View.xaml, *ViewModel.cs (3 sets) | 3 |
| 01-05 | Desktop Clock & Pomodoro Pages | *View.xaml, ClockStyleModel.cs | 3 |
| 01-06 | Countdown Anniversary Page | AnniversaryView.xaml, AnniversaryEventModel.cs | 3 |

### Wave Structure

```
Wave 1: Navigation Framework
├── 01-01: Left navigation with expandable submenus
└── Enables: All feature pages to be loaded

Wave 2: Core Feature Pages
├── 01-02: Desktop Background (most complex media management)
├── 01-03: Mouse Click (interactive canvas editor)
└── Parallel: Both pages need navigation from Wave 1

Wave 3: Secondary Feature Pages
├── 01-04: System Event Pages (Shutdown, Boot, Wake) - same layout as Desktop Background
├── 01-05: Utility Pages (Clock, Pomodoro) - style selection pattern
├── 01-06: Anniversary Page - event management
└── Parallel: All independent once Wave 1 complete
```

### Key Features Implemented

**Multi-Scheme Support:**
- Desktop Background, Mouse Click, Shutdown, Boot Restart, Screen Wake support multiple schemes
- Expandable/collapsible left navigation
- Active scheme highlighting with checkmark
- "New Scheme" button in submenus

**Desktop Background:**
- Empty state with upload UI
- Edit state with image/video/audio lists
- File management (import, delete, preview)
- Playback mode selection
- Display mode per file (Fill, Center, Tile)
- Mute/unmute toggle
- Preview window for media

**Mouse Click:**
- Interactive canvas maintaining screen aspect ratio
- Draw rectangular regions by dragging
- Region selection and deletion
- Media assignment per region (1 visual, up to 5 audio)
- Background preview from Desktop Background media

**System Event Pages (Shutdown/Boot/Wake):**
- Same layout as Desktop Background
- Independent data per feature
- Triggered by system events (Phase 2)

**Desktop Clock:**
- Style selection grid
- 12h/24h toggle per style
- Opacity slider (0-100%)
- Single active style

**Pomodoro Timer:**
- Timer style selection
- Do Not Disturb toggle
- Work/break duration settings

**Countdown Anniversary:**
- Display style selection
- Event list with editable names
- Repeat modes: No Repeat, Yearly, Monthly, Weekly
- Adaptive date picker per repeat mode
- Days remaining calculation

### Phase 1 Success Criteria

- [x] Left navigation with expandable submenus for 5 multi-scheme features
- [x] Desktop Background: upload and edit pages with file management
- [x] Mouse Click: interactive region editor with canvas
- [x] Shutdown/Boot Restart/Screen Wake: same layout as Desktop Background
- [x] Desktop Clock: style selection with format and opacity
- [x] Pomodoro: timer with DND and duration settings
- [x] Anniversary: event management with repeat modes
- [x] All pages integrated with CreatorView navigation
- [x] Localization keys added for new UI text

---

## Phase 2-fix: Creator View UI Fixes (03-fix-v2)

**Goal:** Fix remaining UI/UX issues from Phase 01

**Status:** ✅ Completed

### 03-fix-v2-03-01: Creator View Fixes

| Task | Fix | Status |
|------|-----|--------|
| 1 | New scheme button text ("+ 新建方案") | ✅ |
| 2 | Navigation single highlight logic | ✅ |
| 3 | Single expand logic for submenus | ✅ |
| 4 | Scheme item selection highlight | ✅ |
| 5 | Theme preview split ratio (70/30) | ✅ |
| 6 | Scrollbar style (brighter thumb, reserved space) | ✅ |
| 7 | Arrow vertical alignment | ✅ |
| 8 | Feature pages display (DI/DataTemplate fixes) | ✅ |

**Files Modified:**
- Views/CreatorView.xaml
- ViewModels/CreatorViewModel.cs
- Models/SchemeModel.cs
- Resources/Theme.xaml
- App.xaml.cs

**Summary:** All 9 critical UI issues from Phase-01-Issue-Fix.md resolved.

---

## Phase 01-fix: Creator View Final Fixes

**Goal:** Fix persistent Creator View UI/UX issues from Phase-01-Issue-Fix.md (third fix attempt)

**Status:** ✅ Complete

**Requirements:** FIX-01, FIX-02, FIX-03, FIX-04, FIX-05

### Phase 01-fix Plans

| Plan | Objective | Files | Wave | Requirements | Status |
|------|-----------|-------|------|--------------|--------|
| 01-fix-01 | Arrow vertical alignment fix | CreatorView.xaml | 1 | FIX-02 | ✅ |
| 01-fix-02 | Scheme auto-creation on expand | CreatorViewModel.cs, CreatorView.xaml | 1 | FIX-04 | ✅ |
| 01-fix-03 | Navigation highlight consistency | CreatorViewModel.cs, CreatorView.xaml | 1 | FIX-03 | ✅ |
| 01-fix-04 | Checkmark vs highlight distinction | CreatorView.xaml, CreatorViewModel.cs | 2 | FIX-05 | ✅ |
| 01-fix-05 | Feature page display fix (critical) | CreatorViewModel.cs, CreatorView.xaml, App.xaml | 2 | FIX-01 | ✅ |

### Wave Structure

```
Wave 1: Navigation and Visual Fixes (Parallel)
├── 01-fix-01: Arrow stays centered when expanded
├── 01-fix-02: Auto-create scheme when expanding empty feature
└── 01-fix-03: Parent header highlighted when child scheme selected

Wave 2: State and Content Fixes
├── 01-fix-04: IsSelected (highlight) vs IsActive (checkmark) distinction
└── 01-fix-05: Clock/Pomodoro/Anniversary pages display correctly
    Depends on: 01-fix-03 (navigation state), 01-fix-04 (selection state)
```

### Issues Being Fixed

| ID | Issue | Description |
|----|-------|-------------|
| FIX-01 | Feature page display | Clock/Pomodoro/Anniversary pages show content, not theme name |
| FIX-02 | Arrow vertical alignment | Arrow stays centered when ToggleButton expanded |
| FIX-03 | Navigation highlight consistency | Parent header highlighted when child scheme selected |
| FIX-04 | Scheme auto-creation | Empty features auto-create "{FeatureName} 1" on expand |
| FIX-05 | Checkmark/active state | Clear distinction: highlight=viewing, checkmark=enabled |

---

## Phase 01-fix-v2: Creator View Critical Fixes (4th Attempt)

**Goal:** Emergency fix for Creator View - fix page display for all features, add monitoring, fix highlight persistence, UI polish

**Status:** 🔄 In Progress (2/3 plans complete)

**Requirements:** FIX-V2-001, FIX-V2-002, FIX-V2-003, FIX-V2-004

### Phase 01-fix-v2 Plans

| Plan | Objective | Files | Wave | Requirements | Status |
|------|-----------|-------|------|--------------|--------|
| 01-fix-v2-01 | Page display debugging + Navigation monitor | NavigationMonitorService.cs, CreatorViewModel.cs | 1 | FIX-V2-001, FIX-V2-002 | ✅ Complete |
| 01-fix-v2-02 | Expandable button highlight persistence fix | CreatorViewModel.cs | 1 | FIX-V2-003 | ✅ Complete |
| 01-fix-v2-03 | UI polish (checkmark removal, arrow, scrollbar) | CreatorView.xaml | 2 | FIX-V2-004 | 📝 Planned |

### Wave Structure

```
Wave 1: Critical Fixes (Parallel)
├── 01-fix-v2-01: Navigation monitoring + error handling for all ViewModel factories
│   └── Creates: NavigationMonitorService with logging
│   └── Fixes: Silent failures in LoadFeatureContent()
└── 01-fix-v2-02: Expandable highlight cleanup when collapsing
    └── Fixes: Buttons staying highlighted after collapse

Wave 2: UI Polish
└── 01-fix-v2-03: Remove checkmarks, center arrows, verify scrollbar
    └── Depends on: Wave 1 for testing infrastructure
```

### Issues Being Fixed

| ID | Issue | Priority | Description |
|----|-------|----------|-------------|
| FIX-V2-001 | Page display | P0 | All 9 features must display, not just Theme Preview |
| FIX-V2-002 | Navigation monitor | P1 | Runtime monitoring of button→ViewModel→success flow |
| FIX-V2-003 | Highlight persistence | P1 | Expandable buttons clear highlight when collapsed |
| FIX-V2-004 | UI polish | P2 | Remove checkmark, center arrow, scrollbar spacing |

---

## Phase 01-fix-v3: Creator View Root Cause Fixes (5th Attempt - FINAL)

**Goal:** Definitive fix for Creator View - ROOT CAUSE IDENTIFIED: ViewModels ARE created (logs prove it) but ContentControl not displaying them. This is a WPF DataTemplate/content display issue, NOT ViewModel creation issue.

**Status:** ✅ Complete (all planned plans executed)

**Requirements:** FIX-V3-001, FIX-V3-002, FIX-V3-003, FIX-V3-004, FIX-V3-005

### Phase 01-fix-v3 Plans

| Plan | Objective | Files | Wave | Requirements | Status |
|------|-----------|-------|------|--------------|--------|
| 01-fix-v3-01 | Content display fix + Navigation rewrite + Image converter | CreatorView.xaml, CreatorViewModel.cs, CreatorViewState.cs, StringToImageSourceConverter.cs | 1 | FIX-V3-001, FIX-V3-002, FIX-V3-003 | ✅ Complete |
| 01-fix-v3-02 | Scheme highlight + UI polish verification | CreatorView.xaml, Theme.xaml | 2 | FIX-V3-004, FIX-V3-005 | ✅ Complete |

### Wave Structure

```
Wave 1: Critical Content Display (Root Cause Fix)
├── 01-fix-v3-01: ContentPresenter fix, CreatorViewState enum, Image converter
│   └── Fixes: ContentControl not displaying ViewModels despite creation
│   └── Creates: CreatorViewState enum for single navigation state
│   └── Creates: StringToImageSourceConverter for empty PreviewImagePath
│   └── Depends on: All prior work for base structure

Wave 2: UI Polish
└── 01-fix-v3-02: Scheme highlight verification, arrow/scrollbar verification
    └── Depends on: Wave 1 for navigation state correctness
```

### Issues Being Fixed

| ID | Issue | Priority | Description |
|----|-------|----------|-------------|
| FIX-V3-001 | ContentControl display | P0 | ViewModels created but ContentControl not displaying them - use ContentPresenter with explicit null ContentTemplate |
| FIX-V3-002 | Unified navigation state | P0 | Replace multiple boolean flags with single CreatorViewState enum, one source of truth |
| FIX-V3-003 | ImageSource binding | P1 | StringToImageSourceConverter for empty PreviewImagePath causing binding errors |
| FIX-V3-004 | Scheme highlight | P1 | Verify IsSelected trigger works with SchemeSelectedBrush for visual feedback |
| FIX-V3-005 | UI polish | P2 | Arrow centering, scrollbar padding verification, button MinWidth |

### Key Insight

Previous 4 attempts failed because they focused on ViewModel creation (which already works per NavigationMonitor logs) instead of content display (which is broken). This attempt focuses on:
1. ContentControl/ContentPresenter display mechanism
2. Single enum state (not multiple booleans)
3. Image binding converter

---

## Phase 02: Data Structure

**Goal:** Establish complete data structures for theme packages and local widget settings

**Source:** Phase-02-DataStructure.md

**Requirements:** DS-01, DS-02, DS-03, DS-04, DS-05, DS-06, DS-07

### Phase 02 Plans

| Plan | Objective | Files | Wave | Requirements |
|------|-----------|-------|------|--------------|
| 02-01 | Theme Manifest and Resource Models | ThemeManifest.cs, ResourceEntry.cs, ThemeService.cs | 1 | DS-01, DS-02 |
| 02-02 | Scheme Resource References | FeatureType.cs, SchemeModel.cs, ClickRegionModel.cs | 1 | DS-03, DS-04, DS-05 |
| 02-03 | System Events and Widget Settings | UserWidgetSettings.cs, UserSettingsService.cs | 2 | DS-06, DS-07 |

### Wave Structure

```
Wave 1: Core Models (Parallel)
├── 02-01: Theme manifest, resource library, loading services
└── 02-02: FeatureType extension, ID-based references, ClickAction

Wave 2: User Settings
└── 02-03: User widget settings, anniversary events, persistence
    Depends on: 02-01 (ThemeManifest), 02-02 (SchemeModel)
```

### Key Data Structures

**Theme Package:**
- `theme.json` manifest with metadata, resource library, schemes
- Resources organized by type: `/images/`, `/videos/`, `/audio/`
- Thumbnails in `/thumbnails/`

**Scheme References:**
- ID-based media references (not embedded objects)
- MediaReferenceList for ordered playback
- ClickAction for visual + audio (max 5)

**System Events:**
- Extended FeatureType: SessionLock, SessionUnlock, NetworkDisconnect, NetworkReconnect, PowerLow, PowerCharging
- Same multi-scheme support as Desktop Background

**User Settings:**
- Separate from theme data (UserWidgetSettings)
- Pomodoro: global defaults + per-theme override toggle
- Anniversary: user events only (personal data)

---

## Phase 2: System Awareness

**Goal:** Implement system event monitoring and video triggering

**Requirements:** REQ-1.1, REQ-1.2, REQ-1.4

**Deliverables:**
- `Services/SystemEventService.cs` - Monitor Windows events
- `Services/LoggingService.cs` - Replace silent failures
- Extended configuration schema for system events

**System Events to Monitor:**
- Session Lock/Unlock
- Network Disconnect/Reconnect
- Power Low/Charging

**Integration:**
- Connect system events to feature pages from Phase 1
- When event triggers, play configured video from corresponding scheme

---

## Phase 3: Quality & Testing

**Goal:** Establish testing infrastructure and fix technical debt

**Requirements:** REQ-2.1, REQ-2.2, REQ-2.3, REQ-1.3

**Deliverables:**
- Test project with xUnit
- Abstraction interfaces for Win32/VLC
- Validation layer
- Memory management improvements

---

## Phase Dependencies

```
Phase 1 (Creator Theme UI)
├── Required for Phase 02
│   └── Feature pages for data structure integration
└── Independent
    └── UI-only, no backend integration yet

Phase 02 (Data Structure)
├── Required for Phase 2 (System Awareness)
│   ├── ThemeManifest for event configuration storage
│   └── Extended FeatureType enum
└── Required for Creator
    └── Theme export/load functionality

Phase 2 (System Awareness)
├── Required for Phase 3
│   └── Stable event handling for testing
└── Builds on
    ├── Phase 1 UI for configuration
    └── Phase 02 data structures

Phase 3 (Quality)
└── Builds on
    ├── Phase 1 (UI to test)
    ├── Phase 02 (data models to test)
    └── Phase 2 (event handling to test)
```

---

## Milestones

| Milestone | Phase | Target | Key Deliverable |
|-----------|-------|--------|-----------------|
| M1 | 1 | Week 1 | Navigation + Desktop Background |
| M2 | 1 | Week 2 | Mouse Click + System Event Pages |
| M3 | 1 | Week 3 | Clock, Pomodoro, Anniversary |
| M4 | 2 | Week 4 | System event monitoring working |
| M5 | 3 | Week 6 | Test suite passing |
| M6 | 3 | Week 7 | Quality release |

---

## Phase Status

| Phase | Name | Status | Plans |
|-------|------|--------|-------|
| 01-foundation | Creator Theme UI | ✅ Complete | 6 |
| 01-fix | Creator View Fixes | ✅ Complete | 5 |
| 01-fix-v2 | Critical Fixes | ✅ Complete | 3 |
| 01-fix-v3 | Root Cause Fixes | ✅ Complete | 2 |
| **02** | **Data Structure** | 📝 Planned | **3** |
| 2 | System Awareness | 📋 Backlog | - |
| 3 | Quality & Testing | 📋 Backlog | - |

## Next Step

Phase 02 is planned with 3 executable plans. Ready for execution.

Run `/gsd-execute-phase 02` to begin implementing Phase 02: Data Structure.
