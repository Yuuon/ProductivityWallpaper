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
            if (nCode >= 0 && (int)wParam == 0x0201) // WM_LBUTTONDOWN
            {
                Win32Api.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<Win32Api.MSLLHOOKSTRUCT>(lParam);

                // 触发事件，询问订阅者是否处理了这个点击
                // 注意：这里为了简单，我们只通知，不拦截。
                // 如果需要严格拦截（让桌面不响应），需要订阅者返回 true，然后这里返回 (IntPtr)1

                OnMouseClick?.Invoke(new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y));

                // 在这个架构中，我们不需要“吞掉”点击，
                // 因为我们的 UI 窗口虽然在图标下，但通过这个 Hook 我们可以手动触发 UI 的 Command。
                // 让桌面图标层继续响应（比如取消选中状态）通常是无害的。
                // 如果必须拦截，这里需要改逻辑。
            }
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}