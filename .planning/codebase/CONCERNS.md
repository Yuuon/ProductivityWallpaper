# Codebase Concerns

**Analysis Date:** 2026-03-14

## Window Initialization & Flickering Issues

**Critical Workaround Required:**
- **Issue:** WPF windows in `Show()` render at default size (800x450) briefly, causing visual flicker
- **Files:** `Services/WallpaperService.cs` (lines 393-414, 291-331)
- **Current Mitigation:** Complex initialization sequence - create at 1x1 pixel, move to (-32000, -32000), Show(), mount to WorkerW, then resize
- **Risk:** Any deviation from this sequence causes user-visible flickering
- **Impact:** High - affects every video window creation
- **Fragility:** The workaround relies on timing (`Task.Delay(100)`) which may fail on slower systems

**Action Video Layer Creation (lines 291-331):**
```csharp
// 1. Create at 1x1 size off-screen
_actionVideoWindow = CreateHiddenVideoWindow(videoPath);
_actionVideoWindow.Show(); // Still 1x1, invisible

// 2. Mount to desktop
InjectActionLayer(_actionVideoWindow, _idleVideoWindow, _currentUiWindow);

// 3. Buffer wait (timing-dependent!)
await Task.Delay(100);

// 4. Resize to fullscreen
Win32Api.SetWindowPos(helper.Handle, ..., screenW, screenH, ...);
```

## Video Transparency Limitations

**HwndHost Airspace Problem:**
- **Issue:** `LibVLCSharp.WPF.VideoView` uses `HwndHost` which does not support transparency
- **Files:** `Views/VideoPlayerWindow.xaml.cs` (entire file)
- **Impact:** Videos MUST have built-in backgrounds - transparent WebM overlays impossible
- **Workaround:** UI elements (bubbles, text) must render in separate `InteractiveUiWindow` layer
- **Constraint:** Video-to-video transitions will always show hard cuts, no alpha blending

## Z-Order & Layering Challenges

**Three-Layer Architecture Complexity:**
- **Files:** `Services/WallpaperService.cs` (lines 505-553)
- **Required Z-Order:** UI (top) > Action (middle) > Idle (bottom) > WorkerW
- **Risk:** Windows desktop composition changes, other apps manipulating WorkerW can break layering
- **Current Implementation:** Manual `SetWindowPos` calls with `HWND_TOP`/`HWND_BOTTOM`
- **Fragility Points:**
  - Action layer injection (line 535-553) must ensure UI stays on top
  - Idle layer reset during action playback (line 334-361) risks z-order race conditions

## Mouse Input Handling Complexities

**Global Hook Dependency:**
- **Issue:** Mounted WorkerW windows cannot receive direct mouse messages
- **Files:** `Services/MouseHookService.cs` (lines 35-86), `Views/InteractiveUiWindow.xaml.cs` (lines 94-139)
- **Implementation:** Low-level mouse hook (`WH_MOUSE_LL`) captures all system mouse events
- **Security Concern:** App functions as a global input monitor (potential keylogger pattern)
- **Performance Impact:** Every mouse movement processed through managed callback
- **Coordinate Transformation:** Screen-to-window coordinate conversion required for hit testing

**Sweep Detection Algorithm:**
- **Files:** `Services/MouseHookService.cs` (lines 88-181)
- **Hardcoded Threshold:** 3000 pixels/second (configurable but no validation)
- **Tracking Queue:** Fixed 5-position history with thread-safe locking
- **Cooldown Period:** 500ms to prevent rapid re-triggering

## Performance Considerations

**Resource Management Issues:**

1. **Multiple LibVLC Instances:**
   - `WallpaperService` creates `_tempLibVLC` for audio/metadata (lines 34-47)
   - Each `VideoPlayerWindow` creates separate `LibVLC` instance (line 17)
   - No instance pooling - could exhaust resources with many videos

2. **Video Player Lifecycle:**
   - Action videos create/destroy windows per interaction
   - Idle videos recreated on every action completion (line 334-361)
   - GC pressure from repeated window/media player disposal

3. **Thumbnail Generation:**
   - Blocks on `System.Drawing.Image.FromFile` (line 620) for images
   - Uses Shell API for video thumbnails (lines 633-647) - Windows-dependent
   - No async I/O for file operations

4. **Memory Leaks Risk:**
   - `Media` objects in `PlayAudio` (line 261) - `using` statement correct but verify disposal chain
   - Event handlers in `InteractiveUiWindow` - check for closure captures

## Platform Dependencies

**Windows-Specific APIs:**
- **Files:** `Services/Win32Api.cs` (entire file - 146 lines of P/Invoke)
- **Critical Dependencies:**
  - `SetParent` to WorkerW (desktop manipulation)
  - `SetWindowsHookEx` (global input capture)
  - `SHCreateItemFromParsingName` (Shell thumbnail generation)
  - Registry access for wallpaper/colorization (lines 448-456)

**Portability Blockers:**
- No abstraction layer for platform-specific operations
- WorkerW window class name is Windows implementation detail
- `Progman` message `0x052C` is undocumented Windows behavior (line 558)

## Error Handling & Silent Failures

**Empty Catch Blocks:**
- **Files:** `Services/WallpaperService.cs`
- **Lines:** 46, 92, 149, 287, 356-357, 455-456, 476-478, 481-484, 488-489, 593, 651
- **Pattern:** `try { ... } catch { }` suppresses all exceptions
- **Risk:** Failures go undetected, making debugging difficult
- **Examples:**
  - LibVLC initialization failure (line 46)
  - JSON deserialization failure (line 92, 149, 593)
  - Window disposal failures (lines 476-484)
  - Registry write failures (line 455-456)

**Missing Validation:**
- No bounds checking on `SweepSpeedThreshold` (could be negative/zero)
- No validation of `InteractiveConfig` coordinates (X, Y, Width, Height as percentages)
- File path injection possible via config files (no sanitization)

## Threading & Concurrency

**Dispatcher Usage:**
- **Files:** `Services/WallpaperService.cs` (lines 213-227, 233-248)
- Pattern: `Application.Current.Dispatcher.Invoke()` from mouse hook callback
- Risk: UI thread blocking if hook handler is slow

**Lock Contention:**
- **Files:** `Services/MouseHookService.cs` (line 90-131)
- Lock held during `ThreadPool.QueueUserWorkItem` (line 124) - may be unnecessary
- Position history queue accessed under lock on every mouse move

## Security Considerations

**Global Input Hook:**
- **Files:** `Services/MouseHookService.cs`
- App requires elevated privileges or user approval for global hooks on some systems
- Antivirus software may flag as potential keylogger
- All mouse movements captured even when not interacting with wallpaper

**File System Access:**
- Unrestricted file read access for video/audio paths
- Path traversal possible via crafted `config.wallpaper` files
- No validation that media files are within library folder

**Registry Modification:**
- **Files:** `Services/WallpaperService.cs` (lines 448-456)
- Modifies `HKEY_CURRENT_USER\Control Panel\Desktop`
- No permission checks before write

## Technical Debt Areas

**Hardcoded Values:**
```csharp
// Line 276: Default duration fallback
durationMs = 3000;

// Line 305: Buffer wait timing
await Task.Delay(100);

// Line 320: Fade timing calculation
int remainingTime = (int)durationMs - 500 - 300;

// Line 403-404: Off-screen position
win.Left = -32000;
win.Top = -32000;

// MouseHookService.cs line 16: Sweep threshold
public double SweepSpeedThreshold { get; set; } = 3000;
```

**Incomplete Error Context:**
- Debug.WriteLine only (lines 183, 189, 244, 266) - no structured logging
- Exception messages lost in empty catch blocks
- No telemetry or crash reporting

**Missing Features:**
- Monitor index parameter accepted but not implemented for multi-monitor
- `MediaType.Web` defined but no implementation
- `TriggerType.Special` not differentiated from Head/Body in logic

## Testing Gaps

**Untested Scenarios:**
- No unit tests for sweep detection algorithm
- No integration tests for WorkerW injection
- Manual coordinate transformation testing required
- No automated tests for flicker-free initialization

**Manual Verification Required:**
- Visual flickering on different GPU/drivers
- Z-order stability after prolonged use
- Memory usage during extended video playback

---

*Concerns audit: 2026-03-14*
