using Size = System.Drawing.Size;
using Application = System.Windows.Application;
using MediaType = ProductivityWallpaper.Models.MediaType;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class WallpaperService : IDisposable
    {
        private const string MetaFileName = ".wp_meta.json";
        private const string InteractiveConfigName = "config.wallpaper";

        // --- Desktop bridge & performance services ---
        private readonly DesktopBridgeService _desktopBridge;
        private readonly PlaybackMonitorService _playbackMonitor;
        private bool _desktopLayerReady;

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
        private bool _isPausedByFullscreen;
        private readonly Random _random = new();

        // Click region state for theme-based mode
        private List<ClickRegionModel>? _activeClickRegions;
        private ResourceResolver? _activeResolver;
        private WallpaperPlaybackSettings? _activeWallpaperSettings;
        private SchemeModel? _activeMouseClickScheme;
        private ClickRegionOverlayWindow? _clickRegionOverlay;
        private bool _bgAudioEndReachedSubscribed;
        private readonly Dictionary<string, int> _regionAudioIndex = new();
        private const int MinWallpaperDurationSeconds = 5;
        private const int DesktopRecoveryDelayMs = 500;

        // Track Media objects for disposal during cleanup
        private readonly List<Media> _activeMediaObjects = new();
        private bool _disposed;

        // Named handler references for proper event unsubscription (prevents memory leaks)
        private Action<string>? _interactiveTriggerHandler;
        private Action<System.Windows.Point>? _interactiveMouseClickHandler;

        public WallpaperService()
        {
            _desktopBridge = new DesktopBridgeService();
            _playbackMonitor = new PlaybackMonitorService();

            // Wire up desktop layer recovery
            _desktopBridge.OnDesktopLayerInvalidated += OnDesktopLayerInvalidated;

            // Wire up fullscreen auto-pause
            _playbackMonitor.OnFullscreenAppDetected += OnFullscreenAppDetected;
            _playbackMonitor.OnDesktopVisible += OnDesktopVisible;

            try 
            { 
                _tempLibVLC = new LibVLC();
                _audioPlayer = new MediaPlayer(_tempLibVLC);
                _bgAudioPlayer = new MediaPlayer(_tempLibVLC);
            } 
            catch { }
        }

        /// <summary>
        /// Whether the current desktop is in Win11 Raised Desktop mode.
        /// </summary>
        public bool IsRaisedDesktop => _desktopBridge.IsRaisedDesktop;

        // --- Fullscreen Auto-Pause Handlers ---

        private void OnFullscreenAppDetected()
        {
            if (!_isDynamicWallpaperActive || _isPausedByFullscreen) return;
            _isPausedByFullscreen = true;

            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Debug.WriteLine("[WallpaperService] Pausing wallpaper — fullscreen app detected");
                PausePlayback();
            });
        }

        private void OnDesktopVisible()
        {
            if (!_isDynamicWallpaperActive || !_isPausedByFullscreen) return;
            _isPausedByFullscreen = false;

            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Debug.WriteLine("[WallpaperService] Resuming wallpaper — desktop visible");
                ResumePlayback();
            });
        }

        /// <summary>
        /// Pauses video playback and wallpaper cycling timer for zero CPU/GPU usage.
        /// </summary>
        private void PausePlayback()
        {
            _wallpaperCycleTimer?.Stop();

            // Pause the current video if playing
            if (_currentBackgroundWindow is VideoPlayerWindow vpw)
            {
                try { vpw.SetPause(true); } catch { }
            }
            if (_idleVideoWindow != null)
            {
                try { _idleVideoWindow.SetPause(true); } catch { }
            }

            // Pause background audio
            try { _bgAudioPlayer?.SetPause(true); } catch { }
        }

        /// <summary>
        /// Resumes video playback and wallpaper cycling timer.
        /// </summary>
        private void ResumePlayback()
        {
            // Resume the current video
            if (_currentBackgroundWindow is VideoPlayerWindow vpw)
            {
                try { vpw.SetPause(false); } catch { }
            }
            if (_idleVideoWindow != null)
            {
                try { _idleVideoWindow.SetPause(false); } catch { }
            }

            // Resume background audio
            try { _bgAudioPlayer?.SetPause(false); } catch { }

            // Restart the cycle timer
            if (_wallpaperPlaylist.Count > 1)
            {
                _wallpaperCycleTimer?.Start();
            }
        }

        // --- Desktop Layer Recovery ---

        private void OnDesktopLayerInvalidated()
        {
            Debug.WriteLine("[WallpaperService] Desktop layer invalidated — attempting recovery");
            Application.Current?.Dispatcher.BeginInvoke(async () =>
            {
                // Brief delay for Windows to recreate desktop structures
                await Task.Delay(DesktopRecoveryDelayMs);
                
                if (_isDynamicWallpaperActive)
                {
                    // Re-setup the desktop layer
                    _desktopLayerReady = _desktopBridge.SetupDesktopLayer();
                    if (_desktopLayerReady)
                    {
                        Debug.WriteLine("[WallpaperService] Desktop layer recovered — re-injecting current wallpaper");
                        // Re-inject the current background window
                        if (_currentBackgroundWindow != null)
                        {
                            var helper = new WindowInteropHelper(_currentBackgroundWindow);
                            if (helper.Handle != IntPtr.Zero)
                            {
                                _desktopBridge.InjectWindow(helper.Handle, asTopmost: false);
                                var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
                                Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP,
                                    0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);
                            }
                        }

                        // Re-inject click region overlay if active
                        if (_clickRegionOverlay != null)
                        {
                            var overlayHelper = new WindowInteropHelper(_clickRegionOverlay);
                            if (overlayHelper.Handle != IntPtr.Zero)
                            {
                                _desktopBridge.InjectWindow(overlayHelper.Handle, asTopmost: true);
                                var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
                                Win32Api.SetWindowPos(overlayHelper.Handle, Win32Api.HWND_TOP,
                                    0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Ensures the desktop bridge is set up. Returns true if the layer is ready.
        /// </summary>
        private bool EnsureDesktopLayer()
        {
            if (_desktopLayerReady && _desktopBridge.WorkerW != IntPtr.Zero)
                return true;

            _desktopLayerReady = _desktopBridge.SetupDesktopLayer();
            return _desktopLayerReady;
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

            // Ensure desktop layer is ready
            if (!EnsureDesktopLayer()) return;

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

            // 点击热区 - 播放对应的视频和音频 (named handler for proper cleanup)
            var basePath = item.FilePath;
            _interactiveTriggerHandler = (actionVideoName) =>
            {
                var actionPath = Path.Combine(basePath, actionVideoName);
                if (File.Exists(actionPath))
                {
                    PlayActionVideo(actionPath);
                }
            };
            _currentUiWindow.OnTriggerClicked += _interactiveTriggerHandler;

            // 注入
            InjectInteractiveLayers(_idleVideoWindow, _currentUiWindow);

            // 使用淡入效果显示初始壁纸 (更平滑)
            FadeWindowAsync(_idleVideoWindow, 0, 1, 500);
            FadeWindowAsync(_currentUiWindow, 0, 1, 500);

            // 启动 Hook (click only, no sweep)
            _mouseHook = new MouseHookService();

            // 鼠标点击事件 (named handler for proper cleanup)
            _interactiveMouseClickHandler = (screenPoint) =>
            {
                // Only handle clicks that target the desktop — ignore clicks on foreground windows
                if (!_desktopBridge.IsDesktopClick((int)screenPoint.X, (int)screenPoint.Y)) return;

                if (_currentUiWindow != null)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var trigger = _currentUiWindow?.GetTriggerAtPoint(screenPoint);
                        if (trigger != null)
                        {
                            if (!string.IsNullOrEmpty(trigger.Audio))
                            {
                                var audioPath = Path.Combine(basePath, trigger.Audio);
                                PlayAudio(audioPath);
                            }
                            _currentUiWindow?.SimulateClickIfHit(screenPoint);
                        }
                    });
                }
            };
            _mouseHook.OnMouseClick += _interactiveMouseClickHandler;

            _mouseHook.Start();

            // Start playback monitoring for auto-pause
            _isDynamicWallpaperActive = true;
            _playbackMonitor.Start();
        }

        // --- 播放音频 ---
        private void PlayAudio(string audioPath)
        {
            var player = _audioPlayer;
            if (player == null || !File.Exists(audioPath)) return;

            try
            {
                var media = CreateTrackedMedia(audioPath);
                if (media == null) return;
                player.Play(media);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play audio: {ex.Message}");
            }
        }

        // --- 播放动作视频 (核心优化：淡入淡出 + 缓冲) ---
        // 
        // CRITICAL: This method manages the lifecycle of two VLC windows simultaneously:
        //   1. The action video window (overlay)
        //   2. The idle video window (must be reset in background while action plays)
        //
        // The key safety requirement: NEVER stop/dispose one VLC window while another
        // is being created or rendered. VLC's native threads share process-level state,
        // so concurrent Stop()/Dispose() and Play() can corrupt internal state and cause
        // AccessViolationException that .NET 8 cannot catch.
        //
        // Solution: Use _isActionTransitioning guard + await StopAndCloseAsync() to ensure
        // each VLC lifecycle completes before the next one begins.
        private bool _isActionTransitioning;

        private async void PlayActionVideo(string videoPath)
        {
            // Guard: prevent overlapping action video plays.
            // Safe without Interlocked: this method always runs on the UI thread
            // (called from click handlers which dispatch to UI thread).
            if (_isActionTransitioning || _actionVideoWindow != null) return;
            _isActionTransitioning = true;

            try
            {
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WallpaperService] Failed to parse action video duration: {ex.Message}");
                    }
                }

                _actionVideoWindow = CreateHiddenVideoWindow(videoPath);
                _actionVideoWindow.Show();

                // Inject into desktop layer (uses DesktopBridge for Win11 compatibility)
                InjectActionLayer(_actionVideoWindow, _idleVideoWindow, _currentUiWindow);

                await Task.Delay(100);

                // Expand to full screen
                var helper = new WindowInteropHelper(_actionVideoWindow);
                var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
                Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP, 0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);

                // Reset idle video while action video plays (awaited to prevent race)
                await ResetIdleVideoAsync();

                int remainingTime = (int)durationMs - 500 - 300;
                if (remainingTime > 0) await Task.Delay(remainingTime);

                // Capture reference before fade (prevents null ref if cleanup runs during fade)
                var actionWindow = _actionVideoWindow;
                if (actionWindow != null)
                {
                    await FadeWindowAsync(actionWindow, 1, 0, 300);
                    actionWindow.Visibility = Visibility.Hidden;

                    // CRITICAL: Await full VLC shutdown before clearing reference.
                    // This ensures VLC's native rendering thread is completely stopped before
                    // the idle video's VLC instance takes over rendering to the same WorkerW surface.
                    try { await actionWindow.StopAndCloseAsync(); } catch { }
                    _actionVideoWindow = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WallpaperService] PlayActionVideo error: {ex.Message}");
                // Emergency cleanup
                if (_actionVideoWindow != null)
                {
                    try { _actionVideoWindow.StopAndClose(); } catch { }
                    _actionVideoWindow = null;
                }
            }
            finally
            {
                _isActionTransitioning = false;
            }
        }

        /// <summary>
        /// Displays a full-screen image overlay for a set duration (default 3 seconds) on click region trigger.
        /// Uses the same anti-flicker pattern as PlayActionVideo: create hidden → show → inject → expand.
        /// </summary>
        private async void PlayActionImage(string imagePath, int displayDurationMs = 3000)
        {
            // Reuse _actionVideoWindow field as guard (null = no action playing)
            if (_actionVideoWindow != null) return;

            Window? actionWindow = null;
            try
            {
                var imageWindow = CreateHiddenImageWindow(imagePath);
                actionWindow = imageWindow;

                imageWindow.Show();

                // Inject into desktop layer
                InjectDynamicWallpaper(imageWindow);

                await Task.Delay(50);

                // Expand to full screen
                var helper = new WindowInteropHelper(imageWindow);
                var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
                Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP, 0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);

                // Fade in
                await FadeWindowAsync(imageWindow, 0, 1, 300);

                // Display for the specified duration
                await Task.Delay(displayDurationMs);

                // Fade out
                await FadeWindowAsync(imageWindow, 1, 0, 300);

                imageWindow.StopAndClose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperService] PlayActionImage error: {ex.Message}");
                if (actionWindow != null)
                {
                    try
                    {
                        if (actionWindow is ImagePlayerWindow ipw)
                            ipw.StopAndClose();
                        else
                            actionWindow.Close();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Resets the idle video by creating a new window and properly disposing the old one.
        /// 
        /// CRITICAL: Must await the old idle window's StopAndCloseAsync() to ensure VLC's
        /// native rendering thread is fully stopped before the new idle window begins rendering.
        /// Without this await, two VLC instances render to the same WorkerW surface simultaneously,
        /// causing AccessViolationException from native thread conflicts.
        /// </summary>
        private async Task ResetIdleVideoAsync()
        {
            if (_currentInteractiveItem?.InteractiveConfig != null)
            {
                var idlePath = Path.Combine(_currentInteractiveItem.FilePath, _currentInteractiveItem.InteractiveConfig.IdleVideo);
                if (File.Exists(idlePath))
                {
                    // Create new idle window (hidden, 1x1 at -32000,-32000)
                    var newIdleWindow = CreateHiddenVideoWindow(idlePath);
                    newIdleWindow.Show();

                    // Inject at bottom Z-order
                    InjectIdleLayer(newIdleWindow);
                    newIdleWindow.Opacity = 1;

                    // CRITICAL: Stop old idle AFTER new one is ready, and AWAIT completion.
                    // This ensures VLC's native threads from the old window are fully stopped
                    // before we return control to the caller.
                    var oldIdle = _idleVideoWindow;
                    _idleVideoWindow = newIdleWindow;

                    if (oldIdle != null)
                    {
                        try { await oldIdle.StopAndCloseAsync(); } catch { }
                    }
                }
            }
        }

        // Keep synchronous version for non-critical paths (cleanup only)
        private void ResetIdleVideoInBackground()
        {
            _ = ResetIdleVideoAsync();
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
            if (!EnsureDesktopLayer()) return;
            
            var videoWin = CreateHiddenVideoWindow(path);
            videoWin.Show();
            InjectDynamicWallpaper(videoWin);
            FadeWindowAsync(videoWin, 0, 1, 500);
            _idleVideoWindow = videoWin;
            
            _isDynamicWallpaperActive = true;
            _playbackMonitor.Start();
        }

        /// <summary>
        /// Sets a static wallpaper using IDesktopWallpaper COM interface (Windows native).
        /// This is zero-overhead — Windows renders the wallpaper natively with perfect DPI/HDR support.
        /// Falls back to SystemParametersInfo if COM interface is unavailable.
        /// </summary>
        public void SetStaticWallpaper(string path, int monitorIndex = -1)
        {
            CleanupCurrentWallpaper();
            
            try
            {
                // Prefer IDesktopWallpaper COM for native Windows rendering
                var desktop = (Win32Api.IDesktopWallpaper)new Win32Api.DesktopWallpaperClass();
                desktop.SetPosition(Win32Api.DesktopWallpaperPosition.Fill);
                
                if (monitorIndex >= 0)
                {
                    // Set wallpaper for specific monitor
                    uint count = desktop.GetMonitorDevicePathCount();
                    if ((uint)monitorIndex < count)
                    {
                        string monitorId = desktop.GetMonitorDevicePathAt((uint)monitorIndex);
                        desktop.SetWallpaper(monitorId, path);
                    }
                }
                else
                {
                    // Set wallpaper for all monitors
                    desktop.SetWallpaper(null, path);
                }
                
                Debug.WriteLine($"[WallpaperService] Static wallpaper set via IDesktopWallpaper COM: {path}");
            }
            catch (Exception ex)
            {
                // Fallback to legacy SystemParametersInfo
                Debug.WriteLine($"[WallpaperService] IDesktopWallpaper COM failed ({ex.Message}), falling back to SPI");
                Win32Api.SystemParametersInfo(Win32Api.SPI_SETDESKWALLPAPER, 0, path,
                    Win32Api.SPIF_UPDATEINIFILE | Win32Api.SPIF_SENDCHANGE);
            }
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
            // Ensure desktop layer is ready
            if (!EnsureDesktopLayer())
            {
                Debug.WriteLine("[WallpaperService] Failed to setup desktop layer");
                return;
            }

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

            // Start fullscreen detection for auto-pause
            _playbackMonitor.Start();
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
                var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
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
                    try { await CloseBackgroundWindowAsync(oldWindow); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WallpaperService] Failed to close old background window: {ex.Message}");
                    }
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
            _wallpaperCycleTimer.Tick += OnWallpaperCycleTick;
            _wallpaperCycleTimer.Start();
        }

        /// <summary>
        /// Named handler for the wallpaper cycle timer tick event.
        /// Using a named method instead of an anonymous lambda allows proper unsubscription
        /// during cleanup, preventing memory leaks from accumulated event handlers.
        /// </summary>
        private void OnWallpaperCycleTick(object? sender, EventArgs e)
        {
            ShowNextWallpaper();
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

        /// <summary>
        /// Closes a background window, awaiting VLC shutdown for video windows.
        /// This prevents concurrent VLC native threads from conflicting during transitions.
        /// </summary>
        private static async Task CloseBackgroundWindowAsync(Window window)
        {
            try
            {
                if (window is VideoPlayerWindow vpw)
                    await vpw.StopAndCloseAsync();
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

        private static void CloseBackgroundWindow(Window window)
        {
            _ = CloseBackgroundWindowAsync(window);
        }

        // --- Background Audio ---

        private void StartBackgroundAudio(int volumePercent)
        {
            if (_audioPlaylist.Count == 0 || _bgAudioPlayer == null || _tempLibVLC == null)
                return;

            _bgAudioPlayer.Volume = Math.Clamp(volumePercent, 0, 100);

            // Subscribe to EndReached only once to avoid handler accumulation
            // Uses a named method so it can be properly unsubscribed during cleanup
            if (!_bgAudioEndReachedSubscribed)
            {
                _bgAudioPlayer.EndReached += OnBgAudioEndReached;
                _bgAudioEndReachedSubscribed = true;
            }

            PlayNextBackgroundAudio();
        }

        /// <summary>
        /// Named handler for background audio EndReached event.
        /// VLC callbacks fire on native background threads, so we dispatch to UI thread.
        /// Named method enables proper unsubscription during cleanup (preventing memory leaks).
        /// </summary>
        private void OnBgAudioEndReached(object? sender, EventArgs e)
        {
            if (!_isDynamicWallpaperActive) return;
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                PlayNextBackgroundAudio();
            });
        }

        private void PlayNextBackgroundAudio()
        {
            var player = _bgAudioPlayer;
            if (!_isDynamicWallpaperActive || _audioPlaylist.Count == 0 || player == null || _tempLibVLC == null)
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

                var media = CreateTrackedMedia(audioItem.FilePath);
                if (media == null) return;
                player.Play(media);
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
            _activeWallpaperSettings = settings;

            // Create the click region overlay window — follows the old InteractiveUiWindow pattern:
            // a transparent window injected into WorkerW at the topmost Z-order.
            // debugVisible=true makes regions red so we can verify correct positioning.
            _clickRegionOverlay = new ClickRegionOverlayWindow();
            _clickRegionOverlay.WindowStartupLocation = WindowStartupLocation.Manual;
            _clickRegionOverlay.Left = -32000;
            _clickRegionOverlay.Top = -32000;
            _clickRegionOverlay.Width = 1;
            _clickRegionOverlay.Height = 1;
            _clickRegionOverlay.LoadRegions(_activeClickRegions, debugVisible: false);
            _clickRegionOverlay.Show();

            // Inject into WorkerW at the topmost Z-order (same as InteractiveUiWindow)
            InjectClickRegionOverlay(_clickRegionOverlay);

            // Layout is auto-updated via SizeChanged event in ClickRegionOverlayWindow

            // Start mouse hook for click detection
            _mouseHook = new MouseHookService();
            _mouseHook.OnMouseClick += OnThemeMouseClick;
            _mouseHook.Start();
        }

        /// <summary>
        /// Named handler for mouse clicks in theme-based dynamic wallpaper mode.
        /// Uses stored _activeResolver and _activeWallpaperSettings instead of captured locals
        /// so the handler can be properly unsubscribed during cleanup (preventing memory leaks).
        /// 
        /// Checks IsDesktopClick first to ensure no foreground window is covering the click point.
        /// This prevents accidental triggers when clicking on browsers, popups, or other windows
        /// that overlap with the desktop click regions.
        /// </summary>
        private void OnThemeMouseClick(System.Windows.Point screenPoint)
        {
            var resolver = _activeResolver;
            var settings = _activeWallpaperSettings;
            if (resolver == null || settings == null) return;

            // Verify the click actually targets the desktop — not a foreground window covering it
            if (!_desktopBridge.IsDesktopClick((int)screenPoint.X, (int)screenPoint.Y)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                HandleThemeClick(screenPoint, resolver, settings);
            });
        }

        /// <summary>
        /// Injects the click region overlay window into WorkerW at the topmost Z-order.
        /// This is the same pattern used by InjectInteractiveLayers for the old InteractiveUiWindow.
        /// The overlay sits above the wallpaper content but below desktop icons.
        /// Uses WorkerW's actual client rect for sizing (DPI-safe).
        /// </summary>
        private void InjectClickRegionOverlay(ClickRegionOverlayWindow overlay)
        {
            if (!EnsureDesktopLayer()) return;

            var helper = new WindowInteropHelper(overlay);

            // Remove popup style, keep child window style
            int style = Win32Api.GetWindowLong(helper.Handle, Win32Api.GWL_STYLE);
            style = style & ~Win32Api.WS_POPUP & ~Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(helper.Handle, Win32Api.GWL_STYLE, style);

            _desktopBridge.InjectWindow(helper.Handle, asTopmost: true);
            overlay.WindowState = WindowState.Maximized;

            var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
            Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP, 0, 0,
                screenW, screenH, Win32Api.SWP_NOACTIVATE);
        }

        private void HandleThemeClick(System.Windows.Point screenPoint,
            ResourceResolver resolver, WallpaperPlaybackSettings settings)
        {
            if (_activeClickRegions == null) return;

            // Use physical screen resolution because mouse hook coordinates are in
            // physical screen pixels (WH_MOUSE_LL with Per-Monitor DPI Aware v2)
            int physicalW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int physicalH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);
            if (physicalW <= 0 || physicalH <= 0) return;

            // Convert physical screen point to normalized (0–1)
            double xNorm = screenPoint.X / physicalW;
            double yNorm = screenPoint.Y / physicalH;

            foreach (var region in _activeClickRegions)
            {
                if (!region.ContainsPoint(xNorm, yNorm)) continue;

                // Play visual content (image or video) if available
                if (!string.IsNullOrEmpty(region.ClickAction.VisualMediaId))
                {
                    var visualItem = resolver.ResolveToMediaItem(region.ClickAction.VisualMediaId);
                    if (visualItem != null && File.Exists(visualItem.FilePath))
                    {
                        if (visualItem.Type == MediaFileType.Video)
                        {
                            PlayActionVideo(visualItem.FilePath);
                        }
                        else if (visualItem.Type == MediaFileType.Image)
                        {
                            PlayActionImage(visualItem.FilePath);
                        }
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
            var player = _audioPlayer;
            if (player == null || _tempLibVLC == null) return;

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
                _regionAudioIndex[region.Id] = currentIndex + 1;
            }

            if (string.IsNullOrEmpty(audioId)) return;

            var audioItem = resolver.ResolveToMediaItem(audioId);
            if (audioItem == null || !File.Exists(audioItem.FilePath)) return;

            try
            {
                player.Volume = Math.Clamp(volumePercent, 0, 100);
                var media = CreateTrackedMedia(audioItem.FilePath);
                if (media == null) return;
                player.Play(media);
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

        /// <summary>
        /// Creates a Media object with lifecycle tracking. Tracked Media objects
        /// are disposed during CleanupCurrentWallpaper to prevent resource leaks.
        /// Returns null if LibVLC is not available (e.g., during cleanup).
        /// </summary>
        private Media? CreateTrackedMedia(string filePath)
        {
            if (_tempLibVLC == null) return null;
            var media = new Media(_tempLibVLC, new Uri(filePath));
            _activeMediaObjects.Add(media);
            return media;
        }

        /// <summary>
        /// Disposes all tracked Media objects and clears the list.
        /// </summary>
        private void DisposeTrackedMedia()
        {
            foreach (var media in _activeMediaObjects)
            {
                try { media.Dispose(); } catch { }
            }
            _activeMediaObjects.Clear();
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
            // Reset transition guard to allow new actions after cleanup
            _isActionTransitioning = false;

            // Stop playback monitor and reset pause state
            _playbackMonitor.Stop();
            _isPausedByFullscreen = false;

            // Stop dynamic wallpaper cycling
            _isDynamicWallpaperActive = false;
            if (_wallpaperCycleTimer != null)
            {
                _wallpaperCycleTimer.Stop();
                _wallpaperCycleTimer.Tick -= OnWallpaperCycleTick;
            }
            _wallpaperCycleTimer = null;
            _wallpaperPlaylist.Clear();
            _audioPlaylist.Clear();
            _currentWallpaperIndex = -1;
            _currentAudioIndex = -1;
            _activeClickRegions = null;
            _activeResolver = null;
            _activeMouseClickScheme = null;
            _regionAudioIndex.Clear();

            // Stop mouse hook first to prevent new callbacks — unsubscribe ALL named handlers
            if (_mouseHook != null)
            {
                _mouseHook.OnMouseClick -= OnThemeMouseClick;
                _mouseHook.OnMouseClick -= _interactiveMouseClickHandler;
                _mouseHook.Stop();
                _mouseHook.Dispose();
            }
            _mouseHook = null;
            _interactiveMouseClickHandler = null;
            _activeWallpaperSettings = null;
            _currentInteractiveItem = null;
            _currentConfig = null;

            // Capture and null-out audio players to prevent VLC callbacks from accessing them
            // Unsubscribe EndReached handler before releasing the reference
            var oldAudioPlayer = _audioPlayer;
            var oldBgAudioPlayer = _bgAudioPlayer;
            if (oldBgAudioPlayer != null)
            {
                oldBgAudioPlayer.EndReached -= OnBgAudioEndReached;
            }
            _audioPlayer = null;
            _bgAudioPlayer = null;
            _bgAudioEndReachedSubscribed = false;

            // Stop audio playback on captured references
            try { oldAudioPlayer?.Stop(); } catch { }
            try { oldBgAudioPlayer?.Stop(); } catch { }

            // Dispose tracked Media objects to prevent resource leaks
            DisposeTrackedMedia();

            if (_currentUiWindow != null)
            {
                // Unsubscribe named trigger handler to prevent memory leak
                if (_interactiveTriggerHandler != null)
                    _currentUiWindow.OnTriggerClicked -= _interactiveTriggerHandler;
                _interactiveTriggerHandler = null;
                _currentUiWindow.Close();
                _currentUiWindow = null;
            }

            // Close click region overlay window (cleanup first to unsubscribe handlers)
            if (_clickRegionOverlay != null)
            {
                try { _clickRegionOverlay.Cleanup(); } catch { }
                try { _clickRegionOverlay.Close(); } catch { }
                _clickRegionOverlay = null;
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

            // Dispose old audio players after all windows are closed
            try { oldAudioPlayer?.Dispose(); } catch { }
            try { oldBgAudioPlayer?.Dispose(); } catch { }

            // Dispose and recreate LibVLC to prevent corrupted state across theme switches
            // This is critical: reusing a stale LibVLC instance causes AccessViolationException
            try { _tempLibVLC?.Dispose(); } catch { }
            _tempLibVLC = null;

            try
            {
                _tempLibVLC = new LibVLC();
                _audioPlayer = new MediaPlayer(_tempLibVLC);
                _bgAudioPlayer = new MediaPlayer(_tempLibVLC);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperService] Failed to recreate LibVLC: {ex.Message}");
            }
        }

        // --- 注入与层级控制 ---

        private void InjectDynamicWallpaper(Window playerWindow)
        {
            if (!EnsureDesktopLayer()) return;

            var helper = new WindowInteropHelper(playerWindow);
            RemoveBorderAndSetTransparent(helper.Handle);
            _desktopBridge.InjectWindow(helper.Handle, asTopmost: false);
            playerWindow.WindowState = WindowState.Maximized;
        }

        private void InjectIdleLayer(Window idleWin)
        {
            if (!EnsureDesktopLayer()) return;

            var idleHelper = new WindowInteropHelper(idleWin);
            RemoveBorderAndSetTransparent(idleHelper.Handle);
            _desktopBridge.InjectWindow(idleHelper.Handle, asTopmost: false);
            Win32Api.SetWindowPos(idleHelper.Handle, Win32Api.HWND_BOTTOM, 0, 0, 0, 0,
                Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
            idleWin.WindowState = WindowState.Maximized;
        }

        private void InjectInteractiveLayers(Window idleWin, InteractiveUiWindow uiWin)
        {
            InjectIdleLayer(idleWin);

            if (!EnsureDesktopLayer()) return;

            var uiHelper = new WindowInteropHelper(uiWin);
            int style = Win32Api.GetWindowLong(uiHelper.Handle, Win32Api.GWL_STYLE);
            style = style & ~Win32Api.WS_POPUP & ~Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(uiHelper.Handle, Win32Api.GWL_STYLE, style);

            _desktopBridge.InjectWindow(uiHelper.Handle, asTopmost: true);
            uiWin.WindowState = WindowState.Maximized;

            var (screenW, screenH) = _desktopBridge.GetScreenDimensions();
            Win32Api.SetWindowPos(uiHelper.Handle, Win32Api.HWND_TOP, 0, 0, screenW, screenH, Win32Api.SWP_NOACTIVATE);
            uiWin.UpdateLayout(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        private void InjectActionLayer(Window actionWin, Window? idleWin, InteractiveUiWindow? uiWin)
        {
            if (!EnsureDesktopLayer()) return;

            var actionHelper = new WindowInteropHelper(actionWin);
            RemoveBorderAndSetTransparent(actionHelper.Handle);
            _desktopBridge.InjectWindow(actionHelper.Handle, asTopmost: false);
            actionWin.WindowState = WindowState.Maximized;

            // Z-Order: UI > Action > Idle
            Win32Api.SetWindowPos(actionHelper.Handle, Win32Api.HWND_TOP, 0, 0, 0, 0,
                Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);

            if (uiWin != null)
            {
                var uiHandle = new WindowInteropHelper(uiWin).Handle;
                Win32Api.SetWindowPos(uiHandle, Win32Api.HWND_TOP, 0, 0, 0, 0,
                    Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE);
            }
        }

        // FindWorkerW is now handled by DesktopBridgeService with Win11 Raised Desktop support

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
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                CleanupCurrentWallpaper();
                
                // Unsubscribe from service events
                _desktopBridge.OnDesktopLayerInvalidated -= OnDesktopLayerInvalidated;
                _playbackMonitor.OnFullscreenAppDetected -= OnFullscreenAppDetected;
                _playbackMonitor.OnDesktopVisible -= OnDesktopVisible;
                
                _desktopBridge.Dispose();
                _playbackMonitor.Dispose();
                
                try { _tempLibVLC?.Dispose(); } catch { }
                _tempLibVLC = null;
            }
        }
    }
}