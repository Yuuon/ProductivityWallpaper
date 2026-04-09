using LibVLCSharp.Shared;
using System.Windows;

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
        /// Detaches the MediaPlayer from the VideoView first to prevent AccessViolationException,
        /// then stops and disposes on a background thread to avoid native callback conflicts.
        /// </summary>
        public void StopAndClose()
        {
            if (_isStopping) return;
            _isStopping = true;

            // Capture and null-out references to prevent concurrent access
            var player = _mediaPlayer;
            var libvlc = _libVLC;
            _mediaPlayer = null;
            _libVLC = null;

            // Detach MediaPlayer from the VideoView FIRST — this breaks the native rendering link
            // before we attempt to stop/dispose, preventing AccessViolationException
            try { VideoView.MediaPlayer = null; } catch { }

            // Stop and dispose on ThreadPool to avoid calling Stop() from a VLC callback thread
            // which is the primary cause of AccessViolationException
            if (player != null)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { player.Stop(); } catch { }
                    try { player.Dispose(); } catch { }
                    try { libvlc?.Dispose(); } catch { }
                });
            }

            this.Close();
        }
    }
}