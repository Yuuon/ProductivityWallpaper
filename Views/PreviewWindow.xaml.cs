using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Views
{
    /// <summary>
    /// Preview window for displaying images, videos, and audio files.
    /// </summary>
    public partial class PreviewWindow : Window
    {
        private DispatcherTimer? _positionTimer;
        private bool _isPlaying;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewWindow"/> class.
        /// </summary>
        public PreviewWindow()
        {
            InitializeComponent();
            Loaded += PreviewWindow_Loaded;
            Closed += PreviewWindow_Closed;
        }

        /// <summary>
        /// Handles the Loaded event.
        /// Starts media playback based on the data context.
        /// </summary>
        private void PreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MediaItemModel item)
                return;

            // Set window title
            Title = $"Preview - {item.FileName}";

            // Handle media based on type
            switch (item.Type)
            {
                case MediaFileType.Image:
                    // Image is loaded automatically via binding
                    break;

                case MediaFileType.Video:
                    SetupVideoPlayback(item);
                    break;

                case MediaFileType.Audio:
                    SetupAudioPlayback(item);
                    break;
            }
        }

        /// <summary>
        /// Handles the Closed event.
        /// Cleans up media resources.
        /// </summary>
        private void PreviewWindow_Closed(object? sender, EventArgs e)
        {
            _positionTimer?.Stop();

            // Stop and clean up video
            if (PreviewVideo.Source != null)
            {
                PreviewVideo.Stop();
                PreviewVideo.Source = null;
            }
        }

        /// <summary>
        /// Sets up video playback.
        /// </summary>
        private void SetupVideoPlayback(MediaItemModel item)
        {
            if (!File.Exists(item.FilePath))
                return;

            try
            {
                PreviewVideo.Source = new Uri(item.FilePath);
                PreviewVideo.MediaOpened += Video_MediaOpened;
                PreviewVideo.MediaEnded += Video_MediaEnded;

                // Apply mute setting
                PreviewVideo.IsMuted = item.IsMuted;
                UpdateMuteIcon(item.IsMuted);

                // Start playback
                PreviewVideo.Play();
                _isPlaying = true;
                UpdatePlayPauseIcon();

                // Start position timer
                StartPositionTimer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading video: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up audio playback.
        /// </summary>
        private void SetupAudioPlayback(MediaItemModel item)
        {
            if (!File.Exists(item.FilePath))
                return;

            try
            {
                PreviewVideo.Source = new Uri(item.FilePath);
                PreviewVideo.MediaOpened += Video_MediaOpened;
                PreviewVideo.MediaEnded += Video_MediaEnded;

                // Apply mute setting
                PreviewVideo.IsMuted = item.IsMuted;
                UpdateMuteIcon(item.IsMuted);

                // Start playback
                PreviewVideo.Play();
                _isPlaying = true;
                UpdatePlayPauseIcon();

                // Start position timer
                StartPositionTimer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the MediaOpened event.
        /// Updates the total time display.
        /// </summary>
        private void Video_MediaOpened(object? sender, RoutedEventArgs e)
        {
            if (PreviewVideo.NaturalDuration.HasTimeSpan)
            {
                var duration = PreviewVideo.NaturalDuration.TimeSpan;
                Dispatcher.Invoke(() =>
                {
                    TotalTimeText.Text = FormatTime(duration);
                });
            }
        }

        /// <summary>
        /// Handles the MediaEnded event.
        /// Resets playback to the beginning.
        /// </summary>
        private void Video_MediaEnded(object? sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            UpdatePlayPauseIcon();
            PreviewVideo.Position = TimeSpan.Zero;
        }

        /// <summary>
        /// Starts the position update timer.
        /// </summary>
        private void StartPositionTimer()
        {
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionTimer.Tick += PositionTimer_Tick;
            _positionTimer.Start();
        }

        /// <summary>
        /// Handles the position timer tick.
        /// Updates the current time display.
        /// </summary>
        private void PositionTimer_Tick(object? sender, EventArgs e)
        {
            if (PreviewVideo.Source != null)
            {
                CurrentTimeText.Text = FormatTime(PreviewVideo.Position);
            }
        }

        /// <summary>
        /// Formats a TimeSpan for display.
        /// </summary>
        private static string FormatTime(TimeSpan time)
        {
            if (time.Hours > 0)
                return $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}";
            return $"{time.Minutes}:{time.Seconds:D2}";
        }

        /// <summary>
        /// Updates the play/pause button icon based on playback state.
        /// </summary>
        private void UpdatePlayPauseIcon()
        {
            if (_isPlaying)
            {
                PlayIcon.Visibility = Visibility.Collapsed;
                PauseIcon.Visibility = Visibility.Visible;
            }
            else
            {
                PlayIcon.Visibility = Visibility.Visible;
                PauseIcon.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the mute button icon based on mute state.
        /// </summary>
        private void UpdateMuteIcon(bool isMuted)
        {
            if (isMuted)
            {
                MutedIcon.Visibility = Visibility.Visible;
                UnmutedIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                MutedIcon.Visibility = Visibility.Collapsed;
                UnmutedIcon.Visibility = Visibility.Visible;
            }
        }

        #region Window Control Event Handlers

        /// <summary>
        /// Handles the minimize button click.
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles the maximize/restore button click.
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeIcon.Data = Geometry.Parse("M0 0H10V10H0V0ZM1 1V9H9V1H1Z");
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeIcon.Data = Geometry.Parse("M0 2H8V10H0V2ZM1 3V9H7V3H1ZM2 0V1H10V9H11V0H2Z");
            }
        }

        /// <summary>
        /// Handles the close button click.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the play/pause button click.
        /// </summary>
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewVideo.Source == null)
                return;

            if (_isPlaying)
            {
                PreviewVideo.Pause();
                _isPlaying = false;
            }
            else
            {
                PreviewVideo.Play();
                _isPlaying = true;
            }

            UpdatePlayPauseIcon();
        }

        /// <summary>
        /// Handles the stop button click.
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewVideo.Source == null)
                return;

            PreviewVideo.Stop();
            _isPlaying = false;
            UpdatePlayPauseIcon();
            CurrentTimeText.Text = "0:00";
        }

        /// <summary>
        /// Handles the mute button click.
        /// </summary>
        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            PreviewVideo.IsMuted = !PreviewVideo.IsMuted;
            UpdateMuteIcon(PreviewVideo.IsMuted);

            // Update the model if available
            if (DataContext is MediaItemModel item)
            {
                item.IsMuted = PreviewVideo.IsMuted;
            }
        }

        #endregion
    }
}
