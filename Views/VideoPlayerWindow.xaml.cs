using LibVLCSharp.Shared;
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
        /// Safely stops VLC playback and closes the window.
        /// 
        /// CRITICAL: The sequence must be:
        /// 1. Hide window (stops native VLC rendering demand)
        /// 2. Stop VLC on ThreadPool (NOT on UI thread — VLC callbacks fire on native threads,
        ///    calling Stop from UI while native thread renders causes AccessViolationException)
        /// 3. Only AFTER Stop() returns, detach VideoView.MediaPlayer on UI thread
        /// 4. WAIT for UI thread to finish step 3 before proceeding
        /// 5. Only AFTER VideoView is detached, dispose player resources
        /// 
        /// Steps 4-5 are critical: BeginInvoke is non-blocking, so without waiting,
        /// player.Dispose() runs while VideoView.MediaPlayer still references the player.
        /// VLC's native renderer tries to access the freed surface → AccessViolationException.
        /// This causes "freeze and crash a few seconds after action video ends."
        /// </summary>
        public void StopAndClose()
        {
            if (_isStopping) return;
            _isStopping = true;

            // Capture and null-out references to prevent concurrent access from VLC callbacks
            var player = _mediaPlayer;
            var libvlc = _libVLC;
            _mediaPlayer = null;
            _libVLC = null;

            // STEP 1: Hide window immediately — this tells the native rendering pipeline
            // there is no visible surface, reducing the chance of native thread conflicts
            try { this.Visibility = Visibility.Hidden; } catch { }

            // STEP 2-5: All VLC cleanup on ThreadPool to avoid native callback thread conflicts
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                // STEP 2: Stop playback (blocks until VLC native rendering thread stops)
                try { player?.Stop(); } catch { }

                // Brief pause for native thread to fully wind down
                System.Threading.Thread.Sleep(50);

                // STEP 3-4: Detach VideoView and close window on UI thread, then WAIT for completion
                var uiCleanupDone = new System.Threading.ManualResetEventSlim(false);
                try
                {
                    Application.Current?.Dispatcher?.BeginInvoke(() =>
                    {
                        try { VideoView.MediaPlayer = null; } catch { }
                        try { this.Close(); } catch { }
                        uiCleanupDone.Set();
                    });

                    // Wait for UI thread to finish detaching VideoView.MediaPlayer.
                    // Without this wait, Dispose below would run while VLC's native renderer
                    // still references the player surface → AccessViolationException.
                    uiCleanupDone.Wait(3000);
                }
                catch
                {
                    // App may be shutting down — UI thread not available
                    uiCleanupDone.Set();
                }

                // STEP 5: Dispose player resources — safe now because VideoView is detached
                try { player?.Dispose(); } catch { }
                try { libvlc?.Dispose(); } catch { }
            });
        }
    }
}