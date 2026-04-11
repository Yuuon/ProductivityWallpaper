using LibVLCSharp.Shared;
using Microsoft.Win32;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using System.Windows.Threading;
using Application = System.Windows.Application;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using MediaType = ProductivityWallpaper.Models.MediaType;
using Size = System.Drawing.Size;

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
        private WallpaperPlaybackSettings? _activeWallpaperSettings;
        private SchemeModel? _activeMouseClickScheme;
        private ClickRegionOverlayWindow? _clickRegionOverlay;
        private bool _bgAudioEndReachedSubscribed;
        private readonly Dictionary<string, int> _regionAudioIndex = new();
        private const int MinWallpaperDurationSeconds = 5;

        // Track Media objects for disposal during cleanup
        private readonly List<Media> _activeMediaObjects = new();

        // Cached WorkerW handle — FindWorkerW is expensive (sends 0x052C to Explorer).
        // We find it once and reuse. Invalidated during CleanupCurrentWallpaper.
        private IntPtr _cachedWorkerW = IntPtr.Zero;

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

            // Don't use WPF Opacity animation — it doesn't work after SetParent into WorkerW.
            // Set opacity directly to 1 for both windows.
            _idleVideoWindow.Opacity = 1;
            _currentUiWindow.Opacity = 1;

            // 启动 Hook
            _mouseHook = new MouseHookService();

            // 鼠标点击事件 — use BeginInvoke to avoid blocking the global hook chain.
            // Invoke blocks until UI thread finishes, stalling all mouse input system-wide.
            _mouseHook.OnMouseClick += (screenPoint) =>
            {
                if (_currentUiWindow != null)
                {
                    // Check desktop visibility first — don't trigger through other windows
                    if (!IsDesktopAtPoint(screenPoint)) return;

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        var trigger = _currentUiWindow?.GetTriggerAtPoint(screenPoint);
                        if (trigger != null)
                        {
                            // 播放音频（如果有配置）
                            if (!string.IsNullOrEmpty(trigger.Audio))
                            {
                                var audioPath = Path.Combine(item.FilePath, trigger.Audio);
                                PlayAudio(audioPath);
                            }
                            _currentUiWindow?.SimulateClickIfHit(screenPoint);
                        }
                    });
                }
            };

            _mouseHook.Start();
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
        // Flag to prevent re-entry while the previous action video is still cleaning up.
        private bool _isActionVideoClosing;

        private async void PlayActionVideo(string videoPath)
        {
            // Guard: only one action video at a time.
            // Also guard against re-entry while the previous action video's async cleanup is in progress.
            if (_actionVideoWindow != null || _isActionVideoClosing) return;

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

            // 2. 创建窗口 (此时它是 1x1 大小，位于 -32000)
            _actionVideoWindow = CreateHiddenVideoWindow(videoPath);

            // 3. 显示窗口 — HwndHost 初始化需要 Show()
            _actionVideoWindow.Show();

            // 4. 挂载到桌面 — 窗口变成 WorkerW 的子窗口
            bool injected = InjectActionLayer(_actionVideoWindow, _idleVideoWindow, _currentUiWindow);

            // If injection failed, try invalidating cache and retrying once
            if (!injected)
            {
                System.Diagnostics.Debug.WriteLine("[PlayActionVideo] First injection failed, invalidating cache and retrying...");
                _cachedWorkerW = IntPtr.Zero;
                injected = InjectActionLayer(_actionVideoWindow, _idleVideoWindow, _currentUiWindow);
            }

            if (!injected)
            {
                System.Diagnostics.Debug.WriteLine("[PlayActionVideo] Injection failed after retry, aborting");
                try { _actionVideoWindow.StopAndClose(); } catch { }
                _actionVideoWindow = null;
                return;
            }

            // 5. [缓冲等待] — VLC 在 1x1 时加载视频
            await Task.Delay(100);

            // 6. 后台重置 Idle
            ResetIdleVideoInBackground();

            // 7. 等待播放结束
            int remainingTime = (int)durationMs - 500 - 300;
            if (remainingTime > 0) await Task.Delay(remainingTime);

            // 8. Close — use _isActionVideoClosing flag to prevent re-entry during async cleanup
            if (_actionVideoWindow != null)
            {
                _isActionVideoClosing = true;
                var actionWin = _actionVideoWindow;
                _actionVideoWindow = null;

                try { actionWin.Visibility = Visibility.Hidden; } catch { }
                try { actionWin.StopAndClose(); } catch { }

                // Give StopAndClose time to process: ThreadPool.Stop() + BeginInvoke(Close).
                await Task.Delay(200);
                _isActionVideoClosing = false;
            }
        }

        /// <summary>
        /// Displays a full-screen image overlay for a set duration (default 3 seconds) on click region trigger.
        /// Uses the same anti-flicker pattern as PlayActionVideo: create hidden → show → inject → expand.
        /// Also uses _isActionVideoClosing guard for synchronization with action video cleanup.
        /// </summary>
        private async void PlayActionImage(string imagePath, int displayDurationMs = 3000)
        {
            // Reuse _actionVideoWindow field as guard (null = no action playing)
            if (_actionVideoWindow != null || _isActionVideoClosing) return;

            Window? actionWindow = null;
            try
            {
                var imageWindow = CreateHiddenImageWindow(imagePath);
                actionWindow = imageWindow;

                imageWindow.Show();

                // Inject into WorkerW — use InjectActionLayer for proper Z-order (above background)
                bool injected = InjectActionLayer(imageWindow, _idleVideoWindow, _currentUiWindow);
                if (!injected)
                {
                    // Retry with cache invalidation
                    _cachedWorkerW = IntPtr.Zero;
                    injected = InjectActionLayer(imageWindow, _idleVideoWindow, _currentUiWindow);
                }

                if (!injected)
                {
                    imageWindow.StopAndClose();
                    return;
                }

                // Brief delay for rendering
                await Task.Delay(50);

                // Display for the specified duration
                await Task.Delay(displayDurationMs);

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

        public void ApplyVideoWallpaper(string path, int monitorIndex)
        {
            CleanupCurrentWallpaper();
            var videoWin = CreateHiddenVideoWindow(path);
            videoWin.Show();
            bool injected = InjectDynamicWallpaper(videoWin);
            if (!injected)
            {
                videoWin.StopAndClose();
                return;
            }
            // Don't use WPF Opacity animation — it doesn't work after SetParent into WorkerW
            videoWin.Opacity = 1;
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
                bool injected = InjectDynamicWallpaper(newWindow);
                if (!injected)
                {
                    // Injection failed — close the orphaned window to prevent it from covering the screen
                    System.Diagnostics.Debug.WriteLine("[WallpaperService] Injection failed, closing orphaned window");
                    if (newWindow is VideoPlayerWindow vpw2)
                        vpw2.StopAndClose();
                    else
                        newWindow.Close();
                    return;
                }

                // Wait for content to buffer (100ms for video, less for image)
                await Task.Delay(mediaItem.Type == MediaFileType.Video ? 100 : 50);

                // CRITICAL: Do NOT use WPF's Window.Opacity animation for WorkerW-injected windows.
                // After SetParent into WorkerW, WPF's DWM-based Opacity property doesn't work correctly.
                // Setting Opacity=0 makes the window invisible, and the animation back to 1 never
                // visually takes effect. VLC video shows through because HwndHost bypasses WPF Opacity,
                // but WPF-rendered content (Image, Background) stays invisible — causing the
                // "transparent edges around video" and "image wallpaper shows nothing" symptoms.
                // Instead, ensure full opacity and use direct Win32 calls if fade is needed.
                newWindow.Opacity = 1;

                // Close the old background window
                var oldWindow = _currentBackgroundWindow;
                _currentBackgroundWindow = newWindow;

                if (oldWindow != null)
                {
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
        /// </summary>
        private void OnThemeMouseClick(System.Windows.Point screenPoint)
        {
            var resolver = _activeResolver;
            var settings = _activeWallpaperSettings;
            if (resolver == null || settings == null) return;

            // Check if the click is actually on the desktop — not through another window.
            // The global mouse hook (WH_MOUSE_LL) fires for ALL clicks system-wide.
            // Without this check, clicking in a browser/app that overlaps a trigger zone
            // would still trigger the wallpaper action.
            if (!IsDesktopAtPoint(screenPoint)) return;

            // BeginInvoke (not Invoke) — global mouse hooks run on every mouse event system-wide.
            // Invoke blocks until UI thread finishes, stalling the hook chain and causing
            // system-wide mouse lag when HandleThemeClick takes time (e.g., creating windows).
            Application.Current.Dispatcher.BeginInvoke(() =>
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
            var helper = new WindowInteropHelper(overlay);
            IntPtr workerw = FindWorkerW();
            if (workerw == IntPtr.Zero) return;

            Win32Api.SetParent(helper.Handle, workerw);

            // Remove popup and border, add WS_CHILD, keep visible.
            int style = Win32Api.GetWindowLong(helper.Handle, Win32Api.GWL_STYLE);
            style = style & ~Win32Api.WS_POPUP & ~0x00C00000 & ~0x00040000;
            style = style | Win32Api.WS_CHILD | Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(helper.Handle, Win32Api.GWL_STYLE, style);

            int exStyle = Win32Api.GetWindowLong(helper.Handle, Win32Api.GWL_EXSTYLE);
            exStyle |= Win32Api.WS_EX_TOOLWINDOW;
            Win32Api.SetWindowLong(helper.Handle, Win32Api.GWL_EXSTYLE, exStyle);

            int screenW, screenH;
            if (Win32Api.GetClientRect(workerw, out var rect))
            {
                screenW = rect.right - rect.left;
                screenH = rect.bottom - rect.top;
            }
            else
            {
                screenW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
                screenH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);
            }

            Win32Api.SetWindowPos(helper.Handle, Win32Api.HWND_TOP, 0, 0,
                screenW, screenH, Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_FRAMECHANGED);

            SetupNativeBlackBackground(helper.Handle);
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

            // Invalidate WorkerW cache — next ApplyTheme will rediscover it
            _cachedWorkerW = IntPtr.Zero;

            // Stop mouse hook first to prevent new callbacks — unsubscribe named handlers
            if (_mouseHook != null)
            {
                _mouseHook.OnMouseClick -= OnThemeMouseClick;
                _mouseHook.Stop();
            }
            _mouseHook = null;
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
                _currentUiWindow.Close();
                _currentUiWindow = null;
            }

            // Close click region overlay window
            if (_clickRegionOverlay != null)
            {
                try { _clickRegionOverlay.Close(); } catch { }
                _clickRegionOverlay = null;
            }

            if (_actionVideoWindow != null)
            {
                try { _actionVideoWindow.StopAndClose(); } catch { }
                _actionVideoWindow = null;
            }
            _isActionVideoClosing = false;

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

        /// <summary>
        /// Injects a wallpaper window into the WorkerW behind the desktop icons.
        /// 
        /// Uses SetParent + WS_CHILD style for proper parent-child relationship.
        /// After injection, sets up native GDI painting to ensure content is visible
        /// (WPF's DirectX rendering doesn't work inside WorkerW).
        /// 
        /// Returns true if injection succeeded, false if WorkerW not found.
        /// </summary>
        private bool InjectDynamicWallpaper(Window playerWindow)
        {
            var helper = new WindowInteropHelper(playerWindow);
            IntPtr hwnd = helper.Handle;
            IntPtr workerw = FindWorkerW();

            if (workerw == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[InjectDynamicWallpaper] FindWorkerW returned null — injection skipped");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[InjectDynamicWallpaper] Injecting hwnd=0x{hwnd:X8} into WorkerW=0x{workerw:X8}");

            // 1. Reparent: make the WPF window a child of WorkerW
            IntPtr result = Win32Api.SetParent(hwnd, workerw);
            System.Diagnostics.Debug.WriteLine($"[InjectDynamicWallpaper] SetParent result: 0x{result:X8}");

            // 2. Change styles: remove popup/border, add WS_CHILD, add toolwindow
            RemoveBorderAndSetTransparent(hwnd);

            // 3. Size to fill the screen using physical pixels (DPI-safe).
            //    SWP_FRAMECHANGED is REQUIRED after SetWindowLong to force the window
            //    to recalculate its non-client area and repaint correctly.
            int screenW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int screenH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);

            System.Diagnostics.Debug.WriteLine($"[InjectDynamicWallpaper] SetWindowPos to {screenW}x{screenH}");

            Win32Api.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, screenW, screenH,
                Win32Api.SWP_NOZORDER | Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_FRAMECHANGED);

            // 4. Ensure the window is shown
            Win32Api.ShowWindow(hwnd, Win32Api.SW_SHOW);

            // 5. Clear WPF size constraints so WPF layout uses the actual window size
            ClearWpfSizeConstraints(playerWindow);

            // 6. Set up native GDI painting based on window type.
            //    This bypasses WPF's broken DirectX/DWM rendering inside WorkerW.
            if (playerWindow is ImagePlayerWindow imageWindow)
            {
                SetupNativeImagePainting(hwnd, imageWindow);
            }
            else
            {
                SetupNativeBlackBackground(hwnd);
            }

            // Verify injection
            IntPtr actualParent = Win32Api.GetParent(hwnd);
            bool isVisible = Win32Api.IsWindowVisible(hwnd);
            System.Diagnostics.Debug.WriteLine($"[InjectDynamicWallpaper] Verification: parent=0x{actualParent:X8} (expected 0x{workerw:X8}), visible={isVisible}");

            return actualParent == workerw;
        }

        private void InjectIdleLayer(Window idleWin)
        {
            var idleHelper = new WindowInteropHelper(idleWin);
            IntPtr workerw = FindWorkerW();
            if (workerw == IntPtr.Zero) return;

            Win32Api.SetParent(idleHelper.Handle, workerw);
            RemoveBorderAndSetTransparent(idleHelper.Handle);

            // Size at bottom z-order using physical screen metrics
            int screenW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int screenH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);
            Win32Api.SetWindowPos(idleHelper.Handle, Win32Api.HWND_BOTTOM, 0, 0, screenW, screenH,
                Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_FRAMECHANGED);

            ClearWpfSizeConstraints(idleWin);

            if (idleWin is ImagePlayerWindow imageWindow)
                SetupNativeImagePainting(idleHelper.Handle, imageWindow);
            else
                SetupNativeBlackBackground(idleHelper.Handle);
        }

        private void InjectInteractiveLayers(Window idleWin, InteractiveUiWindow uiWin)
        {
            InjectIdleLayer(idleWin);

            var uiHelper = new WindowInteropHelper(uiWin);
            IntPtr workerw = FindWorkerW();
            Win32Api.SetParent(uiHelper.Handle, workerw);

            // UI overlay: remove popup and border, add WS_CHILD, keep visible, add toolwindow
            int style = Win32Api.GetWindowLong(uiHelper.Handle, Win32Api.GWL_STYLE);
            style = style & ~Win32Api.WS_POPUP & ~0x00C00000 & ~0x00040000;
            style = style | Win32Api.WS_CHILD | Win32Api.WS_VISIBLE;
            Win32Api.SetWindowLong(uiHelper.Handle, Win32Api.GWL_STYLE, style);

            int exStyle = Win32Api.GetWindowLong(uiHelper.Handle, Win32Api.GWL_EXSTYLE);
            exStyle |= Win32Api.WS_EX_TOOLWINDOW;
            Win32Api.SetWindowLong(uiHelper.Handle, Win32Api.GWL_EXSTYLE, exStyle);

            // Size at top z-order using physical screen metrics
            int screenW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int screenH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);
            Win32Api.SetWindowPos(uiHelper.Handle, Win32Api.HWND_TOP, 0, 0, screenW, screenH,
                Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_FRAMECHANGED);

            SetupNativeBlackBackground(uiHelper.Handle);

            uiWin.UpdateLayout(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        private bool InjectActionLayer(Window actionWin, Window? idleWin, InteractiveUiWindow? uiWin)
        {
            var actionHelper = new WindowInteropHelper(actionWin);
            IntPtr workerw = FindWorkerW();
            if (workerw == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[InjectActionLayer] FindWorkerW returned null");
                return false;
            }

            IntPtr setParentResult = Win32Api.SetParent(actionHelper.Handle, workerw);
            System.Diagnostics.Debug.WriteLine($"[InjectActionLayer] SetParent result: 0x{setParentResult:X8}");

            RemoveBorderAndSetTransparent(actionHelper.Handle);

            // Size and position — Z-Order: UI > Action > Idle
            int screenW = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
            int screenH = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);
            Win32Api.SetWindowPos(actionHelper.Handle, Win32Api.HWND_TOP, 0, 0, screenW, screenH,
                Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_FRAMECHANGED);

            ClearWpfSizeConstraints(actionWin);

            if (actionWin is ImagePlayerWindow imageWindow)
                SetupNativeImagePainting(actionHelper.Handle, imageWindow);
            else
                SetupNativeBlackBackground(actionHelper.Handle);

            if (uiWin != null)
            {
                var uiHandle = new WindowInteropHelper(uiWin).Handle;
                Win32Api.SetWindowPos(uiHandle, Win32Api.HWND_TOP, 0, 0, 0, 0,
                    Win32Api.SWP_NOSIZE | Win32Api.SWP_NOMOVE | Win32Api.SWP_NOACTIVATE);
            }

            // Verify injection succeeded
            IntPtr actualParent = Win32Api.GetParent(actionHelper.Handle);
            bool injected = actualParent == workerw;
            System.Diagnostics.Debug.WriteLine($"[InjectActionLayer] Verification: parent=0x{actualParent:X8} (expected 0x{workerw:X8}), injected={injected}");
            return injected;
        }

        /// <summary>
        /// Finds the WorkerW window behind the desktop icons for wallpaper injection.
        /// Uses a cache to avoid repeatedly sending 0x052C to Explorer, which destabilizes
        /// the shell and causes system lag over time.
        /// </summary>
        private IntPtr FindWorkerW()
        {
            // Check cache first — avoid expensive shell IPC on every injection.
            // Validate both visibility AND class name to prevent injecting into a
            // wrong window if the HWND gets reused after a shell restart.
            if (_cachedWorkerW != IntPtr.Zero && Win32Api.IsWindowVisible(_cachedWorkerW))
            {
                var sb = new System.Text.StringBuilder(64);
                Win32Api.GetClassName(_cachedWorkerW, sb, sb.Capacity);
                if (sb.ToString() == "WorkerW")
                {
                    System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Using cached WorkerW=0x{_cachedWorkerW:X8}");
                    return _cachedWorkerW;
                }
                System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Cache invalidated — HWND 0x{_cachedWorkerW:X8} class is '{sb}', not WorkerW");
            }

            _cachedWorkerW = IntPtr.Zero;
            IntPtr result = FindWorkerWCore();
            if (result != IntPtr.Zero)
                System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Caching new WorkerW=0x{result:X8}");
            _cachedWorkerW = result;
            return result;
        }

        /// <summary>
        /// Core WorkerW discovery logic. Called once and cached.
        /// Works on both Windows 10 and Windows 11.
        ///
        /// Desktop shell hierarchy after sending 0x052C to Progman:
        ///
        /// Win10 layout (standard):
        ///   Progman → contains SHELLDLL_DefView (desktop icons)
        ///   WorkerW → wallpaper render target (top-level sibling, created by 0x052C)
        ///
        /// Win11 layout (some builds):
        ///   Progman
        ///     SHELLDLL_DefView → desktop icons (child of Progman)
        ///     WorkerW          → wallpaper render target (CHILD of Progman, not top-level!)
        ///
        /// Win11 layout (other builds):
        ///   WorkerW → contains SHELLDLL_DefView
        ///   WorkerW → wallpaper render target (top-level sibling)
        ///   Progman
        ///
        /// Algorithm:
        ///   1. Send 0x052C to Progman to spawn a WorkerW.
        ///   2. Strategy A: Find top-level window with SHELLDLL_DefView, get next sibling WorkerW.
        ///   3. Strategy B: Find WorkerW as a CHILD of the DefView parent (Win11 Progman-child case).
        ///   4. Strategy C: Retry with delay if neither worked.
        /// </summary>
        private IntPtr FindWorkerWCore()
        {
            IntPtr progman = Win32Api.FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[FindWorkerW] ERROR: Progman not found");
                return IntPtr.Zero;
            }

            System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Progman found: 0x{progman:X8}");

            // Send 0x052C to create WorkerW wallpaper layer
            Win32Api.SendMessageTimeout(progman, 0x052C, new UIntPtr(0xD), new IntPtr(0x1), 0x0, 1000, out _);

            // --- Strategy A: Classic — find top-level window with SHELLDLL_DefView, get next sibling WorkerW ---
            IntPtr workerw = IntPtr.Zero;
            IntPtr defViewParent = IntPtr.Zero;

            Win32Api.EnumWindows((hwnd, lParam) =>
            {
                if (Win32Api.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                {
                    defViewParent = hwnd;
                    workerw = Win32Api.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (workerw != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Strategy A: Found WorkerW=0x{workerw:X8} (sibling of DefView parent=0x{defViewParent:X8})");
                return workerw;
            }

            System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Strategy A failed. DefView parent=0x{defViewParent:X8}");

            // --- Strategy B: Win11 — SHELLDLL_DefView is under Progman, WorkerW is also a child of Progman ---
            if (defViewParent != IntPtr.Zero)
            {
                IntPtr defView = Win32Api.FindWindowEx(defViewParent, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    // Look for WorkerW as the next child after SHELLDLL_DefView
                    workerw = Win32Api.FindWindowEx(defViewParent, defView, "WorkerW", null);
                    if (workerw != IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Strategy B: Found WorkerW=0x{workerw:X8} as child of 0x{defViewParent:X8} (after DefView)");
                        return workerw;
                    }

                    // Also try finding WorkerW as any child (not necessarily after DefView)
                    workerw = Win32Api.FindWindowEx(defViewParent, IntPtr.Zero, "WorkerW", null);
                    if (workerw != IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Strategy B (variant): Found WorkerW=0x{workerw:X8} as child of 0x{defViewParent:X8}");
                        return workerw;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("[FindWorkerW] Strategy B failed");

            // --- Strategy C: Retry — shell may not have processed 0x052C yet ---
            // Use SpinWait instead of Thread.Sleep to avoid blocking the UI thread for 300ms.
            // SpinWait yields the thread without fully sleeping, allowing message pump processing.
            System.Threading.SpinWait.SpinUntil(() => false, 100);
            Win32Api.SendMessageTimeout(progman, 0x052C, new UIntPtr(0xD), new IntPtr(0x1), 0x0, 1000, out _);
            System.Threading.SpinWait.SpinUntil(() => false, 50);

            // Retry Strategy A
            Win32Api.EnumWindows((hwnd, lParam) =>
            {
                if (Win32Api.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                {
                    defViewParent = hwnd;
                    workerw = Win32Api.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (workerw != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Strategy C (retry A): Found WorkerW=0x{workerw:X8}");
                return workerw;
            }

            // Retry Strategy B
            if (defViewParent != IntPtr.Zero)
            {
                workerw = Win32Api.FindWindowEx(defViewParent, IntPtr.Zero, "WorkerW", null);
                if (workerw != IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine($"[FindWorkerW] Strategy C (retry B): Found WorkerW=0x{workerw:X8}");
                    return workerw;
                }
            }

            System.Diagnostics.Debug.WriteLine("[FindWorkerW] ALL STRATEGIES FAILED — WorkerW not found");
            return IntPtr.Zero;
        }

        /// <summary>
        /// Sets up native GDI painting for a window injected into WorkerW.
        /// 
        /// WHY THIS IS NEEDED:
        /// WPF renders via DirectX through DWM (Desktop Window Manager). After SetParent into
        /// WorkerW, DWM doesn't properly redirect the WPF D3D rendering surface — WPF content
        /// (Background="Black", Image controls) is invisible. VLC video shows because HwndHost
        /// creates its own native DirectX surface, bypassing WPF compositor.
        ///
        /// SOLUTION:
        /// Instead of trying to fix WPF's rendering inside WorkerW (which doesn't work reliably
        /// with SoftwareRendering or any other WPF API), we paint directly on the native Win32
        /// window surface using GDI. We hook WM_ERASEBKGND on the HwndSource to fill the
        /// background with black. This uses the same GDI BitBlt path that Windows Explorer itself
        /// uses for WorkerW content.
        ///
        /// For image windows, we additionally hook WM_PAINT to draw the image via System.Drawing.
        /// </summary>
        private static void SetupNativeBlackBackground(IntPtr hwnd)
        {
            try
            {
                var hwndSource = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                if (hwndSource == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SetupNativeBlackBackground] No HwndSource for 0x{hwnd:X8}");
                    return;
                }

                hwndSource.AddHook((IntPtr h, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                {
                    if (msg == Win32Api.WM_ERASEBKGND)
                    {
                        IntPtr hdc = wParam;
                        Win32Api.GetClientRect(h, out var rect);
                        IntPtr brush = Win32Api.GetStockObject(Win32Api.BLACK_BRUSH);
                        Win32Api.FillRect(hdc, ref rect, brush);
                        // Don't DeleteObject on stock objects
                        handled = true;
                        return new IntPtr(1); // Background erased
                    }
                    return IntPtr.Zero;
                });

                // Force an immediate repaint so the black background shows right away
                Win32Api.InvalidateRect(hwnd, IntPtr.Zero, true);

                System.Diagnostics.Debug.WriteLine($"[SetupNativeBlackBackground] Hooked WM_ERASEBKGND on 0x{hwnd:X8}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SetupNativeBlackBackground] Failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up native GDI image painting for an ImagePlayerWindow injected into WorkerW.
        /// This paints the image directly on the Win32 surface via System.Drawing, bypassing
        /// WPF's invisible DirectX rendering.
        /// 
        /// Handles both WM_ERASEBKGND (black background) and WM_PAINT (image drawing).
        /// Also hooks WM_SIZE and WM_DISPLAYCHANGE to repaint on resize.
        /// </summary>
        private static void SetupNativeImagePainting(IntPtr hwnd, ImagePlayerWindow imageWindow)
        {
            try
            {
                var hwndSource = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                if (hwndSource == null) return;

                hwndSource.AddHook((IntPtr h, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                {
                    if (msg == Win32Api.WM_ERASEBKGND)
                    {
                        // Paint black background via GDI (fills letterbox areas)
                        IntPtr hdc = wParam;
                        Win32Api.GetClientRect(h, out var rect);
                        IntPtr brush = Win32Api.GetStockObject(Win32Api.BLACK_BRUSH);
                        Win32Api.FillRect(hdc, ref rect, brush);
                        handled = true;
                        return new IntPtr(1);
                    }

                    if (msg == Win32Api.WM_PAINT)
                    {
                        // Paint the image via GDI+ (System.Drawing)
                        var bitmap = imageWindow.GetGdiBitmap();
                        if (bitmap != null)
                        {
                            Win32Api.BeginPaint(h, out var ps);
                            try
                            {
                                Win32Api.GetClientRect(h, out var clientRect);
                                int clientW = clientRect.right - clientRect.left;
                                int clientH = clientRect.bottom - clientRect.top;

                                if (clientW > 0 && clientH > 0)
                                {
                                    using (var g = System.Drawing.Graphics.FromHdc(ps.hdc))
                                    {
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                        // Calculate UniformToFill scaling (same as WPF's Stretch.UniformToFill)
                                        double scaleX = (double)clientW / bitmap.Width;
                                        double scaleY = (double)clientH / bitmap.Height;
                                        double scale = Math.Max(scaleX, scaleY); // UniformToFill = max

                                        int drawW = (int)(bitmap.Width * scale);
                                        int drawH = (int)(bitmap.Height * scale);
                                        int drawX = (clientW - drawW) / 2;
                                        int drawY = (clientH - drawH) / 2;

                                        // Fill background black first (for any uncovered edges)
                                        g.Clear(System.Drawing.Color.Black);
                                        g.DrawImage(bitmap, drawX, drawY, drawW, drawH);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[NativeImagePaint] Error: {ex.Message}");
                            }
                            finally
                            {
                                Win32Api.EndPaint(h, ref ps);
                            }
                            handled = true;
                            return IntPtr.Zero;
                        }
                    }

                    if (msg == Win32Api.WM_SIZE || msg == Win32Api.WM_DISPLAYCHANGE)
                    {
                        // Force repaint on size change
                        Win32Api.InvalidateRect(h, IntPtr.Zero, true);
                    }

                    return IntPtr.Zero;
                });

                // Force initial paint
                Win32Api.InvalidateRect(hwnd, IntPtr.Zero, true);

                System.Diagnostics.Debug.WriteLine($"[SetupNativeImagePainting] Hooked WM_PAINT+WM_ERASEBKGND on 0x{hwnd:X8}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SetupNativeImagePainting] Failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears WPF's explicit Width/Height after injection so WPF uses the actual
        /// native window size for layout. Without this, WPF might fight SetWindowPos
        /// by trying to maintain the original 1x1 pixel size from CreateHidden*Window.
        /// </summary>
        private static void ClearWpfSizeConstraints(Window window)
        {
            window.ClearValue(Window.WidthProperty);
            window.ClearValue(Window.HeightProperty);
        }

        /// <summary>
        /// Checks whether the desktop is visible at the given screen point.
        /// Uses WindowFromPoint to find the topmost window at the coordinates, then walks
        /// up the parent chain looking for desktop-related windows (WorkerW, Progman, etc.).
        /// Returns false if an application window is at that point (blocking the desktop).
        /// 
        /// This prevents click regions from triggering when the user clicks on a regular
        /// window that happens to overlap with a trigger zone.
        /// </summary>
        private static bool IsDesktopAtPoint(System.Windows.Point screenPoint)
        {
            var pt = new Win32Api.POINT { x = (int)screenPoint.X, y = (int)screenPoint.Y };
            IntPtr hwnd = Win32Api.WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero) return false;

            // Walk up the parent chain — if any ancestor is a desktop-related window,
            // the click is on the desktop area.
            IntPtr current = hwnd;
            while (current != IntPtr.Zero)
            {
                var sb = new System.Text.StringBuilder(64);
                Win32Api.GetClassName(current, sb, sb.Capacity);
                string className = sb.ToString();

                // Desktop-related window classes:
                // "WorkerW"          - wallpaper render target (where our windows are injected)
                // "Progman"          - program manager (desktop host)
                // "SHELLDLL_DefView" - desktop icon container
                // "SysListView32"    - desktop icon list view
                // "#32769"           - desktop window class
                if (className == "WorkerW" || className == "Progman" ||
                    className == "SHELLDLL_DefView" || className == "SysListView32" ||
                    className == "#32769")
                {
                    return true;
                }

                current = Win32Api.GetParent(current);
            }

            return false;
        }

        /// <summary>
        /// Strips borders/popup style and adds WS_CHILD for a window parented into WorkerW.
        /// 
        /// WS_CHILD is the correct Win32 style for a SetParent'd window. Without it,
        /// the window is treated as an "owned" window with independent Z-order, which causes
        /// the second action video to pop out as an independent window (SetParent doesn't
        /// "stick" properly when the window lacks WS_CHILD).
        /// 
        /// WPF rendering inside WorkerW is handled separately by native GDI painting
        /// (SetupNativeBlackBackground / SetupNativeImagePainting) — we don't rely on WPF's
        /// DirectX/DWM pipeline for visibility.
        /// </summary>
        private void RemoveBorderAndSetTransparent(IntPtr hwnd)
        {
            int style = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_STYLE);
            int exStyle = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_EXSTYLE);

            System.Diagnostics.Debug.WriteLine($"[RemoveBorder] Before: style=0x{style:X8}, exStyle=0x{exStyle:X8}");

            // Remove WS_POPUP (incompatible with WS_CHILD) and all border styles.
            // Add WS_CHILD for proper parent-child relationship inside WorkerW.
            // Add WS_VISIBLE so the window is shown.
            style = style & ~Win32Api.WS_POPUP;
            style = style & ~0x00C00000;  // WS_CAPTION
            style = style & ~0x00040000;  // WS_THICKFRAME
            style = style & ~0x00800000;  // WS_BORDER
            style = style | Win32Api.WS_CHILD | Win32Api.WS_VISIBLE;

            // WS_EX_TOOLWINDOW hides from Alt+Tab and taskbar.
            exStyle = exStyle | Win32Api.WS_EX_TOOLWINDOW;

            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_STYLE, style);
            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_EXSTYLE, exStyle);

            System.Diagnostics.Debug.WriteLine($"[RemoveBorder] After: style=0x{style:X8}, exStyle=0x{exStyle:X8}");
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