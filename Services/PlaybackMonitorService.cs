using System;
using System.Diagnostics;
using System.Text;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Monitors foreground window changes to detect when a fullscreen application is running.
    /// Raises events to pause/resume wallpaper playback for zero GPU usage during gaming etc.
    /// 
    /// Inspired by Lively Wallpaper's Playback system:
    /// - Uses WinEventHook (EVENT_SYSTEM_FOREGROUND) instead of polling
    /// - Checks if foreground window covers the primary screen
    /// - Notifies WallpaperService to pause/resume
    /// </summary>
    public class PlaybackMonitorService : IDisposable
    {
        /// <summary>
        /// Fired when a fullscreen app is detected (wallpaper should pause).
        /// </summary>
        public event Action? OnFullscreenAppDetected;

        /// <summary>
        /// Fired when the desktop is visible again (wallpaper should resume).
        /// </summary>
        public event Action? OnDesktopVisible;

        private const int FullscreenBorderTolerance = 2; // Pixel tolerance for borderless window detection

        private IntPtr _winEventHook = IntPtr.Zero;
        private Win32Api.WinEventDelegate? _winEventProc;
        private bool _isFullscreenActive;
        private bool _disposed;

        // Shell class names to ignore (these are desktop/taskbar windows, not real fullscreen apps)
        private static readonly string[] IgnoredClassNames = new[]
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Windows.UI.Core.CoreWindow", // Start menu, notifications
            "ApplicationFrameWindow",     // Could be UWP but also tablet mode shell
        };

        /// <summary>
        /// Starts monitoring foreground window changes.
        /// </summary>
        public void Start()
        {
            if (_winEventHook != IntPtr.Zero) return;

            _winEventProc = OnWinEvent;
            _winEventHook = Win32Api.SetWinEventHook(
                Win32Api.EVENT_SYSTEM_FOREGROUND,
                Win32Api.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventProc,
                0, 0,
                Win32Api.WINEVENT_OUTOFCONTEXT | Win32Api.WINEVENT_SKIPOWNPROCESS);

            Debug.WriteLine("[PlaybackMonitor] Started monitoring foreground windows");
        }

        /// <summary>
        /// Stops monitoring foreground window changes.
        /// </summary>
        public void Stop()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                Win32Api.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }
            _winEventProc = null;
            _isFullscreenActive = false;

            Debug.WriteLine("[PlaybackMonitor] Stopped monitoring");
        }

        private void OnWinEvent(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType != Win32Api.EVENT_SYSTEM_FOREGROUND) return;

            try
            {
                bool isFullscreen = IsWindowFullscreen(hwnd);

                if (isFullscreen && !_isFullscreenActive)
                {
                    _isFullscreenActive = true;
                    Debug.WriteLine("[PlaybackMonitor] Fullscreen app detected — pausing wallpaper");
                    OnFullscreenAppDetected?.Invoke();
                }
                else if (!isFullscreen && _isFullscreenActive)
                {
                    _isFullscreenActive = false;
                    Debug.WriteLine("[PlaybackMonitor] Desktop visible — resuming wallpaper");
                    OnDesktopVisible?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PlaybackMonitor] Error in foreground check: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the given window covers the entire primary screen.
        /// Uses physical screen dimensions (GetSystemMetrics) for accurate comparison
        /// since window rects from GetWindowRect are in physical pixels.
        /// </summary>
        private static bool IsWindowFullscreen(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            if (!Win32Api.IsWindow(hwnd)) return false;
            if (!Win32Api.IsWindowVisible(hwnd)) return false;

            // Skip shell/desktop windows
            var className = GetWindowClassName(hwnd);
            foreach (var ignored in IgnoredClassNames)
            {
                if (string.Equals(className, ignored, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Get window bounds (physical pixels)
            if (!Win32Api.GetWindowRect(hwnd, out var windowRect))
                return false;

            // Get primary screen dimensions (physical pixels)
            int screenW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int screenH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);

            // A window is considered fullscreen if it covers the entire primary screen
            // Allow a small tolerance for borderless windows that might be slightly larger
            bool coversScreen =
                windowRect.left <= 0 &&
                windowRect.top <= 0 &&
                windowRect.right >= screenW - FullscreenBorderTolerance &&
                windowRect.bottom >= screenH - FullscreenBorderTolerance;

            return coversScreen;
        }

        private static string GetWindowClassName(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            Win32Api.GetClassName(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Gets the current fullscreen state without waiting for an event.
        /// </summary>
        public bool IsCurrentlyFullscreen => _isFullscreenActive;

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
