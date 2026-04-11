using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace ProductivityWallpaper.Services
{
    public class MouseHookService : IDisposable
    {
        public event Action<System.Windows.Point>? OnMouseClick;

        private IntPtr _hookId = IntPtr.Zero;
        private Win32Api.LowLevelMouseProc _proc;

        public void Start()
        {
            if (_hookId == IntPtr.Zero)
            {
                _proc = HookCallback;
                // WH_MOUSE_LL = 14
                _hookId = Win32Api.SetWindowsHookEx(14, _proc, Win32Api.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
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

                // WM_LBUTTONDOWN (0x0201) — only process left-clicks
                if (msg == 0x0201)
                {
                    Win32Api.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<Win32Api.MSLLHOOKSTRUCT>(lParam);
                    OnMouseClick?.Invoke(new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y));
                }
                // All other messages (WM_MOUSEMOVE, etc.) fall through immediately
                // to CallNextHookEx with zero processing — minimizing hook overhead.
            }
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
