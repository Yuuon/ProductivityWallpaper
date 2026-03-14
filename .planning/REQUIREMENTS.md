# Requirements Document

**Project:** ProductivityWallpaper  
**Type:** Brownfield Enhancement  
**Version:** 1.0  
**Date:** 2026-03-14

---

## 1. Functional Requirements

### 1.1 System Awareness Module [NEW]
**Priority:** High  
**Source:** AGENTS.md Roadmap Section

The application must respond to system events with appropriate video content:

| Event | Trigger | Response |
|-------|---------|----------|
| Session Lock | Windows+L / Timeout | Play "sleep" video |
| Session Unlock | User login | Play "wake" video |
| Network Disconnect | Adapter down | Play "offline" video |
| Network Reconnect | Adapter up | Play "online" video |
| Power Low | Battery < 20% | Play "low battery" video |
| Power Charging | AC connected | Play "charging" video |

**Technical Requirements:**
- Subscribe to Windows session events (WTS_SESSION_LOCK/WTS_SESSION_UNLOCK)
- Monitor network adapter status changes
- Register for power notifications (WM_POWERBROADCAST)
- Graceful fallback if videos not configured

### 1.2 Window Stability [ENHANCE]
**Priority:** Critical  
**Source:** CONCERNS.md

Current flickering issues must be resolved:

- **REQ-WIN-001:** All video windows must initialize without visible flicker
- **REQ-WIN-002:** Anti-flicker sequence must work on systems with varying performance
- **REQ-WIN-003:** Window z-order must remain stable across desktop composition changes
- **REQ-WIN-004:** Recovery mechanism when WorkerW hierarchy is disrupted

### 1.3 Memory Management [ENHANCE]
**Priority:** High  
**Source:** CONCERNS.md

Resource lifecycle improvements:

- **REQ-MEM-001:** LibVLC instances must be pooled/reused where possible
- **REQ-MEM-002:** Video player windows must dispose cleanly without memory leaks
- **REQ-MEM-003:** Thumbnail generation must use async I/O
- **REQ-MEM-004:** Media objects must have verified disposal chain

### 1.4 Error Handling [ENHANCE]
**Priority:** High  
**Source:** CONCERNS.md

Replace silent failures with proper handling:

- **REQ-ERR-001:** All catch blocks must log meaningful error context
- **REQ-ERR-002:** Critical failures (VLC init, window creation) must surface to user
- **REQ-ERR-003:** JSON deserialization failures must show file path and line
- **REQ-ERR-004:** Registry operations must check permissions before write

---

## 2. Technical Requirements

### 2.1 Testing Infrastructure
**Priority:** High  
**Source:** TESTING.md

Establish automated testing:

- **Framework:** xUnit.net + FluentAssertions + Moq
- **Coverage Targets:**
  - ConfigService: 100%
  - MouseHookService sweep algorithm: 100%
  - ViewModels: 80%
  - WallpaperService helpers: 80%
- **Integration Tests:** WorkerW injection flow, config roundtrip
- **Mock Requirements:** IVideoPlayer, IWin32Wrapper, IMouseHook abstractions

### 2.2 Abstraction Layers
**Priority:** Medium  
**Source:** CONCERNS.md

Enable testability through interfaces:

```csharp
// Required abstractions
public interface IWindowManager { /* Win32 operations */ }
public interface IVideoPlayer : IDisposable { /* VLC wrapper */ }
public interface IMouseHook { /* Global hook */ }
public interface ISystemEvents { /* Session/power/network */ }
```

### 2.3 Validation
**Priority:** Medium  
**Source:** CONCERNS.md

Input validation requirements:

- **REQ-VAL-001:** InteractiveConfig coordinates must be validated (0-100% range)
- **REQ-VAL-002:** SweepSpeedThreshold must be positive non-zero
- **REQ-VAL-003:** File paths must be validated within library scope (prevent traversal)
- **REQ-VAL-004:** Media file formats must be verified before playback

### 2.4 Threading Safety
**Priority:** Medium  
**Source:** CONCERNS.md

Concurrency improvements:

- **REQ-THR-001:** Dispatcher.Invoke from hook callbacks must not block
- **REQ-THR-002:** MouseHookService position queue must minimize lock contention
- **REQ-THR-003:** Async operations must use ConfigureAwait appropriately

---

## 3. Non-Functional Requirements

### 3.1 Performance

- **REQ-PERF-001:** Video initialization < 200ms (current: ~100ms buffer + overhead)
- **REQ-PERF-002:** Mouse event latency < 16ms (60fps)
- **REQ-PERF-003:** Memory footprint < 500MB during normal operation
- **REQ-PERF-004:** Thumbnail generation must not block UI thread

### 3.2 Security

- **REQ-SEC-001:** File path validation to prevent directory traversal
- **REQ-SEC-002:** Config file sanitization (JSON parsing with strict validation)
- **REQ-SEC-003:** Optional: Permission check before installing global hooks

### 3.3 Maintainability

- **REQ-MAINT-001:** All magic numbers must be configurable constants
- **REQ-MAINT-002:** Hardcoded timings (100ms, 500ms, 3000px/s) must be documented
- **REQ-MAINT-003:** P/Invoke operations must have XML documentation
- **REQ-MAINT-004:** Complex logic must include inline comments

---

## 4. Future Requirements (Backlog)

### 4.1 Media Extension
- **REQ-FUT-001:** FFmpeg thumbnail generation for better video previews
- **REQ-FUT-002:** GIF/WebP support for lightweight animations
- **REQ-FUT-003:** HEIC image support

### 4.2 AI Integration
- **REQ-AI-001:** AI Studio view implementation (currently disabled)
- **REQ-AI-002:** LLM integration for dynamic character responses

---

## 5. Constraint Validation

| Constraint | Impact | Mitigation |
|------------|--------|------------|
| Windows-only | No Mac/Linux support | Documented limitation |
| No video transparency | WebM alpha not supported | Design around with layers |
| Global hooks required | AV software concerns | Document and provide opt-out |
| HwndHost airspace | UI must be separate layer | Three-layer architecture |

---

## 6. Definition of Done

A feature is complete when:
- [ ] Code implemented following CONVENTIONS.md
- [ ] Unit tests written (where testable)
- [ ] Manual testing completed per TESTING.md
- [ ] Documentation updated
- [ ] No new CONCERNS.md entries introduced
- [ ] Memory profiled for leaks
