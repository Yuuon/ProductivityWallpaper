using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace ProductivityWallpaper.Services
{
    public class MouseHookService : IDisposable
    {
        public event Action<System.Windows.Point>? OnMouseClick;
        public event Action<System.Windows.Point>? OnMouseSweep;

        // 配置属性
        public double SweepSpeedThreshold { get; set; } = 3000; // 速度阈值（像素/秒），默认 3000
        public int TrackingPointCount { get; set; } = 5; // 追踪点数，默认 5
        public int SweepCooldownMs { get; set; } = 500; // 横扫冷却期（毫秒），默认 500

        private IntPtr _hookId = IntPtr.Zero;
        private Win32Api.LowLevelMouseProc _proc;

        // 位置历史记录
        private class MousePositionRecord
        {
            public System.Windows.Point Position { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private readonly Queue<MousePositionRecord> _positionHistory = new();
        private readonly object _lockObject = new();
        private bool _isSweeping = false;
        private DateTime _lastSweepTime = DateTime.MinValue;

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

                // WM_LBUTTONDOWN (0x0201) - 保持原有逻辑
                if (msg == 0x0201)
                {
                    Win32Api.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<Win32Api.MSLLHOOKSTRUCT>(lParam);

                    // 触发事件，询问订阅者是否处理了这个点击
                    // 注意：这里为了简单，我们只通知，不拦截。
                    // 如果需要严格拦截（让桌面不响应），需要订阅者返回 true，然后这里返回 (IntPtr)1

                    OnMouseClick?.Invoke(new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y));

                    // 在这个架构中，我们不需要"吞掉"点击，
                    // 因为我们的 UI 窗口虽然在图标下，但通过这个 Hook 我们可以手动触发 UI 的 Command。
                    // 让桌面图标层继续响应（比如取消选中状态）通常是无害的。
                    // 如果必须拦截，这里需要改逻辑。
                }
                // WM_MOUSEMOVE (0x0200) - 处理鼠标移动，检测横扫
                else if (msg == 0x0200)
                {
                    Win32Api.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<Win32Api.MSLLHOOKSTRUCT>(lParam);
                    var currentPoint = new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y);

                    ProcessMouseMove(currentPoint);
                }
            }
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void ProcessMouseMove(System.Windows.Point currentPoint)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                // 检查冷却期
                if (_isSweeping)
                {
                    if ((now - _lastSweepTime).TotalMilliseconds >= SweepCooldownMs)
                    {
                        _isSweeping = false;
                    }
                    else
                    {
                        // 仍在冷却期内，只更新位置记录但不检测横扫
                        AddPositionRecord(currentPoint, now);
                        return;
                    }
                }

                // 添加新记录
                AddPositionRecord(currentPoint, now);

                // 如果记录数 >= 2，计算速度
                if (_positionHistory.Count >= 2)
                {
                    double speed = CalculateSweepSpeed();

                    // 如果速度超过阈值，触发横扫事件
                    if (speed >= SweepSpeedThreshold)
                    {
                        _isSweeping = true;
                        _lastSweepTime = now;

                        // 在锁外触发事件以避免死锁
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            OnMouseSweep?.Invoke(currentPoint);
                        });
                    }
                }
            }
        }

        private void AddPositionRecord(System.Windows.Point point, DateTime timestamp)
        {
            // 添加新记录
            _positionHistory.Enqueue(new MousePositionRecord
            {
                Position = point,
                Timestamp = timestamp
            });

            // 移除超旧记录，只保留最近的 TrackingPointCount 个
            while (_positionHistory.Count > TrackingPointCount)
            {
                _positionHistory.Dequeue();
            }
        }

        private double CalculateSweepSpeed()
        {
            // 获取最近两个点
            MousePositionRecord[] records = _positionHistory.ToArray();

            if (records.Length < 2)
            {
                return 0;
            }

            // 使用最近的两个点计算速度
            var latest = records[^1];
            var previous = records[^2];

            // 计算欧几里得距离（像素）
            double dx = latest.Position.X - previous.Position.X;
            double dy = latest.Position.Y - previous.Position.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // 计算时间差（秒）
            double timeDiffSeconds = (latest.Timestamp - previous.Timestamp).TotalSeconds;

            // 防止除以零
            if (timeDiffSeconds < 0.0001)
            {
                return 0;
            }

            // 计算速度（像素/秒）
            double speed = distance / timeDiffSeconds;

            return speed;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
