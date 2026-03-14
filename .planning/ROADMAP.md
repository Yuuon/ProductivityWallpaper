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
├── Required for Phase 2
│   ├── All feature pages must exist
│   └── Scheme management for event configuration
└── Independent
    └── UI-only, no backend integration yet

Phase 2 (System Awareness)
├── Required for Phase 3
│   └── Stable event handling for testing
└── Builds on
    └── Phase 1 UI for configuration

Phase 3 (Quality)
└── Builds on
    ├── Phase 1 (UI to test)
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

## Next Step

Run `/gsd-execute-phase 01-fix` to begin Phase 01-fix execution.
