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
        /// 4. Close the window and dispose resources
        /// 
        /// Setting VideoView.MediaPlayer = null while VLC is still rendering causes
        /// AccessViolationException that CANNOT be caught in .NET 8 (corrupted state exception).
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

            // STEP 2-4: All VLC cleanup on ThreadPool to avoid native callback thread conflicts
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                // STEP 2: Stop playback (blocks until VLC native rendering thread stops)
                try { player?.Stop(); } catch { }

                // Brief pause for native thread to fully wind down
                System.Threading.Thread.Sleep(50);

                // STEP 3: Now VLC is stopped — safe to detach VideoView and close window on UI thread
                try
                {
                    Application.Current?.Dispatcher?.BeginInvoke(() =>
                    {
                        try { VideoView.MediaPlayer = null; } catch { }
                        try { this.Close(); } catch { }
                    });
                }
                catch { /* App may be shutting down */ }

                // STEP 4: Dispose player resources
                try { player?.Dispose(); } catch { }
                try { libvlc?.Dispose(); } catch { }
            });
        }
    }
}