using LibVLCSharp.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;

namespace ProductivityWallpaper.Views
{
    public partial class VideoPlayerWindow : Window
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private bool _isStopping;

        public VideoPlayerWindow(string videoPath, bool muted = true)
        {
            InitializeComponent();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            
            VideoView.MediaPlayer = _mediaPlayer;

            var media = new Media(_libVLC, new Uri(videoPath));
            media.AddOption("input-repeat=65535");
            
            _mediaPlayer.Play(media);
            _mediaPlayer.Mute = muted;
        }

        /// <summary>
        /// Sets the mute state for video audio.
        /// </summary>
        public void SetMute(bool muted)
        {
            try { if (_mediaPlayer != null) _mediaPlayer.Mute = muted; } catch { }
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