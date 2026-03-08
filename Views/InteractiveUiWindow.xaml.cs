using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Input;
using ProductivityWallpaper.Models;
using Point = System.Windows.Point;
using Button = System.Windows.Controls.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Cursors = System.Windows.Input.Cursors;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace ProductivityWallpaper.Views
{
    public partial class InteractiveUiWindow : Window
    {
        // 定义一个事件，当用户点击某个触发器时通知 ViewModel/Service
        public event Action<string> OnTriggerClicked;

        // 悬停事件
        public event Action<InteractionTrigger>? OnTriggerHoverStart;
        public event Action? OnTriggerHoverEnd;

        // 私有字段
        private InteractionTrigger? _currentHoverTrigger;
        private System.Timers.Timer? _hoverTimer;
        private Border? _hoverBubble;
        private Dictionary<Button, InteractionTrigger> _buttonTriggerMap = new();

        public InteractiveUiWindow()
        {
            InitializeComponent();
            Closed += OnWindowClosed;
        }

        public void LoadConfig(InteractiveConfig config)
        {
            OverlayCanvas.Children.Clear();
            _buttonTriggerMap.Clear();

            // 为了简单，我们假设窗口已经是最大化状态，直接用 ActualWidth/Height
            // 在实际使用中，需要在 SizeChanged 事件中重绘
            
            foreach (var trigger in config.Triggers)
            {
                var btn = new Button
                {
                    Content = trigger.ButtonText,
                    Tag = trigger.ActionVideo, // 存 Video Path 或 ID
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom(trigger.BackgroundHex),
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(trigger.TextHex),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };

                // 设置绝对位置 (假设是百分比)
                // 注意：这里需要外部调用 SetupLayout 来确切定位
                // 我们先把数据存着
                btn.DataContext = trigger; 
                btn.Click += (s, e) => OnTriggerClicked?.Invoke(trigger.ActionVideo);

                // 添加鼠标悬停事件
                btn.MouseEnter += OnButtonMouseEnter;
                btn.MouseLeave += OnButtonMouseLeave;

                // 存入映射
                _buttonTriggerMap[btn] = trigger;

                OverlayCanvas.Children.Add(btn);
            }
        }

        public void UpdateLayout(double screenWidth, double screenHeight)
        {
            foreach (UIElement child in OverlayCanvas.Children)
            {
                if (child is Button btn && btn.DataContext is InteractionTrigger trigger)
                {
                    double x = trigger.X * screenWidth;
                    double y = trigger.Y * screenHeight;
                    double w = trigger.Width * screenWidth;
                    double h = trigger.Height * screenHeight;

                    Canvas.SetLeft(btn, x);
                    Canvas.SetTop(btn, y);
                    btn.Width = w;
                    btn.Height = h;
                }
            }
        }
        
        // 提供一个方法来手动触发点击（供 MouseHookService 调用）
        public bool SimulateClickIfHit(Point screenPoint)
        {
            // 将屏幕坐标转换为窗口内坐标
            var winPoint = this.PointFromScreen(screenPoint);

            foreach (UIElement child in OverlayCanvas.Children)
            {
                if (child is Button btn && btn.DataContext is InteractionTrigger trigger)
                {
                    // 简单的矩形碰撞检测
                    double x = Canvas.GetLeft(btn);
                    double y = Canvas.GetTop(btn);
                    
                    if (winPoint.X >= x && winPoint.X <= x + btn.Width &&
                        winPoint.Y >= y && winPoint.Y <= y + btn.Height)
                    {
                        // 命中！触发点击逻辑
                        OnTriggerClicked?.Invoke(trigger.ActionVideo);
                        return true;
                    }
                }
            }
            return false;
        }

        // 获取指定屏幕坐标下的触发器（用于 MouseHookService 判断）
        public InteractionTrigger? GetTriggerAtPoint(Point screenPoint)
        {
            var winPoint = this.PointFromScreen(screenPoint);

            foreach (UIElement child in OverlayCanvas.Children)
            {
                if (child is Button btn && btn.DataContext is InteractionTrigger trigger)
                {
                    double x = Canvas.GetLeft(btn);
                    double y = Canvas.GetTop(btn);
                    
                    if (winPoint.X >= x && winPoint.X <= x + btn.Width &&
                        winPoint.Y >= y && winPoint.Y <= y + btn.Height)
                    {
                        return trigger;
                    }
                }
            }
            return null;
        }

        #region Hover Logic

        private void OnButtonMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Button btn) return;
            if (!_buttonTriggerMap.TryGetValue(btn, out var trigger)) return;

            _currentHoverTrigger = trigger;

            // 如果已有计时器，先停止
            _hoverTimer?.Stop();
            _hoverTimer?.Dispose();

            // 创建新的计时器
            _hoverTimer = new System.Timers.Timer(trigger.HoverDelayMs);
            _hoverTimer.AutoReset = false;
            _hoverTimer.Elapsed += (_, _) =>
            {
                // 在 UI 线程执行
                Dispatcher.Invoke(() =>
                {
                    // 检查当前是否仍在悬停同一个按钮
                    if (_currentHoverTrigger == trigger)
                    {
                        ShowBubble(trigger, btn);
                        OnTriggerHoverStart?.Invoke(trigger);
                    }
                });
            };
            _hoverTimer.Start();
        }

        private void OnButtonMouseLeave(object sender, MouseEventArgs e)
        {
            // 停止计时器
            _hoverTimer?.Stop();
            _hoverTimer?.Dispose();
            _hoverTimer = null;

            // 如果有悬停的热区，触发结束事件
            if (_currentHoverTrigger != null)
            {
                _currentHoverTrigger = null;
                
                Dispatcher.Invoke(() =>
                {
                    HideBubble();
                    OnTriggerHoverEnd?.Invoke();
                });
            }
        }

        #endregion

        #region Bubble UI

        private void ShowBubble(InteractionTrigger trigger, Button btn)
        {
            HideBubble(); // 先隐藏已有的气泡

            if (string.IsNullOrEmpty(trigger.HoverText)) return;

            // 创建文本块
            var textBlock = new TextBlock
            {
                Text = trigger.HoverText,
                Foreground = Brushes.Black,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 200
            };

            // 创建气泡边框
            _hoverBubble = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)), // 半透明白色
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Child = textBlock,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 3,
                    BlurRadius = 8,
                    Opacity = 0.3
                }
            };

            // 添加到画布
            OverlayCanvas.Children.Add(_hoverBubble);

            // 计算气泡位置（显示在按钮上方）
            UpdateBubblePosition(btn);
        }

        private void UpdateBubblePosition(Button btn)
        {
            if (_hoverBubble == null) return;

            // 等待气泡渲染完成以获取实际尺寸
            _hoverBubble.UpdateLayout();

            double btnLeft = Canvas.GetLeft(btn);
            double btnTop = Canvas.GetTop(btn);
            double btnWidth = btn.Width;
            double btnHeight = btn.Height;

            double bubbleWidth = _hoverBubble.ActualWidth > 0 ? _hoverBubble.ActualWidth : 200;
            double bubbleHeight = _hoverBubble.ActualHeight > 0 ? _hoverBubble.ActualHeight : 40;

            // 气泡居中显示在按钮上方，留出 8px 间距
            double bubbleLeft = btnLeft + (btnWidth - bubbleWidth) / 2;
            double bubbleTop = btnTop - bubbleHeight - 8;

            // 边界检查：如果上方空间不足，显示在按钮下方
            if (bubbleTop < 0)
            {
                bubbleTop = btnTop + btnHeight + 8;
            }

            // 边界检查：确保不超出左右边界
            double canvasWidth = OverlayCanvas.ActualWidth;
            if (bubbleLeft < 0) bubbleLeft = 8;
            if (bubbleLeft + bubbleWidth > canvasWidth) bubbleLeft = canvasWidth - bubbleWidth - 8;

            Canvas.SetLeft(_hoverBubble, bubbleLeft);
            Canvas.SetTop(_hoverBubble, bubbleTop);
        }

        private void HideBubble()
        {
            if (_hoverBubble != null)
            {
                OverlayCanvas.Children.Remove(_hoverBubble);
                _hoverBubble = null;
            }
        }

        #endregion

        #region Helper Methods

        private Button? GetButtonFromTrigger(InteractionTrigger trigger)
        {
            foreach (var pair in _buttonTriggerMap)
            {
                if (pair.Value == trigger)
                {
                    return pair.Key;
                }
            }
            return null;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            // 清理计时器
            _hoverTimer?.Stop();
            _hoverTimer?.Dispose();
            _hoverTimer = null;
        }

        #endregion
    }
}
