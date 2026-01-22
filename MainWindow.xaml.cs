using System.Windows;
using System.Windows.Input;
using ProductivityWallpaper.Services;
using ProductivityWallpaper.ViewModels;
// 使用别名明确区分 WPF 和 WinForms 组件，避免歧义
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ProductivityWallpaper
{
    public partial class MainWindow : Window
    {
        private readonly WinForms.NotifyIcon _notifyIcon;
        private readonly LocalizationService _locService;

        public MainWindow(MainViewModel viewModel, LocalizationService locService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _locService = locService;

            // 初始化系统托盘图标
            _notifyIcon = new WinForms.NotifyIcon();

            // 使用系统默认的应用图标
            // 注意：如果在 .csproj 中未启用 <UseWindowsForms>true</UseWindowsForms>，这里会报错
            _notifyIcon.Icon = Drawing.SystemIcons.Application;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Productivity Wallpaper";

            // 处理左键点击：显示窗口
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left)
                {
                    ShowApp();
                }
            };

            // 构建右键菜单
            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Show Window", null, (s, e) => ShowApp());
            contextMenu.Items.Add("Exit", null, (s, e) => AppExit());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // 隐藏窗口到托盘，而不是关闭
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // 确认退出逻辑
            var result = System.Windows.MessageBox.Show(FindResource("Msg_ExitConfirm") as string ?? "Exit?",
                "Confirm", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                AppExit();
            }
        }

        private void ShowApp()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void AppExit()
        {
            // 务必释放图标资源，否则退出后托盘里还会残留一个幽灵图标
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
}