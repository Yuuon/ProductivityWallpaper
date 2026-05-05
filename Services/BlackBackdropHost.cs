using System;
using static ProductivityWallpaper.Services.Win32Api;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Hosts a native Win32 child HWND inside a parent WPF window that paints a solid
    /// black rectangle covering the parent's client area.
    ///
    /// <para>
    /// <b>Why this exists:</b> On Win11 24H2 raised desktop, Progman owns
    /// <c>WS_EX_NOREDIRECTIONBITMAP</c>. WPF content of a window reparented under Progman
    /// is not reliably composited by DWM, so any pixels NOT painted by a native producer
    /// (e.g. VLC's child HWND) fall through to the OS desktop wallpaper instead of
    /// showing the WPF-defined <c>Background="Black"</c>.
    /// </para>
    ///
    /// <para>
    /// This class creates a real Win32 child HWND that paints solid black via GDI
    /// <c>FillRect(BLACK_BRUSH)</c> in <c>WM_PAINT</c>. Sized to fill the parent's client
    /// area and z-ordered to <c>HWND_BOTTOM</c>, it provides a guaranteed opaque backdrop
    /// for any sibling child HWNDs that paint above it (notably the LibVLCSharp VideoView
    /// child HWND in Center display mode where VLC letterboxes inside the surface).
    /// </para>
    ///
    /// <para>
    /// Lifetime: <see cref="Attach"/> creates the child HWND; <see cref="Resize"/> updates
    /// its size and reasserts bottom Z-order; <see cref="Dispose"/> destroys it.
    /// The window class is registered once per process.
    /// </para>
    /// </summary>
    public sealed class BlackBackdropHost : IDisposable
    {
        private const string ClassName = "ProductivityWallpaper.BlackBackdrop";

        // The WndProc must be kept alive — native code calls back into this delegate.
        // GC must not collect it. Hold it in a static field for process lifetime.
        private static readonly WndProcDelegate s_wndProc = StaticWndProc;

        private static readonly Lazy<bool> s_classRegistered = new(RegisterWindowClass);

        private IntPtr _backdropHwnd;
        private IntPtr _parentHwnd;
        private bool _disposed;

        public IntPtr Handle => _backdropHwnd;

        /// <summary>
        /// Creates and attaches the backdrop child HWND under <paramref name="parentHwnd"/>.
        /// Sized to the parent's current client rect.
        /// </summary>
        public bool Attach(IntPtr parentHwnd)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BlackBackdropHost));
            if (parentHwnd == IntPtr.Zero) return false;
            if (_backdropHwnd != IntPtr.Zero) return true; // already attached

            // Ensure window class is registered once per process.
            if (!s_classRegistered.Value) return false;

            _parentHwnd = parentHwnd;

            if (!GetClientRect(parentHwnd, out var rc)) return false;
            int width = rc.right - rc.left;
            int height = rc.bottom - rc.top;
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            IntPtr hInstance = GetModuleHandle(null);

            _backdropHwnd = CreateWindowEx(
                WS_EX_NOPARENTNOTIFY,
                ClassName,
                null,
                WS_CHILD | WS_VISIBLE | WS_DISABLED | WS_CLIPSIBLINGS,
                0, 0, width, height,
                parentHwnd,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);

            if (_backdropHwnd == IntPtr.Zero) return false;

            // Push to bottom of the parent's child-window Z-order so any sibling
            // (notably VLC's VideoView HwndHost child) paints above us.
            SetWindowPos(
                _backdropHwnd,
                HWND_BOTTOM,
                0, 0, width, height,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);

            InvalidateRect(_backdropHwnd, IntPtr.Zero, false);
            return true;
        }

        /// <summary>
        /// Resizes the backdrop to fill the parent's current client area and reasserts
        /// <c>HWND_BOTTOM</c> z-order so siblings created later (VLC HwndHost) end up above.
        /// Safe to call repeatedly (e.g. from <c>SizeChanged</c>, after VLC initializes,
        /// after the wallpaper window is restored from offscreen anti-flicker init).
        /// </summary>
        public void Resize()
        {
            if (_disposed || _backdropHwnd == IntPtr.Zero || _parentHwnd == IntPtr.Zero) return;
            if (!GetClientRect(_parentHwnd, out var rc)) return;
            int width = rc.right - rc.left;
            int height = rc.bottom - rc.top;
            if (width <= 0 || height <= 0) return;

            SetWindowPos(
                _backdropHwnd,
                HWND_BOTTOM,
                0, 0, width, height,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);

            InvalidateRect(_backdropHwnd, IntPtr.Zero, false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_backdropHwnd != IntPtr.Zero && IsWindow(_backdropHwnd))
                {
                    DestroyWindow(_backdropHwnd);
                }
            }
            catch { }
            _backdropHwnd = IntPtr.Zero;
            _parentHwnd = IntPtr.Zero;
        }

        // --- Static window class plumbing ---

        private static bool RegisterWindowClass()
        {
            try
            {
                var wc = new WNDCLASSEX
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<WNDCLASSEX>(),
                    style = 0,
                    lpfnWndProc = s_wndProc,
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hInstance = GetModuleHandle(null),
                    hIcon = IntPtr.Zero,
                    hCursor = IntPtr.Zero,
                    hbrBackground = GetStockObject(BLACK_BRUSH), // belt + suspenders: erase to black
                    lpszMenuName = null,
                    lpszClassName = ClassName,
                    hIconSm = IntPtr.Zero,
                };

                ushort atom = RegisterClassEx(ref wc);
                // ERROR_CLASS_ALREADY_EXISTS (1410) is fine — class is per-process.
                if (atom == 0)
                {
                    int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    if (err != 1410) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IntPtr StaticWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_ERASEBKGND:
                    // We paint the entire client in WM_PAINT; suppress default erase to
                    // avoid a flash of a different brush color on resize.
                    return new IntPtr(1);

                case WM_PAINT:
                    {
                        IntPtr hdc = BeginPaint(hWnd, out var ps);
                        try
                        {
                            if (GetClientRect(hWnd, out var rc))
                            {
                                FillRect(hdc, ref rc, GetStockObject(BLACK_BRUSH));
                            }
                        }
                        finally
                        {
                            EndPaint(hWnd, ref ps);
                        }
                        return IntPtr.Zero;
                    }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
