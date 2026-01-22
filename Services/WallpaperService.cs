using Size = System.Drawing.Size;
using Application = System.Windows.Application;
using MediaType = ProductivityWallpaper.Models.MediaType;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ProductivityWallpaper.Models;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;
using ProductivityWallpaper.Views;
using LibVLCSharp.Shared; // 需要引用以获取视频时长
using System.Windows.Media.Animation; // 引用动画库

namespace ProductivityWallpaper.Services
{
    public class WallpaperService
    {
        private const string MetaFileName = ".wp_meta.json";
        private const string InteractiveConfigName = "config.wallpaper";

        // 窗口实例管理
        private VideoPlayerWindow? _idleVideoWindow;
        private VideoPlayerWindow? _actionVideoWindow;
        private InteractiveUiWindow? _currentUiWindow;
        private MouseHookService? _mouseHook;

        private LibVLC? _tempLibVLC;
        private MediaItem? _currentInteractiveItem;

        public WallpaperService()
        {
            try { _tempLibVLC = new LibVLC(); } catch { }
        }

        // --- 核心扫描逻辑 ---
        public async Task<List<MediaItem>> RefreshMetadataAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                var items = new List<MediaItem>();
                if (!Directory.Exists(folderPath)) return items;

                var cacheDir = Path.Combine(folderPath, ".wp_cache");
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                // 1. Interactive Package
                var subDirs = Directory.GetDirectories(folderPath);
                foreach (var dir in subDirs)
                {
                    var configPath = Path.Combine(dir, InteractiveConfigName);
                    if (File.Exists(configPath))
                    {
                        var item = new MediaItem
                        {
                            FilePath = dir,
                            Type = MediaType.Interactive
                        };

                        try
                        {
                            var json = File.ReadAllText(configPath);
                            var config = JsonSerializer.Deserialize<InteractiveConfig>(json);
                            item.InteractiveConfig = config;

                            if (config != null && !string.IsNullOrEmpty(config.IdleVideo))
                            {
                                var idleVideoPath = Path.Combine(dir, config.IdleVideo);
                                if (File.Exists(idleVideoPath))
                                {
                                    string thumbName = new DirectoryInfo(dir).Name + "_interactive_thumb.jpg";
                                    string thumbPath = Path.Combine(cacheDir, thumbName);

                                    if (File.Exists(thumbPath)) item.ThumbnailPath = thumbPath;
                                    else item.ThumbnailPath = GenerateThumbnailForFile(idleVideoPath, thumbPath);
                                }
                            }
                        }
                        catch { }
                        items.Add(item);
                    }
                }

                // 2. Normal files
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file).ToLower();
                    var item = new MediaItem { FilePath = file };
                    bool isValid = false;

                    if (ext == ".jpg" || ext == ".png" || ext == ".bmp")
                    {
                        item.Type = MediaType.Image;
                        isValid = true;
                        item.ThumbnailPath = GenerateImageThumbnail(file, cacheDir);
                    }
                    else if (ext == ".mp4" || ext == ".webm" || ext == ".mkv")
                    {
                        item.Type = MediaType.Video;
                        isValid = true;
                        item.ThumbnailPath = GenerateVideoThumbnail(file, cacheDir);
                    }

                    if (isValid) items.Add(item);
                }

                var metadata = new LibraryMetadata
                {
                    FolderPath = folderPath,
                    LastUpdated = DateTime.Now,
                    Items = items
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(Path.Combine(folderPath, MetaFileName), JsonSerializer.Serialize(metadata, options));

                return items;
            });
        }

        // --- 应用互动壁纸逻辑 ---
        public void ApplyInteractiveWallpaper(MediaItem item, int monitorIndex)
        {
            CleanupCurrentWallpaper();
            _currentInteractiveItem = item;

            var configPath = Path.Combine(item.FilePath, InteractiveConfigName);
            if (!File.Exists(configPath)) return;

            InteractiveConfig config;
            try
            {
                config = JsonSerializer.Deserialize<InteractiveConfig>(File.ReadAllText(configPath)) ?? new InteractiveConfig();
            }
            catch { return; }

            var videoPath = Path.Combine(item.FilePath, config.IdleVideo);
            if (!File.Exists(videoPath)) return;

            // 启动 Idle 窗口 (初始透明)
            _idleVideoWindow = CreateHiddenVideoWindow(videoPath);
            _idleVideoWindow.Show();

            // 启动 UI 窗口
            _currentUiWindow = new InteractiveUiWindow();
            _currentUiWindow.Opacity = 0;
            _currentUiWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            _currentUiWindow.Left = -32000;
            _currentUiWindow.Top = -32000;
            _currentUiWindow.LoadConfig(config);
            _currentUiWindow.Show();

            _currentUiWindow.OnTriggerClicked += (actionVideoName) =>
            {
                var actionPath = Path.Combine(item.FilePath, actionVideoName);
                if (File.Exists(actionPath))
                {
                    PlayActionVideo(actionPath);
                }
            };

            // 注入
            InjectInteractiveLayers(_idleVideoWindow, _currentUiWindow);

            // 使用淡入效果显示初始壁纸 (更平滑)
            FadeWindowAsync(_idleVideoWindow, 0, 1, 500);
            FadeWindowAsync(_currentUiWindow, 0, 1, 500);

            // 启动 Hook
            _mouseHook = new MouseHookService();
            _mouseHook.OnMouseClick += (screenPoint) =>
            {
                if (_currentUiWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _currentUiWindow.SimulateClickIfHit(screenPoint);
                    });
                }
            };
            _mouseHook.Start();
        }

        // --- 播放动作视频 (核心优化：淡入淡出 + 缓冲) ---
        private async void PlayActionVideo(string videoPath)
        {
            if (_actionVideoWindow != null) return; // 简单防抖，防止连点

            // 1. 获取时长
            long durationMs = 3000;
            if (_tempLibVLC != null)
            {
                try
                {
                    using (var media = new Media(_tempLibVLC, new Uri(videoPath)))
                    {
                        await media.Parse(MediaParseOptions.ParseLocal);
                        if (media.Duration > 0) durationMs = media.Duration;
                    }
                }
                catch { }
            }

            // 2. 创建 Action 窗口 (完全透明)
            _actionVideoWindow = CreateHiddenVideoWindow(videoPath);
            _actionVideoWindow.Show();

            // 3. 注入层级：Action 盖在 Idle 之上
            InjectActionLayer(_actionVideoWindow, _idleVideoWindow, _currentUiWindow);

            // 4. [缓冲] 给 VLC 一点时间渲染第一帧，避免淡入时看到黑框
            // 虽然不精准，但在本机播放 200ms 通常足够
            await Task.Delay(200);

            // 5. [淡入] Action Window 从透明变实 (300ms)
            // 此时 Idle 还在下面播放，用户看到的是混合渐变
            await FadeWindowAsync(_actionVideoWindow, 0, 1, 300);

            // --- 此时 Action 已经完全覆盖了 Idle ---

            // 6. [后台重置 Idle] 
            // 既然看不见 Idle 了，我们趁机销毁旧的，创建一个新的并从头播放
            // 这样等 Action 消失时，Idle 刚好是从第一帧开始的
            ResetIdleVideoInBackground();

            // 7. [等待播放] 减去前面的耗时
            // 缓冲200ms + 淡入300ms = 500ms 已过
            int remainingTime = (int)durationMs - 500 - 300; // 再预留 300ms 给淡出
            if (remainingTime > 0) await Task.Delay(remainingTime);

            // 8. [淡出] Action Window 变透明，露出底下的新 Idle (300ms)
            await FadeWindowAsync(_actionVideoWindow, 1, 0, 300);

            // 9. [清理]
            if (_actionVideoWindow != null)
            {
                _actionVideoWindow.Visibility = Visibility.Hidden;
                try { _actionVideoWindow.StopAndClose(); } catch { }
                _actionVideoWindow = null;
            }
        }

        private void ResetIdleVideoInBackground()
        {
            if (_currentInteractiveItem?.InteractiveConfig != null)
            {
                var idlePath = Path.Combine(_currentInteractiveItem.FilePath, _currentInteractiveItem.InteractiveConfig.IdleVideo);
                if (File.Exists(idlePath))
                {
                    // 创建新 Idle (透明)
                    var newIdleWindow = CreateHiddenVideoWindow(idlePath);
                    newIdleWindow.Show();

                    // 挂载到最底层
                    InjectIdleLayer(newIdleWindow);

                    // 因为在 Action 之下，直接设为可见即可，无需动画
                    // 但为了保险（防止层级偶尔错乱导致的闪烁），也可以淡入或者延时设为1
                    newIdleWindow.Opacity = 1;

                    // 销毁旧 Idle
                    if (_idleVideoWindow != null)
                    {
                        try { _idleVideoWindow.StopAndClose(); } catch { }
                    }

                    _idleVideoWindow = newIdleWindow;
                }
            }
        }

        // --- 动画辅助方法 ---
        private Task FadeWindowAsync(Window? window, double from, double to, int durationMs)
        {
            if (window == null) return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();

            // 必须在 UI 线程执行动画
            Application.Current.Dispatcher.Invoke(() =>
            {
                var anim = new DoubleAnimation
                {
                    From = from,
                    To = to,
                    Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                    FillBehavior = FillBehavior.HoldEnd
                };

                anim.Completed += (s, e) =>
                {
                    window.Opacity = to; // 确保动画结束后属性值正确固定
                    tcs.SetResult(true);
                };

                window.BeginAnimation(Window.OpacityProperty, anim);
            });

            return tcs.Task;
        }

        private VideoPlayerWindow CreateHiddenVideoWindow(string path)
        {
            var win = new VideoPlayerWindow(path);
            win.Opacity = 0; // 关键：初始全透
            win.ShowInTaskbar = false;
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Left = -32000;
            win.Top = -32000;
            return win;
        }

        public void ApplyVideoWallpaper(string path, int monitorIndex)
        {
            CleanupCurrentWallpaper();
            var videoWin = CreateHiddenVideoWindow(path);
            videoWin.Show();
            InjectDynamicWallpaper(videoWin);
            // 单视频应用也可以加个淡入，体验更好
            FadeWindowAsync(videoWin, 0, 1, 500);
            _idleVideoWindow = videoWin;
        }

        public void SetStaticWallpaper(string path, int monitorIndex = -1)
        {
            CleanupCurrentWallpaper();
            Win32Api.SystemParametersInfo(Win32Api.SPI_SETDESKWALLPAPER, 0, path, Win32Api.SPIF_UPDATEINIFILE | Win32Api.SPIF_SENDCHANGE);
        }

        public void SetAutoColorization(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null) key.SetValue("AutoColorization", enabled ? 1 : 0, RegistryValueKind.DWord);
                }
                Win32Api.SendMessageTimeout(new IntPtr(0xffff), 0x001A, UIntPtr.Zero, IntPtr.Zero, 0x0, 1000, out _);
            }
            catch { }
        }

        private void CleanupCurrentWallpaper()
        {
            _mouseHook?.Stop();
            _mouseHook = null;
            _currentInteractiveItem = null;

            if (_currentUiWindow != null)
            {
                _currentUiWindow.Close();
                _currentUiWindow = null;
            }

            if (_actionVideoWindow != null)
            {
                try { _actionVideoWindow.StopAndClose(); } catch { }
                _actionVideoWindow = null;
            }

            if (_idleVideoWindow != null)
            {
                try { _idleVideoWindow.StopAndClose(); } catch { }
                _idleVideoWindow = null;
            }
        }

        // --- 注入与层级控制 ---

        private void InjectDynamicWallpaper(Window playerWindow)
        {
            var helper = new WindowInteropHelper(playerWindow);
            IntPtr workerw = FindWorkerW();
            if (workerw == IntPtr.Zero) return;

            Win32Api.SetParent(helper.Handle, workerw);
            RemoveBorderAndSetTransparent(helper.Handle);
            playerWindow.WindowState = WindowState.Maximized;
        }

        private void InjectIdleLayer(Window idleWin)
        {
            var idleHelper = new WindowInteropHelper(idleWin);
            IntPtr workerw = FindWorkerW();
            if (workerw == IntPtr.Zero) return;

            Win32Api.SetParent(idleHelper.Handle, workerw);
            RemoveBorderAndSetTransparent(idleHelper.Handle);
            Win32Api.SetWindowPos(idleHelper.Handle, Win32Api.HWND_BOTTOM, 0, 0, 0, 0, 0x0013);
            idleWin.WindowState = WindowState.Maximized;
        }

        private void InjectInteractiveLayers(Window idleWin, InteractiveUiWindow uiWin)
        {
            InjectIdleLayer(idleWin);

            var uiHelper = new WindowInteropHelper(uiWin);
            IntPtr workerw = FindWorkerW();

            Win32Api.SetParent(uiHelper.Handle, workerw);

            int style = Win32Api.GetWindowLong(uiHelper.Handle, Win32Api.GWL_STYLE);
            style = style & ~Win32Api.WS_POPUP & ~Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(uiHelper.Handle, Win32Api.GWL_STYLE, style);

            uiWin.WindowState = WindowState.Maximized;
            Win32Api.SetWindowPos(uiHelper.Handle, Win32Api.HWND_TOP, 0, 0, 0, 0, 0x0013);
            uiWin.UpdateLayout(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        private void InjectActionLayer(Window actionWin, Window? idleWin, InteractiveUiWindow? uiWin)
        {
            var actionHelper = new WindowInteropHelper(actionWin);
            IntPtr workerw = FindWorkerW();
            if (workerw == IntPtr.Zero) return;

            Win32Api.SetParent(actionHelper.Handle, workerw);
            RemoveBorderAndSetTransparent(actionHelper.Handle);
            actionWin.WindowState = WindowState.Maximized;

            // Z-Order: UI > Action > Idle
            Win32Api.SetWindowPos(actionHelper.Handle, Win32Api.HWND_TOP, 0, 0, 0, 0, 0x0013);

            if (uiWin != null)
            {
                var uiHandle = new WindowInteropHelper(uiWin).Handle;
                Win32Api.SetWindowPos(uiHandle, Win32Api.HWND_TOP, 0, 0, 0, 0, 0x0013);
            }
        }

        private IntPtr FindWorkerW()
        {
            IntPtr progman = Win32Api.FindWindow("Progman", null);
            Win32Api.SendMessageTimeout(progman, 0x052C, UIntPtr.Zero, IntPtr.Zero, 0x0, 1000, out _);

            IntPtr workerw = IntPtr.Zero;
            Win32Api.EnumWindows((hwnd, lParam) =>
            {
                if (Win32Api.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                    workerw = Win32Api.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                return true;
            }, IntPtr.Zero);

            return workerw;
        }

        private void RemoveBorderAndSetTransparent(IntPtr hwnd)
        {
            int style = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_STYLE);
            int exStyle = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_EXSTYLE);
            style = style & ~Win32Api.WS_POPUP & ~Win32Api.WS_VISIBLE;
            exStyle = exStyle | Win32Api.WS_EX_TRANSPARENT | Win32Api.WS_EX_LAYERED | Win32Api.WS_EX_TOOLWINDOW;
            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_STYLE, style);
            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_EXSTYLE, exStyle);
        }

        // ... TryLoadMetadata, GenerateThumbnail ... (保持原样)
        public List<MediaItem> TryLoadMetadata(string folderPath)
        {
            var metaPath = Path.Combine(folderPath, MetaFileName);
            if (File.Exists(metaPath))
            {
                try
                {
                    var json = File.ReadAllText(metaPath);
                    var metadata = JsonSerializer.Deserialize<LibraryMetadata>(json);
                    return metadata?.Items ?? new List<MediaItem>();
                }
                catch { }
            }
            return new List<MediaItem>();
        }

        private string GenerateImageThumbnail(string srcPath, string cacheDir)
        {
            string thumbName = Path.GetFileNameWithoutExtension(srcPath) + "_thumb.jpg";
            string thumbPath = Path.Combine(cacheDir, thumbName);
            return GenerateThumbnailForFile(srcPath, thumbPath, true);
        }

        private string GenerateVideoThumbnail(string srcPath, string cacheDir)
        {
            string thumbName = Path.GetFileNameWithoutExtension(srcPath) + "_vthumb.jpg";
            string thumbPath = Path.Combine(cacheDir, thumbName);
            return GenerateThumbnailForFile(srcPath, thumbPath, false);
        }

        private string GenerateThumbnailForFile(string srcPath, string destPath, bool isImage = false)
        {
            try
            {
                if (File.Exists(destPath)) return destPath;

                if (isImage)
                {
                    using (var img = System.Drawing.Image.FromFile(srcPath))
                    {
                        var ratio = (double)200 / img.Width;
                        var newHeight = (int)(img.Height * ratio);
                        using (var thumb = new Bitmap(img, new Size(200, newHeight)))
                        {
                            thumb.Save(destPath, ImageFormat.Jpeg);
                        }
                    }
                    return destPath;
                }
                else
                {
                    Guid shellItemGuid = typeof(Win32Api.IShellItem).GUID;
                    Win32Api.IShellItem shellItem;
                    int hr = Win32Api.SHCreateItemFromParsingName(srcPath, IntPtr.Zero, ref shellItemGuid, out shellItem);
                    if (hr == 0 && shellItem is Win32Api.IShellItemImageFactory imageFactory)
                    {
                        IntPtr hBitmap;
                        var size = new Win32Api.SIZE(320, 180);
                        imageFactory.GetImage(size, Win32Api.SIIGBF.SIIGBF_THUMBNAILONLY | Win32Api.SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                        if (hBitmap != IntPtr.Zero)
                        {
                            using (var bmp = System.Drawing.Image.FromHbitmap(hBitmap))
                                bmp.Save(destPath, ImageFormat.Jpeg);
                            return destPath;
                        }
                    }
                }
                return "";
            }
            catch { return ""; }
        }
    }
}