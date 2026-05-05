using LibVLCSharp.Shared;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace ProductivityWallpaper.Views
{
    public partial class VideoPlayerWindow : Window
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private bool _isStopping;

        // Native black backdrop child HWND. Required because on Win11 24H2 raised desktop
        // (Progman with WS_EX_NOREDIRECTIONBITMAP), WPF-painted Background="Black" is not
        // reliably composited for windows reparented under Progman, so any pixels NOT
        // covered by VLC's child HWND (e.g. letterbox edges in Center mode) fall through
        // to the OS desktop wallpaper. This native HWND paints real black pixels.
        private readonly BlackBackdropHost _backdrop = new();

        public VideoPlayerWindow(string videoPath, bool muted = true)
        {
            InitializeComponent();
            HookBackdropLifecycle();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            
            VideoView.MediaPlayer = _mediaPlayer;

            var media = new Media(_libVLC, new Uri(videoPath));
            media.AddOption("input-repeat=65535");
            
            _mediaPlayer.Play(media);
            _mediaPlayer.Mute = muted;
        }

        /// <summary>
        /// Image-mode constructor. Renders a static image through VLC's DirectX child HWND
        /// so that on Win11 24H2 raised desktop (where Progman has WS_EX_NOREDIRECTIONBITMAP and
        /// WPF's redirection bitmap is not composited for child windows), the image actually
        /// becomes visible — same render path that already works for video.
        ///
        /// Uses --image-duration=-1 (infinite) so VLC keeps the image surface alive forever.
        /// </summary>
        public static VideoPlayerWindow CreateForImage(string imagePath)
        {
            return new VideoPlayerWindow(imagePath, ImageMarker.Instance);
        }

        // Private marker type used purely to disambiguate the image-mode constructor
        // overload from the existing public (string, bool) video constructor — both
        // would otherwise share a (string, bool) signature and cause CS0111.
        private sealed class ImageMarker { public static readonly ImageMarker Instance = new(); }

        private VideoPlayerWindow(string mediaPath, ImageMarker _)
        {
            InitializeComponent();
            HookBackdropLifecycle();

            // For image mode we instruct VLC to keep the image displayed indefinitely.
            // image-duration is honored by the image demuxer; -1 = forever.
            _libVLC = new LibVLC("--image-duration=-1", "--no-audio");

            _mediaPlayer = new MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;

            var media = new Media(_libVLC, new Uri(mediaPath));
            // Belt-and-suspenders: also set the option on the Media itself so it works
            // regardless of whether the LibVLC-wide option is honored by this build.
            media.AddOption(":image-duration=-1");
            media.AddOption(":no-audio");
            // Loop just in case VLC ends-of-stream the still image.
            media.AddOption("input-repeat=65535");

            _mediaPlayer.Play(media);
            _mediaPlayer.Mute = true;
        }

        // --- Native black backdrop wiring ---

        private void HookBackdropLifecycle()
        {
            SourceInitialized += OnSourceInitializedAttachBackdrop;
            SizeChanged += OnSizeChangedResizeBackdrop;
            Loaded += OnLoadedReassertBackdropZOrder;
            Closed += OnClosedDisposeBackdrop;
        }

        private void OnSourceInitializedAttachBackdrop(object? sender, EventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                _backdrop.Attach(hwnd);
            }
            catch { /* non-fatal: window simply lacks the backdrop */ }
        }

        private void OnSizeChangedResizeBackdrop(object? sender, SizeChangedEventArgs e)
        {
            try { _backdrop.Resize(); } catch { }
        }

        private void OnLoadedReassertBackdropZOrder(object? sender, RoutedEventArgs e)
        {
            // VLC's HwndHost child HWND is created lazily after the window appears.
            // Defer to after layout so VideoView's child HWND already exists, then
            // resize the backdrop and push it back to HWND_BOTTOM so VLC paints on top.
            Dispatcher.BeginInvoke(new Action(() => { try { _backdrop.Resize(); } catch { } }),
                                   DispatcherPriority.Background);
        }

        private void OnClosedDisposeBackdrop(object? sender, EventArgs e)
        {
            try { _backdrop.Dispose(); } catch { }
        }

        /// <summary>
        /// Forces the native black backdrop to fill the parent's current client rect and
        /// reasserts <c>HWND_BOTTOM</c> z-order. Call this from <see cref="Services.WallpaperService"/>
        /// AFTER the window is restored from its offscreen anti-flicker init position to its
        /// final on-screen size, since the backdrop is sized at <c>SourceInitialized</c> when
        /// the parent is still 1×1 and offscreen.
        /// </summary>
        public void ReassertBackdrop()
        {
            try { _backdrop.Resize(); } catch { }
        }

        /// <summary>
        /// Sets the mute state for video audio.
        /// </summary>
        public void SetMute(bool muted)
        {
            try { if (_mediaPlayer != null) _mediaPlayer.Mute = muted; } catch { }
        }

        /// <summary>
        /// Forces VLC to fill the entire window with video pixels (no letterbox bars).
        /// On Win11 24H2 raised desktop, the WPF Grid behind VideoView is not composited
        /// (Progman has WS_EX_NOREDIRECTIONBITMAP), so any uncovered pixels show through as
        /// transparent gaps revealing the OS desktop wallpaper. Eliminating letterbox via
        /// VLC aspect override guarantees VLC paints every pixel.
        ///
        /// Slight aspect distortion is the trade-off but matches the user constraint:
        /// "A general non-transparent black background for the video window should be fine."
        /// — the goal is no transparent gaps, not pixel-perfect aspect preservation.
        /// </summary>
        public void SetAspectFill(int screenWidth, int screenHeight)
        {
            try
            {
                if (_mediaPlayer == null) return;
                // VLC accepts "W:H" strings; setting it to the window aspect makes VLC
                // stretch the video to exactly fill the surface without letterboxing.
                _mediaPlayer.AspectRatio = $"{screenWidth}:{screenHeight}";
            }
            catch { }
        }

        /// <summary>
        /// Applies a per-item <see cref="DisplayMode"/> to the underlying VLC surface.
        /// <para>
        /// Fill   → forces VLC to stretch to the window aspect (no letterbox; matches
        ///          legacy <see cref="SetAspectFill(int, int)"/> behavior).
        /// </para>
        /// <para>
        /// Center → clears VLC's aspect override so VLC preserves the source aspect
        ///          ratio and letterboxes inside the surface. The opaque
        ///          <c>Background="Black"</c> on both <see cref="Window"/> and the inner
        ///          <c>Grid</c> guarantees the uncovered edges paint solid black on the
        ///          Win11 24H2 raised desktop where Progman has WS_EX_NOREDIRECTIONBITMAP.
        /// </para>
        /// <para>
        /// Tile   → not supported for video by product spec; falls back to Fill so
        ///          accidental selection (e.g. legacy data) never produces a transparent
        ///          surface. Tile is enforced as image-only by <see cref="MediaItemModel"/>.
        /// </para>
        /// <paramref name="screenWidth"/> and <paramref name="screenHeight"/> are the
        /// destination surface dimensions and are only consumed in Fill mode.
        /// </summary>
        public void ApplyDisplayMode(DisplayMode mode, int screenWidth, int screenHeight)
        {
            try
            {
                if (_mediaPlayer == null) return;

                switch (mode)
                {
                    case DisplayMode.Center:
                        // Clear aspect override → VLC preserves source AR and letterboxes.
                        // Empty string is the documented "use source aspect" sentinel for
                        // libvlc's video_set_aspect_ratio API.
                        _mediaPlayer.AspectRatio = string.Empty;
                        break;

                    case DisplayMode.Tile:
                    case DisplayMode.Fill:
                    default:
                        _mediaPlayer.AspectRatio = $"{screenWidth}:{screenHeight}";
                        break;
                }
            }
            catch { }
        }

        /// <summary>
        /// Sets the volume level (0-100).
        /// </summary>
        public void SetVolume(int volume)
        {
            try { if (_mediaPlayer != null) _mediaPlayer.Volume = System.Math.Clamp(volume, 0, 100); } catch { }
        }

        /// <summary>
        /// Pauses or resumes video playback.
        /// Used by PlaybackMonitorService for fullscreen auto-pause.
        /// </summary>
        public void SetPause(bool pause)
        {
            try { _mediaPlayer?.SetPause(pause); } catch { }
        }

        /// <summary>
        /// Safely stops VLC playback and closes the window.
        /// Returns a Task that completes only when VLC is fully stopped and the window is closed.
        /// 
        /// CRITICAL SEQUENCE:
        /// 1. Hide window first (stops native VLC rendering demand immediately)
        /// 2. Stop VLC on ThreadPool (NOT on UI thread — VLC callbacks fire on native threads,
        ///    calling Stop from UI while native thread renders causes AccessViolationException)
        /// 3. Wait for native rendering thread to fully wind down (50ms safety margin)
        /// 4. Only AFTER Stop() returns, detach VideoView.MediaPlayer on UI thread
        /// 5. Close the window and dispose VLC resources
        /// 
        /// The Task-based design ensures callers can properly await VLC shutdown before
        /// creating new windows, preventing race conditions that cause AccessViolationException.
        /// </summary>
        public Task StopAndCloseAsync()
        {
            if (_isStopping) return Task.CompletedTask;
            _isStopping = true;

            // Capture and null-out references IMMEDIATELY to prevent concurrent access from VLC callbacks
            var player = _mediaPlayer;
            var libvlc = _libVLC;
            _mediaPlayer = null;
            _libVLC = null;

            // STEP 1: Hide window immediately on UI thread — this tells the native rendering
            // pipeline there is no visible surface, greatly reducing native thread conflicts
            try { this.Visibility = Visibility.Hidden; } catch { }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // STEP 2-5: All VLC cleanup on ThreadPool to avoid native callback thread conflicts
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // STEP 2: Stop playback (blocks until VLC native rendering thread stops)
                    try { player?.Stop(); } catch { }

                    // STEP 3: Safety margin for native thread to fully wind down
                    Thread.Sleep(50);

                    // STEP 4: Detach VideoView and close window on UI thread (must be synchronous
                    // within this callback to guarantee ordering before Dispose)
                    try
                    {
                        var dispatcher = Application.Current?.Dispatcher;
                        if (dispatcher != null && !dispatcher.HasShutdownStarted)
                        {
                            dispatcher.Invoke(() =>
                            {
                                try { VideoView.MediaPlayer = null; } catch { }
                                try { this.Close(); } catch { }
                            });
                        }
                    }
                    catch { /* App may be shutting down */ }

                    // STEP 5: Dispose player resources (after VideoView is detached)
                    try { player?.Dispose(); } catch { }
                    try { libvlc?.Dispose(); } catch { }
                }
                finally
                {
                    tcs.TrySetResult();
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Fire-and-forget version for cleanup paths that don't need to await completion.
        /// Prefer StopAndCloseAsync() in video transition scenarios where timing matters.
        /// </summary>
        public void StopAndClose()
        {
            _ = StopAndCloseAsync();
        }
    }
}