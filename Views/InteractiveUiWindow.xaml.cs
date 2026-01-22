using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProductivityWallpaper.Models;
using Point = System.Windows.Point;

namespace ProductivityWallpaper.Views
{
    public partial class InteractiveUiWindow : Window
    {
        // 定义一个事件，当用户点击某个触发器时通知 ViewModel/Service
        public event Action<string> OnTriggerClicked;

        public InteractiveUiWindow()
        {
            InitializeComponent();
        }

        public void LoadConfig(InteractiveConfig config)
        {
            OverlayCanvas.Children.Clear();

            // 为了简单，我们假设窗口已经是最大化状态，直接用 ActualWidth/Height
            // 在实际使用中，需要在 SizeChanged 事件中重绘
            
            foreach (var trigger in config.Triggers)
            {
                var btn = new System.Windows.Controls.Button
                {
                    Content = trigger.ButtonText,
                    Tag = trigger.ActionVideo, // 存 Video Path 或 ID
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom(trigger.BackgroundHex),
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(trigger.TextHex),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // 设置绝对位置 (假设是百分比)
                // 注意：这里需要外部调用 SetupLayout 来确切定位
                // 我们先把数据存着
                btn.DataContext = trigger; 
                btn.Click += (s, e) => OnTriggerClicked?.Invoke(trigger.ActionVideo);

                OverlayCanvas.Children.Add(btn);
            }
        }

        public void UpdateLayout(double screenWidth, double screenHeight)
        {
            foreach (UIElement child in OverlayCanvas.Children)
            {
                if (child is System.Windows.Controls.Button btn && btn.DataContext is InteractionTrigger trigger)
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
                if (child is System.Windows.Controls.Button btn && btn.DataContext is InteractionTrigger trigger)
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
    }
}