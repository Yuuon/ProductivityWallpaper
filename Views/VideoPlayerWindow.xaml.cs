using LibVLCSharp.Shared;
using System.Windows;

namespace ProductivityWallpaper.Views
{
    public partial class VideoPlayerWindow : Window
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;

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
            _mediaPlayer.Mute = muted;
        }

        /// <summary>
        /// Sets the volume level (0-100).
        /// </summary>
        public void SetVolume(int volume)
        {
            _mediaPlayer.Volume = System.Math.Clamp(volume, 0, 100);
        }

        public void StopAndClose()
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            this.Close();
        }
    }
}