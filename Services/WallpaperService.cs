using Size = System.Drawing.Size;
using Application = System.Windows.Application;
using MediaType = ProductivityWallpaper.Models.MediaType;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ProductivityWallpaper.Models;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;
using ProductivityWallpaper.Views;
using LibVLCSharp.Shared;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace ProductivityWallpaper.Services
{
    public class WallpaperService
    {
        private const string MetaFileName = ".wp_meta.json";
        private const string InteractiveConfigName = "config.wallpaper";

        // --- Legacy window management ---
        private VideoPlayerWindow? _idleVideoWindow;
        private VideoPlayerWindow? _actionVideoWindow;
        private InteractiveUiWindow? _currentUiWindow;
        private MouseHookService? _mouseHook;

        private LibVLC? _tempLibVLC;
        private MediaPlayer? _audioPlayer;
        private MediaPlayer? _bgAudioPlayer;
        private MediaItem? _currentInteractiveItem;
        private InteractiveConfig? _currentConfig;

        // --- Theme-based dynamic wallpaper state ---
        private Window? _currentBackgroundWindow;  // Either VideoPlayerWindow or ImagePlayerWindow
        private DispatcherTimer? _wallpaperCycleTimer;
        private List<MediaItemModel> _wallpaperPlaylist = new();
        private List<MediaItemModel> _audioPlaylist = new();
        private int _currentWallpaperIndex = -1;
        private int _currentAudioIndex = -1;
        private int _wallpaperDurationSeconds = 30;
        private PlaybackMode _wallpaperPlaybackMode = PlaybackMode.Sequential;
        private PlaybackMode _audioPlaybackMode = PlaybackMode.Sequential;
        private bool _isDynamicWallpaperActive;
        private readonly Random _random = new();

        // Click region state for theme-based mode
        private List<ClickRegionModel>? _activeClickRegions;
        private ResourceResolver? _activeResolver;
        private SchemeModel? _activeMouseClickScheme;
        private bool _bgAudioEndReachedSubscribed;
        private readonly Dictionary<string, int> _regionAudioIndex = new();
        private const int MinWallpaperDurationSeconds = 5;

        public WallpaperService()
        {
            try 
            { 
                _tempLibVLC = new LibVLC();
                _audioPlayer = new MediaPlayer(_tempLibVLC);
                _bgAudioPlayer = new MediaPlayer(_tempLibVLC);
            } 
            catch { }
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
                            Type = MediaType_Old.Interactive
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
                        item.Type = MediaType_Old.Image;
                        isValid = true;
                        item.ThumbnailPath = GenerateImageThumbnail(file, cacheDir);
                    }
                    else if (ext == ".mp4" || ext == ".webm" || ext == ".mkv")
                    {
                        item.Type = MediaType_Old.Video;
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

            _currentConfig = config;
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

            // 点击热区 - 播放对应的视频和音频
            _currentUiWindow.OnTriggerClicked += (actionVideoName) =>
            {
                var actionPath = Path.Combine(item.FilePath, actionVideoName);
                if (File.Exists(actionPath))
                {
                    PlayActionVideo(actionPath);
                }
            };

            // 悬停开始 - 可以在这里添加日志或其他逻辑
            _currentUiWindow.OnTriggerHoverStart += (trigger) =>
            {
                // 悬停提示已在 UI 层处理，这里可以添加额外的逻辑
                // 例如：播放悬停音效、记录用户行为等
                System.Diagnostics.Debug.WriteLine($"Hover started on {trigger.Type}: {trigger.HoverText}");
            };

            // 悬停结束
            _currentUiWindow.OnTriggerHoverEnd += () =>
            {
                System.Diagnostics.Debug.WriteLine("Hover ended");
            };

            // 注入
            InjectInteractiveLayers(_idleVideoWindow, _currentUiWindow);

            // 使用淡入效果显示初始壁纸 (更平滑)
            FadeWindowAsync(_idleVideoWindow, 0, 1, 500);
            FadeWindowAsync(_currentUiWindow, 0, 1, 500);

            // 启动 Hook
            _mouseHook = new MouseHookService();
            
            // 设置横扫速度阈值（如果配置中有）
            if (config.SweepSpeedThreshold > 0)
            {
                _mouseHook.SweepSpeedThreshold = config.SweepSpeedThreshold;
            }

            // 鼠标点击事件
            _mouseHook.OnMouseClick += (screenPoint) =>
            {
                if (_currentUiWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var trigger = _currentUiWindow.GetTriggerAtPoint(screenPoint);
                        if (trigger != null)
                        {
                            // 播放音频（如果有配置）
                            if (!string.IsNullOrEmpty(trigger.Audio))
                            {
                                var audioPath = Path.Combine(item.FilePath, trigger.Audio);
                                PlayAudio(audioPath);
                            }
                            _currentUiWindow.SimulateClickIfHit(screenPoint);
                        }
                    });
                }
            };

            // 横扫检测事件
            _mouseHook.OnMouseSweep += (screenPoint) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 如果正在播放 Action 视频，则忽略横扫
                    if (_actionVideoWindow != null) return;

                    // 如果配置了横扫视频，则播放
                    if (!string.IsNullOrEmpty(config.SweepActionVideo))
                    {
                        var sweepPath = Path.Combine(item.FilePath, config.SweepActionVideo);
                        if (File.Exists(sweepPath))
                        {
                            System.Diagnostics.Debug.WriteLine("Mouse sweep detected! Playing sweep video.");
                            PlayActionVideo(sweepPath);
                        }
                    }
                });
            };

            _mouseHook.Start();
        }

        // --- 播放音频 ---
        private void PlayAudio(string audioPath)
        {
            if (_audioPlayer == null || !File.Exists(audioPath)) return;

            try
            {
                // Do NOT use 'using' — VLC playback is async and needs the Media alive
                var media = new Media(_tempLibVLC, new Uri(audioPath));
                _audioPlayer.Play(media);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play audio: {ex.Message}");
            }
        }

        // --- 播放动作视频 (核心优化：淡入淡出 + 缓冲) ---
        private async void PlayActionVideo(string videoPath)
        {
            if (_actionVideoWindow != null) return;

            // 1. 获取时长 (保持不变)
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

            // 1. 创建窗口 (此时它是 1x1 大小，位于 -32000)
            _actionVideoWindow = CreateHiddenVideoWindow(videoPath);

            // 2. 显示窗口
            // 这一步是为了让 HwndHost 初始化。
            // 因为它是 1x1 像素且在屏幕外，用户完全看不到任何“弹窗”或“闪烁”。
            _actionVideoWindow.Show();

            // 3. 挂载到桌面
            // 此时窗口变成了 WorkerW 的子窗口，但它仍然是 1x1 像素
            InjectActionLayer(_actionVideoWindow, _idleVideoWindow, _currentUiWindow);

            // 4. [缓冲等待]
            // 此时 VLC 开始加载视频。我们在它还是 1x1 的时候等待一小会儿。
            // 防止拉大后先显示黑屏再出画面。
            await Task.Delay(100);

            // 5. [瞬间展开] 手动将窗口设置为全屏尺寸
            var helper = new WindowInteropHelper(_actionVideoWindow);
            int screenW = (int)SystemParameters.PrimaryScreenWidth;
            int screenH = (int)SystemParameters.PrimaryScreenHeight;

            // 使用 SetWindowPos 瞬间拉伸，跳过“最大化”动画
            // 参数说明: HWND_TOP, x=0, y=0, w=ScreenW, h=ScreenH, NOACTIVATE
            Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP, 0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);

            // 8. 后台重置 Idle (保持不变)
            ResetIdleVideoInBackground();

            // 9. 等待播放结束
            int remainingTime = (int)durationMs - 500 - 300;
            if (remainingTime > 0) await Task.Delay(remainingTime);

            // 10. 淡出并关闭
            await FadeWindowAsync(_actionVideoWindow, 1, 0, 300);

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

            // 1. 将窗口移动到屏幕外
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Left = -32000;
            win.Top = -32000;

            // 2. [核心修复] 将尺寸设置为 1x1 像素
            // 这样，当调用 Show() 时，它虽然是可见的，但只是一个不可见的像素点
            win.Width = 1;
            win.Height = 1;

            // 3. 去除边框和任务栏图标
            win.WindowStyle = WindowStyle.None;
            win.ResizeMode = ResizeMode.NoResize;
            win.ShowInTaskbar = false;

            // 注意：不要设置 AllowsTransparency = true，这会导致 HwndHost 不显示
            return win;
        }

        private void EnableLayeredWindow(Window window)
        {
            var helper = new WindowInteropHelper(window);
            helper.EnsureHandle(); // 关键：强制创建句柄，但不显示窗口

            IntPtr hwnd = helper.Handle;
            int exStyle = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_EXSTYLE);

            // 添加 WS_EX_LAYERED (0x80000) 和 WS_EX_TRANSPARENT (0x20)
            // 这样 WPF 的 Opacity 属性就能在 AllowsTransparency="False" 的情况下生效了
            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_EXSTYLE, exStyle | Win32Api.WS_EX_LAYERED | Win32Api.WS_EX_TRANSPARENT);
        }

        public void ApplyVideoWallpaper(string path, int monitorIndex)
        {
            CleanupCurrentWallpaper();
            var videoWin = CreateHiddenVideoWindow(path);
            videoWin.Show();
            InjectDynamicWallpaper(videoWin);
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

        // ==================== Theme-Based Dynamic Wallpaper ====================

        /// <summary>
        /// Applies a theme-based dynamic wallpaper using the active desktop background scheme.
        /// Cycles through images and videos from the scheme's media list in mixed order,
        /// with configurable duration per wallpaper. Also handles audio playback and click regions.
        /// </summary>
        /// <param name="manifest">The theme manifest containing schemes and resources.</param>
        /// <param name="settings">Wallpaper playback settings (duration, audio, volume).</param>
        public void ApplyThemeWallpaper(ThemeManifest manifest, WallpaperPlaybackSettings? settings = null)
        {
            CleanupCurrentWallpaper();

            settings ??= new WallpaperPlaybackSettings();
            _wallpaperDurationSeconds = settings.WallpaperDurationSeconds;

            var themeRootPath = GetThemeRootPath(manifest);
            var resolver = new ResourceResolver(manifest.ResourceLibrary, themeRootPath);
            _activeResolver = resolver;

            // Find the active desktop background scheme
            var bgScheme = manifest.DesktopBackgroundSchemes
                .FirstOrDefault(s => s.IsActive)
                ?? manifest.DesktopBackgroundSchemes.FirstOrDefault();

            if (bgScheme == null)
            {
                System.Diagnostics.Debug.WriteLine("[WallpaperService] No desktop background scheme found");
                return;
            }

            // Build wallpaper playlist from scheme's media IDs
            _wallpaperPlaylist = resolver.ResolveToMediaItems(bgScheme.DesktopBackgroundMedia.MediaIds)
                .Where(item => item.Type == MediaFileType.Image || item.Type == MediaFileType.Video)
                .Where(item => !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                .ToList();

            if (_wallpaperPlaylist.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[WallpaperService] No valid wallpaper media found in scheme");
                return;
            }

            _wallpaperPlaybackMode = bgScheme.DesktopBackgroundMedia.PlaybackMode;

            // Override duration from MediaReferenceList if set
            if (bgScheme.DesktopBackgroundMedia.ItemDuration.HasValue)
            {
                _wallpaperDurationSeconds = (int)bgScheme.DesktopBackgroundMedia.ItemDuration.Value.TotalSeconds;
            }

            // Build audio playlist from scheme's dedicated background audio list
            // Falls back to any audio in the desktop background media IDs
            if (bgScheme.BackgroundAudio.MediaIds.Count > 0)
            {
                _audioPlaylist = resolver.ResolveToMediaItems(bgScheme.BackgroundAudio.MediaIds)
                    .Where(item => item.Type == MediaFileType.Audio)
                    .Where(item => !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    .ToList();
                _audioPlaybackMode = bgScheme.BackgroundAudio.PlaybackMode;
            }
            else
            {
                _audioPlaylist = resolver.ResolveToMediaItems(bgScheme.DesktopBackgroundMedia.MediaIds)
                    .Where(item => item.Type == MediaFileType.Audio)
                    .Where(item => !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    .ToList();
                _audioPlaybackMode = bgScheme.DesktopBackgroundMedia.PlaybackMode;
            }
            _currentAudioIndex = -1;

            // Show the first wallpaper
            _isDynamicWallpaperActive = true;
            _currentWallpaperIndex = -1;
            ShowNextWallpaper();

            // Start background audio if available and enabled
            if (settings.EnableBackgroundAudio && _audioPlaylist.Count > 0)
            {
                StartBackgroundAudio(settings.BackgroundAudioVolume);
            }

            // Set up cycling timer (only if more than one wallpaper)
            if (_wallpaperPlaylist.Count > 1)
            {
                StartWallpaperCycleTimer();
            }

            // Set up mouse click regions if a mouse click scheme is active
            SetupMouseClickRegions(manifest, resolver, settings);
        }

        /// <summary>
        /// Stops the current theme-based dynamic wallpaper and cleans up resources.
        /// </summary>
        public void StopThemeWallpaper()
        {
            CleanupCurrentWallpaper();
        }

        /// <summary>
        /// Updates the wallpaper duration at runtime (without restarting).
        /// </summary>
        public void SetWallpaperDuration(int durationSeconds)
        {
            _wallpaperDurationSeconds = Math.Max(MinWallpaperDurationSeconds, durationSeconds);

            if (_wallpaperCycleTimer != null)
            {
                _wallpaperCycleTimer.Interval = TimeSpan.FromSeconds(_wallpaperDurationSeconds);
            }
        }

        /// <summary>
        /// Advances to the next wallpaper in the playlist.
        /// Can be called externally to skip the current wallpaper.
        /// </summary>
        public void SkipToNextWallpaper()
        {
            if (_isDynamicWallpaperActive)
            {
                ShowNextWallpaper();
                ResetCycleTimer();
            }
        }

        // --- Theme wallpaper internal methods ---

        private void ShowNextWallpaper()
        {
            if (_wallpaperPlaylist.Count == 0) return;

            // Determine next index
            int nextIndex;
            if (_wallpaperPlaybackMode == PlaybackMode.Random)
            {
                nextIndex = _wallpaperPlaylist.Count > 1
                    ? GetRandomIndexExcluding(_wallpaperPlaylist.Count, _currentWallpaperIndex)
                    : 0;
            }
            else
            {
                nextIndex = (_currentWallpaperIndex + 1) % _wallpaperPlaylist.Count;
            }

            _currentWallpaperIndex = nextIndex;
            var mediaItem = _wallpaperPlaylist[nextIndex];

            Application.Current.Dispatcher.Invoke(() =>
            {
                TransitionToWallpaper(mediaItem);
            });
        }

        private async void TransitionToWallpaper(MediaItemModel mediaItem)
        {
            Window? newWindow = null;

            try
            {
                if (mediaItem.Type == MediaFileType.Video)
                {
                    newWindow = CreateHiddenVideoWindow(mediaItem.FilePath);
                }
                else if (mediaItem.Type == MediaFileType.Image)
                {
                    newWindow = CreateHiddenImageWindow(mediaItem.FilePath);
                }

                if (newWindow == null) return;

                newWindow.Show();

                // Inject into WorkerW (below desktop icons)
                InjectDynamicWallpaper(newWindow);

                // Wait for content to buffer (100ms for video, less for image)
                await Task.Delay(mediaItem.Type == MediaFileType.Video ? 100 : 50);

                // Expand to full screen
                var helper = new WindowInteropHelper(newWindow);
                int screenW = (int)SystemParameters.PrimaryScreenWidth;
                int screenH = (int)SystemParameters.PrimaryScreenHeight;
                Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP,
                    0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);

                // Fade in the new window
                await FadeWindowAsync(newWindow, 0, 1, 500);

                // Close the old background window
                var oldWindow = _currentBackgroundWindow;
                _currentBackgroundWindow = newWindow;

                if (oldWindow != null)
                {
                    await FadeWindowAsync(oldWindow, 1, 0, 300);
                    CloseBackgroundWindow(oldWindow);
                }

                // Also keep legacy _idleVideoWindow reference updated for action video compatibility
                if (newWindow is VideoPlayerWindow vpw)
                    _idleVideoWindow = vpw;
                else
                    _idleVideoWindow = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperService] TransitionToWallpaper error: {ex.Message}");
            }
        }

        private void StartWallpaperCycleTimer()
        {
            _wallpaperCycleTimer?.Stop();
            _wallpaperCycleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_wallpaperDurationSeconds)
            };
            _wallpaperCycleTimer.Tick += (_, _) => ShowNextWallpaper();
            _wallpaperCycleTimer.Start();
        }

        private void ResetCycleTimer()
        {
            if (_wallpaperCycleTimer != null && _wallpaperCycleTimer.IsEnabled)
            {
                _wallpaperCycleTimer.Stop();
                _wallpaperCycleTimer.Start();
            }
        }

        private ImagePlayerWindow CreateHiddenImageWindow(string path)
        {
            var win = new ImagePlayerWindow(path);
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Left = -32000;
            win.Top = -32000;
            win.Width = 1;
            win.Height = 1;
            win.WindowStyle = WindowStyle.None;
            win.ResizeMode = ResizeMode.NoResize;
            win.ShowInTaskbar = false;
            return win;
        }

        private static void CloseBackgroundWindow(Window window)
        {
            try
            {
                if (window is VideoPlayerWindow vpw)
                    vpw.StopAndClose();
                else if (window is ImagePlayerWindow ipw)
                    ipw.StopAndClose();
                else
                    window.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperService] CloseBackgroundWindow error: {ex.Message}");
            }
        }

        // --- Background Audio ---

        private void StartBackgroundAudio(int volumePercent)
        {
            if (_audioPlaylist.Count == 0 || _bgAudioPlayer == null || _tempLibVLC == null)
                return;

            _bgAudioPlayer.Volume = Math.Clamp(volumePercent, 0, 100);

            // Subscribe to EndReached only once to avoid handler accumulation
            if (!_bgAudioEndReachedSubscribed)
            {
                _bgAudioPlayer.EndReached += (_, _) =>
                {
                    // Must schedule on another thread since VLC callbacks are on VLC thread
                    Task.Run(() =>
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            PlayNextBackgroundAudio();
                        });
                    });
                };
                _bgAudioEndReachedSubscribed = true;
            }

            PlayNextBackgroundAudio();
        }

        private void PlayNextBackgroundAudio()
        {
            if (_audioPlaylist.Count == 0 || _bgAudioPlayer == null || _tempLibVLC == null)
                return;

            int nextIndex;
            if (_audioPlaybackMode == PlaybackMode.Random)
            {
                nextIndex = _audioPlaylist.Count > 1
                    ? GetRandomIndexExcluding(_audioPlaylist.Count, _currentAudioIndex)
                    : 0;
            }
            else
            {
                nextIndex = (_currentAudioIndex + 1) % _audioPlaylist.Count;
            }

            _currentAudioIndex = nextIndex;
            var audioItem = _audioPlaylist[nextIndex];

            try
            {
                if (!File.Exists(audioItem.FilePath)) return;

                // Do NOT use 'using' — VLC playback is async and needs the Media alive
                var media = new Media(_tempLibVLC, new Uri(audioItem.FilePath));
                _bgAudioPlayer.Play(media);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperService] Background audio error: {ex.Message}");
            }
        }

        // --- Click Region Setup ---

        private void SetupMouseClickRegions(ThemeManifest manifest, ResourceResolver resolver,
            WallpaperPlaybackSettings settings)
        {
            var clickScheme = manifest.MouseClickSchemes
                .FirstOrDefault(s => s.IsActive)
                ?? manifest.MouseClickSchemes.FirstOrDefault();

            if (clickScheme == null || clickScheme.ClickRegions.Count == 0)
                return;

            _activeClickRegions = clickScheme.ClickRegions.ToList();
            _activeMouseClickScheme = clickScheme;

            // Start mouse hook for click detection
            _mouseHook = new MouseHookService();

            _mouseHook.OnMouseClick += (screenPoint) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HandleThemeClick(screenPoint, resolver, settings);
                });
            };

            _mouseHook.Start();
        }

        private void HandleThemeClick(System.Windows.Point screenPoint,
            ResourceResolver resolver, WallpaperPlaybackSettings settings)
        {
            if (_activeClickRegions == null) return;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            // Convert screen point to percentage (0-100)
            double xPct = screenPoint.X / screenW * 100.0;
            double yPct = screenPoint.Y / screenH * 100.0;

            foreach (var region in _activeClickRegions)
            {
                if (!region.ContainsPoint(xPct, yPct)) continue;

                // Play visual content (action video) if available
                if (!string.IsNullOrEmpty(region.ClickAction.VisualMediaId))
                {
                    var visualItem = resolver.ResolveToMediaItem(region.ClickAction.VisualMediaId);
                    if (visualItem != null && File.Exists(visualItem.FilePath)
                        && visualItem.Type == MediaFileType.Video)
                    {
                        PlayActionVideo(visualItem.FilePath);
                    }
                }

                // Play audio content if available
                if (region.ClickAction.AudioMediaIds.Count > 0)
                {
                    PlayClickRegionAudio(region, resolver, settings.ClickAudioVolume);
                }

                break; // Only trigger the first matching region
            }
        }

        private void PlayClickRegionAudio(ClickRegionModel region, ResourceResolver resolver, int volumePercent)
        {
            if (_audioPlayer == null || _tempLibVLC == null) return;

            var audioIds = region.ClickAction.AudioMediaIds;
            if (audioIds.Count == 0) return;

            // Select audio based on playback mode
            string? audioId;
            if (region.AudioPlaybackMode == PlaybackMode.Random)
            {
                audioId = audioIds[_random.Next(audioIds.Count)];
            }
            else
            {
                // Sequential: track per-region index for true round-robin
                if (!_regionAudioIndex.TryGetValue(region.Id, out int currentIndex))
                {
                    currentIndex = 0;
                }
                audioId = audioIds[currentIndex % audioIds.Count];
                _regionAudioIndex[region.Id] = (currentIndex + 1) % audioIds.Count;
            }

            if (string.IsNullOrEmpty(audioId)) return;

            var audioItem = resolver.ResolveToMediaItem(audioId);
            if (audioItem == null || !File.Exists(audioItem.FilePath)) return;

            try
            {
                _audioPlayer.Volume = Math.Clamp(volumePercent, 0, 100);
                // Do NOT use 'using' — VLC playback is async and needs the Media alive
                var media = new Media(_tempLibVLC, new Uri(audioItem.FilePath));
                _audioPlayer.Play(media);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperService] Click audio error: {ex.Message}");
            }
        }

        // --- Utility ---

        private int GetRandomIndexExcluding(int count, int excludeIndex)
        {
            if (count <= 1) return 0;
            int next;
            do { next = _random.Next(count); }
            while (next == excludeIndex);
            return next;
        }

        private static string GetThemeRootPath(ThemeManifest manifest)
        {
            // If export base path is set, use that
            if (!string.IsNullOrEmpty(manifest.ExportBasePath))
                return manifest.ExportBasePath;

            // Otherwise, construct from AppData
            var themesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProductivityWallpaper", "Themes");
            return Path.Combine(themesRoot, manifest.Name);
        }

        // ==================== End Theme-Based Dynamic Wallpaper ====================

        private void CleanupCurrentWallpaper()
        {
            // Stop dynamic wallpaper cycling
            _isDynamicWallpaperActive = false;
            _wallpaperCycleTimer?.Stop();
            _wallpaperCycleTimer = null;
            _wallpaperPlaylist.Clear();
            _audioPlaylist.Clear();
            _currentWallpaperIndex = -1;
            _currentAudioIndex = -1;
            _activeClickRegions = null;
            _activeResolver = null;
            _activeMouseClickScheme = null;
            _regionAudioIndex.Clear();
            _bgAudioEndReachedSubscribed = false;

            _mouseHook?.Stop();
            _mouseHook = null;
            _currentInteractiveItem = null;
            _currentConfig = null;

            // Stop audio playback
            try { _audioPlayer?.Stop(); } catch { }
            try { _bgAudioPlayer?.Stop(); } catch { }

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

            // Close theme-based background window
            if (_currentBackgroundWindow != null)
            {
                CloseBackgroundWindow(_currentBackgroundWindow);
                _currentBackgroundWindow = null;
            }

            // Recreate audio players for next use (don't dispose _tempLibVLC)
            try { _audioPlayer?.Dispose(); } catch { }
            try { _bgAudioPlayer?.Dispose(); } catch { }
            _audioPlayer = null;
            _bgAudioPlayer = null;

            // Recreate audio players
            if (_tempLibVLC != null)
            {
                try
                {
                    _audioPlayer = new MediaPlayer(_tempLibVLC);
                    _bgAudioPlayer = new MediaPlayer(_tempLibVLC);
                }
                catch { }
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