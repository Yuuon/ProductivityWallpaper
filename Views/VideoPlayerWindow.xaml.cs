using LibVLCSharp.Shared;
using System.Windows;

namespace ProductivityWallpaper.Views
{
    public partial class VideoPlayerWindow : Window
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;

        public VideoPlayerWindow(string videoPath)
        {
            InitializeComponent();

            // 初始化 VLC 核心
            // 确保 App.xaml.cs 中已经调用了 Core.Initialize()，或者在这里做检查
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            
            // 绑定播放器到视图
            VideoView.MediaPlayer = _mediaPlayer;

            // 设置循环播放
            // VLC 的循环通常通过 Media 的选项或事件处理来实现，这里用简单的 Repeat 参数
            var media = new Media(_libVLC, new Uri(videoPath));
            media.AddOption("input-repeat=65535"); // 简单暴力的循环方式
            
            _mediaPlayer.Play(media);

            // 静音 (壁纸通常不需要声音，或者未来做成可配置)
            _mediaPlayer.Mute = true;
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