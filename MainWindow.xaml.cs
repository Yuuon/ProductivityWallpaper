using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ProductivityWallpaper.Services;
using ProductivityWallpaper.ViewModels;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ProductivityWallpaper
{
    public partial class MainWindow : Window
    {
        private readonly WinForms.NotifyIcon _notifyIcon;
        private readonly LocalizationService _locService;
        private bool _isExiting;

        public MainWindow(MainViewModel viewModel, LocalizationService locService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _locService = locService;

            // Initialize system tray icon
            _notifyIcon = new WinForms.NotifyIcon();
            _notifyIcon.Icon = Drawing.SystemIcons.Application;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Productivity Wallpaper";

            // Handle left click: show window
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left)
                {
                    ShowApp();
                }
            };

            // Build context menu
            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Show Window", null, (s, e) => ShowApp());
            contextMenu.Items.Add("Exit", null, (s, e) => AppExit());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// Intercepts the window close event (Alt+F4, taskbar close, etc.).
        /// Hides to tray instead of closing, unless the app is actually exiting.
        /// This prevents InvalidOperationException when trying to Show() a closed window.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
            base.OnClosing(e);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // Hide window to tray instead of closing
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Confirm exit logic
            var result = System.Windows.MessageBox.Show(
                FindResource("Msg_ExitConfirm") as string ?? "Exit?",
                "Confirm", 
                MessageBoxButton.YesNo);

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
            _isExiting = true;
            // Dispose icon resource to prevent ghost icon in tray
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
