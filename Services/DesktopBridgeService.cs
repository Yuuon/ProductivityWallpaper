using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Interop;
using System.Windows;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Manages the desktop injection layer — detecting whether to use classic WorkerW
    /// or Win11 Raised Desktop mode, and monitoring WorkerW lifecycle for auto-recovery.
    /// 
    /// Win11 Raised Desktop Mode:
    ///   Progman has WS_EX_NOREDIRECTIONBITMAP — no GDI content.
    ///   SHELLDLL_DefView is WS_EX_LAYERED child of Progman.
    ///   WorkerW is a child of Progman (not a top-level sibling).
    ///   Wallpaper must be injected as Progman child, z-ordered under DefView.
    /// 
    /// Classic Mode (Win10 / older Win11):
    ///   WorkerW is a top-level window created via 0x052C message to Progman.
    ///   Wallpaper is injected as WorkerW child.
    /// </summary>
    public class DesktopBridgeService : IDisposable
    {
        /// <summary>
        /// Fired when WorkerW is destroyed and wallpapers need re-injection.
        /// </summary>
        public event Action? OnDesktopLayerInvalidated;

        private IntPtr _workerWHook = IntPtr.Zero;
        private Win32Api.WinEventDelegate? _destroyEventProc;
        private bool _disposed;

        // Cached desktop state
        private IntPtr _cachedWorkerW = IntPtr.Zero;
        private IntPtr _cachedProgman = IntPtr.Zero;
        private IntPtr _cachedShellDefView = IntPtr.Zero;
        private bool _isRaisedDesktop;

        /// <summary>
        /// Whether the current desktop is in Win11 Raised Desktop mode.
        /// </summary>
        public bool IsRaisedDesktop => _isRaisedDesktop;

        /// <summary>
        /// The cached WorkerW handle (valid in both modes).
        /// </summary>
        public IntPtr WorkerW => _cachedWorkerW;

        /// <summary>
        /// The cached Progman handle.
        /// </summary>
        public IntPtr Progman => _cachedProgman;

        /// <summary>
        /// The SHELLDLL_DefView handle (relevant in raised desktop mode for z-ordering).
        /// </summary>
        public IntPtr ShellDefView => _cachedShellDefView;

        /// <summary>
        /// Places an injected wallpaper/background window in the correct z-order for the current desktop mode.
        /// In Win11 Raised Desktop, using HWND_TOP after injection can lift wallpaper above icons; keep it
        /// behind SHELLDLL_DefView and above WorkerW instead.
        /// </summary>
        public void PositionWallpaperWindow(IntPtr windowHandle, int x, int y, int width, int height)
        {
            IntPtr zOrder = _isRaisedDesktop && _cachedShellDefView != IntPtr.Zero
                ? _cachedShellDefView
                : Win32Api.HWND_BOTTOM;

            Win32Api.SetWindowPos(windowHandle, zOrder,
                x, y, width, height, Win32Api.SWP_NOACTIVATE);

            // After repositioning a child of Progman with WS_EX_LAYERED, the redirection bitmap
            // does not always repaint automatically when the size changes. Force a full redraw so
            // GDI/WPF content (image wallpaper) renders at the new size instead of the original
            // 1x1 pre-injection size.
            Win32Api.InvalidateRect(windowHandle, IntPtr.Zero, true);
            Win32Api.UpdateWindow(windowHandle);
        }

        /// <summary>
        /// Places an injected overlay/action window above wallpaper content but still below desktop icons
        /// on raised Win11 desktops.
        /// </summary>
        public void PositionOverlayWindow(IntPtr windowHandle, int x, int y, int width, int height)
        {
            IntPtr zOrder = _isRaisedDesktop && _cachedShellDefView != IntPtr.Zero
                ? _cachedShellDefView
                : Win32Api.HWND_TOP;

            Win32Api.SetWindowPos(windowHandle, zOrder,
                x, y, width, height, Win32Api.SWP_NOACTIVATE);

            Win32Api.InvalidateRect(windowHandle, IntPtr.Zero, true);
            Win32Api.UpdateWindow(windowHandle);
        }

        /// <summary>
        /// Re-applies overlay z-order without changing size or position.
        /// </summary>
        public void KeepOverlayBehindIcons(IntPtr windowHandle)
        {
            IntPtr zOrder = _isRaisedDesktop && _cachedShellDefView != IntPtr.Zero
                ? _cachedShellDefView
                : Win32Api.HWND_TOP;

            Win32Api.SetWindowPos(windowHandle, zOrder,
                0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
        }

        /// <summary>
        /// Re-applies wallpaper z-order without changing size or position.
        /// </summary>
        public void KeepWallpaperBehindIcons(IntPtr windowHandle)
        {
            if (_isRaisedDesktop && _cachedShellDefView != IntPtr.Zero)
            {
                Win32Api.SetWindowPos(windowHandle, _cachedShellDefView,
                    0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
            }
            else
            {
                Win32Api.SetWindowPos(windowHandle, Win32Api.HWND_BOTTOM,
                    0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
            }
        }

        /// <summary>
        /// Sets up the desktop layer: finds WorkerW, detects raised desktop mode,
        /// and starts lifecycle monitoring.
        /// </summary>
        public bool SetupDesktopLayer()
        {
            _cachedProgman = Win32Api.FindWindow("Progman", null);
            if (_cachedProgman == IntPtr.Zero)
            {
                Debug.WriteLine("[DesktopBridge] Progman not found");
                return false;
            }

            // Send the magic 0x052C message to trigger WorkerW creation
            Win32Api.SendMessageTimeout(_cachedProgman, 0x052C, new UIntPtr(0xD), new IntPtr(0x1),
                0x0, 1000, out _);

            // Detect raised desktop mode: check if Progman has WS_EX_NOREDIRECTIONBITMAP
            int progmanExStyle = Win32Api.GetWindowLong(_cachedProgman, Win32Api.GWL_EXSTYLE);
            _isRaisedDesktop = (progmanExStyle & Win32Api.WS_EX_NOREDIRECTIONBITMAP) != 0;

            if (_isRaisedDesktop)
            {
                Debug.WriteLine("[DesktopBridge] Raised Desktop mode detected (Win11)");
                return SetupRaisedDesktopMode();
            }
            else
            {
                Debug.WriteLine("[DesktopBridge] Classic WorkerW mode detected");
                return SetupClassicMode();
            }
        }

        /// <summary>
        /// Injects a window into the desktop layer with proper mode-aware handling.
        /// For raised desktop: parents to Progman, adds WS_EX_LAYERED, z-orders under DefView.
        /// For classic: parents to WorkerW.
        /// </summary>
        /// <param name="windowHandle">The HWND of the window to inject.</param>
        /// <param name="asTopmost">If true, place at top z-order within the layer (for overlays).
        /// If false, place at the bottom (for wallpaper content).</param>
        public void InjectWindow(IntPtr windowHandle, bool asTopmost = false)
        {
            // Bug 4 fix (Oracle-diagnosed): on Win11 24H2 the desktop topology can be
            // recomposed by Explorer between injections. Cached Progman/DefView handles
            // can become stale or no longer reflect the active desktop host. Re-validate
            // before every inject; refresh cached topology if anything looks off.
            if (!ValidateCachedTopology())
            {
                Debug.WriteLine("[DesktopBridge] Cached topology invalid — refreshing before inject.");
                SetupDesktopLayer();
            }

            if (_isRaisedDesktop)
            {
                InjectRaisedDesktop(windowHandle, asTopmost);
            }
            else
            {
                InjectClassic(windowHandle, asTopmost);
            }
        }

        /// <summary>
        /// Validates that cached desktop handles still reflect a coherent desktop topology.
        /// Returns false if anything is stale, signaling that SetupDesktopLayer should re-run.
        /// </summary>
        private bool ValidateCachedTopology()
        {
            if (_cachedProgman == IntPtr.Zero || !Win32Api.IsWindow(_cachedProgman))
                return false;

            var className = new StringBuilder(64);
            Win32Api.GetClassName(_cachedProgman, className, className.Capacity);
            if (className.ToString() != "Progman")
                return false;

            if (_isRaisedDesktop)
            {
                if (_cachedShellDefView == IntPtr.Zero || !Win32Api.IsWindow(_cachedShellDefView))
                    return false;

                className.Clear();
                Win32Api.GetClassName(_cachedShellDefView, className, className.Capacity);
                if (className.ToString() != "SHELLDLL_DefView")
                    return false;

                // DefView must still be a child of cached Progman; otherwise the
                // SetWindowPos sibling-anchor in InjectRaisedDesktop is invalid.
                if (Win32Api.GetParent(_cachedShellDefView) != _cachedProgman)
                    return false;
            }
            else
            {
                if (_cachedWorkerW == IntPtr.Zero || !Win32Api.IsWindow(_cachedWorkerW))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the physical screen dimensions using the appropriate method for the current mode.
        /// </summary>
        public (int width, int height) GetScreenDimensions()
        {
            // Try to get from WorkerW client rect first (most accurate)
            IntPtr target = _isRaisedDesktop ? _cachedProgman : _cachedWorkerW;
            if (target != IntPtr.Zero && Win32Api.GetClientRect(target, out var rect))
            {
                int w = rect.right - rect.left;
                int h = rect.bottom - rect.top;
                if (w > 0 && h > 0) return (w, h);
            }

            // Fallback to GetSystemMetrics
            return (Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN),
                    Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN));
        }

        /// <summary>
        /// Invalidates the cached WorkerW handle and forces re-discovery.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedWorkerW = IntPtr.Zero;
            _cachedShellDefView = IntPtr.Zero;
        }

        /// <summary>
        /// Checks whether a physical screen point targets the desktop (not covered by any foreground window).
        /// Uses WindowFromPoint to find the topmost window at the given coordinates, then walks
        /// the parent chain checking window class names to determine if it belongs to the desktop
        /// hierarchy (any "WorkerW" or "Progman" class window).
        /// 
        /// This prevents click regions from firing when other windows (browsers, popups, file explorer)
        /// are covering the desktop at the click point.
        /// 
        /// Note: We check class names instead of comparing cached handle values because in classic mode,
        /// the 0x052C message may create multiple WorkerW windows — one holds SHELLDLL_DefView (desktop
        /// icons) and a different one is our injection target. Both are part of the desktop hierarchy but
        /// only the injection target is cached in _cachedWorkerW.
        /// </summary>
        /// <param name="physicalX">X coordinate in physical screen pixels.</param>
        /// <param name="physicalY">Y coordinate in physical screen pixels.</param>
        /// <returns>True if the click targets the desktop layer; false if another window covers it.</returns>
        public bool IsDesktopClick(int physicalX, int physicalY)
        {
            var pt = new Win32Api.POINT { x = physicalX, y = physicalY };
            IntPtr hwndAtPoint = Win32Api.WindowFromPoint(pt);

            if (hwndAtPoint == IntPtr.Zero) return false;

            // Walk the parent chain checking class names at each level.
            // If any window in the chain has class "Progman" or "WorkerW", the click
            // targets the desktop layer (not a foreground application window).
            const int maxClassNameLength = 256;
            var className = new StringBuilder(maxClassNameLength);
            IntPtr current = hwndAtPoint;
            while (current != IntPtr.Zero)
            {
                className.Clear();
                Win32Api.GetClassName(current, className, maxClassNameLength);
                string cls = className.ToString();

                if (cls == "Progman" || cls == "WorkerW")
                    return true;

                IntPtr parent = Win32Api.GetParent(current);
                if (parent == IntPtr.Zero || parent == current)
                    break;
                current = parent;
            }

            return false;
        }

        // --- Private Setup Methods ---

        private bool SetupRaisedDesktopMode()
        {
            // In raised desktop mode, SHELLDLL_DefView is a child of Progman
            _cachedShellDefView = Win32Api.FindWindowEx(_cachedProgman, IntPtr.Zero,
                "SHELLDLL_DefView", null);

            if (_cachedShellDefView == IntPtr.Zero)
            {
                Debug.WriteLine("[DesktopBridge] SHELLDLL_DefView not found as Progman child — falling back to classic");
                _isRaisedDesktop = false;
                return SetupClassicMode();
            }

            // WorkerW is also a child of Progman in raised mode
            _cachedWorkerW = Win32Api.FindWindowEx(_cachedProgman, IntPtr.Zero, "WorkerW", null);

            Debug.WriteLine($"[DesktopBridge] Raised Desktop: Progman={_cachedProgman}, DefView={_cachedShellDefView}, WorkerW={_cachedWorkerW}");
            StartLifecycleMonitoring();
            return true;
        }

        private bool SetupClassicMode()
        {
            // Classic mode: enumerate top-level windows to find WorkerW
            _cachedWorkerW = IntPtr.Zero;

            Win32Api.EnumWindows((hwnd, lParam) =>
            {
                var defView = Win32Api.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    _cachedShellDefView = defView;
                    // WorkerW is the next sibling after the window containing DefView
                    _cachedWorkerW = Win32Api.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (_cachedWorkerW == IntPtr.Zero)
            {
                Debug.WriteLine("[DesktopBridge] WorkerW not found in classic mode");
                return false;
            }

            Debug.WriteLine($"[DesktopBridge] Classic: WorkerW={_cachedWorkerW}");
            StartLifecycleMonitoring();
            return true;
        }

        private void InjectRaisedDesktop(IntPtr windowHandle, bool asTopmost)
        {
            // Convert to child window. Preserve WS_VISIBLE explicitly — when transitioning
            // popup -> child, some style flags get dropped and the window can disappear.
            int style = Win32Api.GetWindowLong(windowHandle, Win32Api.GWL_STYLE);
            style = (style & ~Win32Api.WS_POPUP) | Win32Api.WS_CHILD | Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(windowHandle, Win32Api.GWL_STYLE, style);

            // Add WS_EX_LAYERED for DWM compositing in raised desktop mode — Lively does this
            // and it is required for the redirection bitmap to be created so DWM can composite
            // our content into the desktop scene.
            int exStyle = Win32Api.GetWindowLong(windowHandle, Win32Api.GWL_EXSTYLE);
            exStyle |= Win32Api.WS_EX_LAYERED;
            Win32Api.SetWindowLong(windowHandle, Win32Api.GWL_EXSTYLE, exStyle);

            // Set full opacity (required for WS_EX_LAYERED — otherwise window is invisible)
            Win32Api.SetLayeredWindowAttributes(windowHandle, 0, 255, Win32Api.LWA_ALPHA);

            // Parent to Progman (not WorkerW!)
            // Bug 4 fix: verify SetParent actually stuck. On Win11 24H2 with stale
            // topology, SetParent can succeed-as-call but the desktop host isn't the
            // currently-active one, leaving the window as a free-floating top-level.
            Win32Api.SetParent(windowHandle, _cachedProgman);
            var actualParent = Win32Api.GetParent(windowHandle);
            if (actualParent != _cachedProgman)
            {
                Debug.WriteLine($"[DesktopBridge] SetParent verification failed (actual=0x{actualParent.ToInt64():X}, expected=0x{_cachedProgman.ToInt64():X}). Refreshing topology and retrying.");
                SetupDesktopLayer();
                Win32Api.SetParent(windowHandle, _cachedProgman);
                actualParent = Win32Api.GetParent(windowHandle);
                if (actualParent != _cachedProgman)
                {
                    Debug.WriteLine($"[DesktopBridge] SetParent retry FAILED. Window 0x{windowHandle.ToInt64():X} will remain free-floating.");
                    return; // give up — caller's window stays top-level but at least we don't crash
                }
            }

            // Place behind SHELLDLL_DefView so desktop icons and wallpaper content remain in the
            // correct order. Same anchor for both wallpaper and overlay — caller controls intent
            // via subsequent PositionWallpaperWindow / PositionOverlayWindow calls.
            IntPtr insertAfter = _cachedShellDefView != IntPtr.Zero
                ? _cachedShellDefView
                : Win32Api.HWND_BOTTOM;

            Win32Api.SetWindowPos(windowHandle, insertAfter,
                0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
        }

        private void InjectClassic(IntPtr windowHandle, bool asTopmost)
        {
            if (_cachedWorkerW == IntPtr.Zero) return;

            // Preserve WS_VISIBLE through reparent — same rationale as InjectRaisedDesktop.
            int style = Win32Api.GetWindowLong(windowHandle, Win32Api.GWL_STYLE);
            style = (style & ~Win32Api.WS_POPUP) | Win32Api.WS_CHILD | Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(windowHandle, Win32Api.GWL_STYLE, style);

            Win32Api.SetParent(windowHandle, _cachedWorkerW);

            IntPtr zOrder = asTopmost ? Win32Api.HWND_TOP : Win32Api.HWND_BOTTOM;
            Win32Api.SetWindowPos(windowHandle, zOrder,
                0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
        }

        private void EnsureWorkerWZOrder()
        {
            // NOTE: previously we pushed WorkerW to HWND_BOTTOM inside Progman whenever it
            // was not the last child. That fights DefView's natural z-order on Win11 24H2 raised
            // desktop and can sink injected wallpaper windows below the rendering surface,
            // resulting in a blank/black screen. Lively does not perform this correction —
            // it relies on the SetWindowPos(handle, SHELLDLL_DefView, ...) anchor in
            // PositionWallpaperWindow / InjectRaisedDesktop to keep wallpaper behind icons.
            //
            // Kept as a no-op so existing call sites compile; remove call sites in a future cleanup.
            if (!_isRaisedDesktop) return;
        }

        // --- Lifecycle Monitoring ---

        private void StartLifecycleMonitoring()
        {
            StopLifecycleMonitoring();

            _destroyEventProc = OnWindowEvent;
            _workerWHook = Win32Api.SetWinEventHook(
                Win32Api.EVENT_OBJECT_DESTROY,
                Win32Api.EVENT_OBJECT_DESTROY,
                IntPtr.Zero,
                _destroyEventProc,
                0, 0,
                Win32Api.WINEVENT_OUTOFCONTEXT | Win32Api.WINEVENT_SKIPOWNPROCESS);

            Debug.WriteLine("[DesktopBridge] Lifecycle monitoring started");
        }

        private void StopLifecycleMonitoring()
        {
            if (_workerWHook != IntPtr.Zero)
            {
                Win32Api.UnhookWinEvent(_workerWHook);
                _workerWHook = IntPtr.Zero;
            }
            _destroyEventProc = null;
        }

        private void OnWindowEvent(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType != Win32Api.EVENT_OBJECT_DESTROY) return;

            // Check if the destroyed window is our cached WorkerW
            if (hwnd == _cachedWorkerW && _cachedWorkerW != IntPtr.Zero)
            {
                Debug.WriteLine("[DesktopBridge] WorkerW destroyed — invalidating and requesting re-injection");
                InvalidateCache();
                OnDesktopLayerInvalidated?.Invoke();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopLifecycleMonitoring();
                _disposed = true;
            }
        }
    }
}
