using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Low-level mouse hook service for capturing desktop mouse clicks.
    /// Uses WH_MOUSE_LL to receive click events even when windows are
    /// parented to WorkerW (which doesn't receive normal input focus).
    /// 
    /// Only handles mouse click events — sweep/gesture detection has been removed
    /// to simplify the interaction model.
    /// </summary>
    public class MouseHookService : IDisposable
    {
        public event Action<System.Windows.Point>? OnMouseClick;

        private IntPtr _hookId = IntPtr.Zero;
        private Win32Api.LowLevelMouseProc? _proc;
        private bool _disposed;

        public void Start()
        {
            if (_hookId == IntPtr.Zero)
            {
                _proc = HookCallback;
                // WH_MOUSE_LL = 14
                // For low-level hooks, hMod can be IntPtr.Zero on .NET (not required for LL hooks).
                // Using IntPtr.Zero is safer than GetModuleHandle which can return null in
                // single-file published apps where MainModule.ModuleName may be null.
                var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName;
                var moduleHandle = moduleName != null 
                    ? Win32Api.GetModuleHandle(moduleName) 
                    : IntPtr.Zero;
                _hookId = Win32Api.SetWindowsHookEx(14, _proc, moduleHandle, 0);
            }
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                Win32Api.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;

                // WM_LBUTTONDOWN (0x0201)
                if (msg == 0x0201)
                {
                    var hookStruct = Marshal.PtrToStructure<Win32Api.MSLLHOOKSTRUCT>(lParam);
                    OnMouseClick?.Invoke(new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y));
                }
            }
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

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
