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
            Win32Api.SendMessageTimeout(_cachedProgman, 0x052C, UIntPtr.Zero, IntPtr.Zero,
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
        /// the parent chain to determine if it belongs to the desktop hierarchy (WorkerW or Progman).
        /// 
        /// This prevents click regions from firing when other windows (browsers, popups, file explorer)
        /// are covering the desktop at the click point.
        /// </summary>
        /// <param name="physicalX">X coordinate in physical screen pixels.</param>
        /// <param name="physicalY">Y coordinate in physical screen pixels.</param>
        /// <returns>True if the click targets the desktop layer; false if another window covers it.</returns>
        public bool IsDesktopClick(int physicalX, int physicalY)
        {
            var pt = new Win32Api.POINT { x = physicalX, y = physicalY };
            IntPtr hwndAtPoint = Win32Api.WindowFromPoint(pt);

            if (hwndAtPoint == IntPtr.Zero) return false;

            // Walk the parent chain to check if the window belongs to our desktop injection layer.
            // Our injected windows are children of either WorkerW (classic) or Progman (raised desktop).
            IntPtr current = hwndAtPoint;
            while (current != IntPtr.Zero)
            {
                // Direct match: the window IS one of our desktop containers
                if (current == _cachedWorkerW || current == _cachedProgman)
                    return true;

                IntPtr parent = Win32Api.GetParent(current);
                if (parent == IntPtr.Zero || parent == current)
                    break;
                current = parent;
            }

            // Also check if the window at point is the desktop itself (class "Progman" or "WorkerW")
            // This handles edge cases where handles may have been recycled
            var className = new System.Text.StringBuilder(256);
            Win32Api.GetClassName(hwndAtPoint, className, 256);
            string cls = className.ToString();
            if (cls == "Progman" || cls == "WorkerW")
                return true;

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
            // Set as child window style
            int style = Win32Api.GetWindowLong(windowHandle, Win32Api.GWL_STYLE);
            style = (style & ~Win32Api.WS_POPUP) | Win32Api.WS_CHILD;
            Win32Api.SetWindowLong(windowHandle, Win32Api.GWL_STYLE, style);

            // Add WS_EX_LAYERED for DWM compositing in raised desktop mode
            int exStyle = Win32Api.GetWindowLong(windowHandle, Win32Api.GWL_EXSTYLE);
            exStyle |= Win32Api.WS_EX_LAYERED;
            Win32Api.SetWindowLong(windowHandle, Win32Api.GWL_EXSTYLE, exStyle);

            // Set full opacity (required for WS_EX_LAYERED — otherwise window is invisible)
            Win32Api.SetLayeredWindowAttributes(windowHandle, 0, 255, Win32Api.LWA_ALPHA);

            // Parent to Progman (not WorkerW!)
            Win32Api.SetParent(windowHandle, _cachedProgman);

            if (asTopmost)
            {
                // For overlays (click regions, UI): place just below DefView (desktop icons).
                // SetWindowPos with hwndInsertAfter=DefView places our window right behind DefView
                // in the z-order, which means above wallpaper but below icons — exactly what we want.
                Win32Api.SetWindowPos(windowHandle, _cachedShellDefView,
                    0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
            }
            else
            {
                // For wallpaper content: z-order under DefView, above WorkerW
                if (_cachedWorkerW != IntPtr.Zero)
                {
                    // Place just above WorkerW
                    Win32Api.SetWindowPos(windowHandle, _cachedWorkerW,
                        0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
                }
                else
                {
                    Win32Api.SetWindowPos(windowHandle, Win32Api.HWND_BOTTOM,
                        0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
                }
            }
        }

        private void InjectClassic(IntPtr windowHandle, bool asTopmost)
        {
            if (_cachedWorkerW == IntPtr.Zero) return;

            Win32Api.SetParent(windowHandle, _cachedWorkerW);

            IntPtr zOrder = asTopmost ? Win32Api.HWND_TOP : Win32Api.HWND_BOTTOM;
            Win32Api.SetWindowPos(windowHandle, zOrder,
                0, 0, 0, 0, Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
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
