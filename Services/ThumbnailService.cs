using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FFMpegCore;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Generates and manages thumbnails for media resources.
    /// All thumbnails are stored in the theme's thumbnails/ subfolder — never in %Temp%.
    /// Uses FFMpegCore NuGet package for animated GIF generation from video files.
    /// Falls back to WPF MediaPlayer frame extraction only when FFmpeg is unavailable.
    /// </summary>
    public class ThumbnailService : IThumbnailService
    {
        private const string ThumbnailsFolderName = "thumbnails";
        private const string JpegThumbnailSuffix = "_thumb.jpg";
        private const string GifThumbnailSuffix = "_thumb.gif";
        private const int GifMaxDurationSeconds = 5;
        private bool? _ffmpegAvailable;

        // ==================== Public API ====================

        /// <inheritdoc/>
        public async Task<string> GenerateThumbnailAsync(ResourceEntry resource, string themeFolderPath,
            CancellationToken ct = default)
        {
            var sourcePath = !string.IsNullOrEmpty(resource.SourcePath)
                ? resource.SourcePath
                : string.Empty;

            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.WriteLine($"[ThumbnailService] No source path for resource {resource.Id}");
                return string.Empty;
            }

            var thumbPath = await GenerateThumbnailAsync(sourcePath, resource.Id, resource.Type,
                themeFolderPath, ct);

            // Update resource thumbnail path
            if (!string.IsNullOrEmpty(thumbPath))
            {
                resource.ThumbnailPath = thumbPath;
                resource.ThumbnailFileName = Path.GetFileName(thumbPath);
            }

            return thumbPath;
        }

        /// <inheritdoc/>
        public async Task<string> GenerateThumbnailAsync(string sourcePath, string resourceId, MediaType resourceType,
            string themeFolderPath, CancellationToken ct = default)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.WriteLine($"[ThumbnailService] Source file not found: {sourcePath}");
                return string.Empty;
            }

            // Ensure thumbnails directory exists in theme folder
            var thumbnailsDir = Path.Combine(themeFolderPath, ThumbnailsFolderName);
            if (!Directory.Exists(thumbnailsDir))
                Directory.CreateDirectory(thumbnailsDir);

            try
            {
                return resourceType switch
                {
                    MediaType.Image => await GenerateImageThumbnailAsync(sourcePath,
                        GetThumbnailPath(resourceId, themeFolderPath, isVideo: false), ct),
                    MediaType.Video => await GenerateVideoGifThumbnailAsync(sourcePath, resourceId, themeFolderPath, ct),
                    MediaType.Audio => string.Empty, // Audio has no thumbnail
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] Error generating thumbnail for {sourcePath}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public string GetThumbnailPath(string resourceId, string themeFolderPath, bool isVideo = false)
        {
            var suffix = isVideo ? GifThumbnailSuffix : JpegThumbnailSuffix;
            var filename = $"{resourceId}{suffix}";
            return Path.Combine(themeFolderPath, ThumbnailsFolderName, filename);
        }

        /// <inheritdoc/>
        public string GetGifThumbnailPath(string resourceId, string themeFolderPath)
        {
            return GetThumbnailPath(resourceId, themeFolderPath, isVideo: true);
        }

        /// <inheritdoc/>
        public bool ThumbnailExists(string resourceId, string themeFolderPath, bool isVideo = false)
        {
            var path = GetThumbnailPath(resourceId, themeFolderPath, isVideo);
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public void DeleteThumbnail(string resourceId, string themeFolderPath)
        {
            // Try deleting both JPEG and GIF variants
            var jpegPath = GetThumbnailPath(resourceId, themeFolderPath, isVideo: false);
            TryDeleteFile(jpegPath);

            var gifPath = GetThumbnailPath(resourceId, themeFolderPath, isVideo: true);
            TryDeleteFile(gifPath);
        }

        /// <inheritdoc/>
        public bool IsFFmpegAvailable()
        {
            if (_ffmpegAvailable.HasValue)
                return _ffmpegAvailable.Value;

            try
            {
                // Use FFMpegCore's built-in binary detection
                var ffmpegPath = GlobalFFOptions.GetFFMpegBinaryPath();
                _ffmpegAvailable = !string.IsNullOrEmpty(ffmpegPath) && File.Exists(ffmpegPath);
            }
            catch
            {
                // Fallback: try to run ffmpeg directly
                try
                {
                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit(5000);
                    _ffmpegAvailable = process.ExitCode == 0;
                }
                catch
                {
                    _ffmpegAvailable = false;
                }
            }

            Debug.WriteLine($"[ThumbnailService] FFmpeg available: {_ffmpegAvailable.Value}");
            return _ffmpegAvailable.Value;
        }

        /// <inheritdoc/>
        public async Task<string> GenerateVideoGifThumbnailAsync(string sourcePath, string resourceId,
            string themeFolderPath, CancellationToken ct = default)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.WriteLine($"[ThumbnailService] Source file not found: {sourcePath}");
                return string.Empty;
            }

            var outputPath = GetGifThumbnailPath(resourceId, themeFolderPath);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // If GIF already exists, return it
            if (File.Exists(outputPath))
            {
                Debug.WriteLine($"[ThumbnailService] GIF thumbnail already exists: {outputPath}");
                return outputPath;
            }

            if (!IsFFmpegAvailable())
            {
                Debug.WriteLine("[ThumbnailService] FFmpeg not available — falling back to JPEG frame extraction.");
                return await GenerateVideoJpegFallbackAsync(sourcePath, resourceId, themeFolderPath, ct);
            }

            try
            {
                return await GenerateGifViaFFMpegCoreAsync(sourcePath, outputPath, ct);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] GIF generation failed: {ex.Message}");
                // Clean up partial file if it exists
                TryDeleteFile(outputPath);
                // Fall back to JPEG frame extraction
                Debug.WriteLine("[ThumbnailService] Falling back to JPEG frame extraction.");
                return await GenerateVideoJpegFallbackAsync(sourcePath, resourceId, themeFolderPath, ct);
            }
        }

        // ==================== Private Generation Methods ====================

        /// <summary>
        /// Generates an animated GIF from a video using FFMpegCore NuGet package.
        /// Creates a looping GIF from the first 5 seconds at thumbnail width, maintaining aspect ratio.
        /// </summary>
        private static async Task<string> GenerateGifViaFFMpegCoreAsync(string sourcePath, string outputPath, CancellationToken ct)
        {
            // Use FFMpegCore's built-in GifSnapshot API:
            // - size: thumbnail width, auto height (maintains aspect ratio with -1)
            // - captureTime: start from beginning
            // - duration: max 5 seconds
            var size = new System.Drawing.Size(IThumbnailService.ThumbnailWidth, -1);
            var captureTime = TimeSpan.Zero;
            var duration = TimeSpan.FromSeconds(GifMaxDurationSeconds);

            var success = await FFMpeg.GifSnapshotAsync(
                sourcePath, outputPath, size, captureTime, duration, streamIndex: null, ct);

            if (success && File.Exists(outputPath))
            {
                Debug.WriteLine($"[ThumbnailService] Generated GIF thumbnail via FFMpegCore: {outputPath}");
                return outputPath;
            }

            Debug.WriteLine("[ThumbnailService] FFMpegCore GifSnapshot returned false or file not created");
            return string.Empty;
        }

        /// <summary>
        /// Generates a thumbnail from an image file using WPF imaging.
        /// Preserves aspect ratio, fits within ThumbnailWidth x ThumbnailHeight.
        /// </summary>
        private Task<string> GenerateImageThumbnailAsync(string sourcePath, string outputPath, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(sourcePath, UriKind.Absolute);
                bitmap.DecodePixelWidth = IThumbnailService.ThumbnailWidth;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Encode as JPEG
                var encoder = new JpegBitmapEncoder
                {
                    QualityLevel = IThumbnailService.ThumbnailQuality
                };
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var stream = File.Create(outputPath);
                encoder.Save(stream);

                Debug.WriteLine($"[ThumbnailService] Generated image thumbnail: {outputPath}");
                return outputPath;
            }, ct);
        }

        /// <summary>
        /// Fallback: extracts a single frame from a video as a JPEG thumbnail using WPF MediaPlayer.
        /// Used when FFmpeg is not available or GIF generation fails.
        /// </summary>
        private async Task<string> GenerateVideoJpegFallbackAsync(string sourcePath, string resourceId,
            string themeFolderPath, CancellationToken ct)
        {
            var jpegOutputPath = GetThumbnailPath(resourceId, themeFolderPath, isVideo: false);

            // If JPEG thumbnail already exists, return it
            if (File.Exists(jpegOutputPath))
            {
                Debug.WriteLine($"[ThumbnailService] JPEG fallback thumbnail already exists: {jpegOutputPath}");
                return jpegOutputPath;
            }

            try
            {
                var tcs = new TaskCompletionSource<string>();

                // MediaPlayer must be created on an STA thread
                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        var player = new System.Windows.Media.MediaPlayer();
                        player.ScrubbingEnabled = true;
                        player.Volume = 0;

                        player.MediaOpened += (s, e) =>
                        {
                            try
                            {
                                // Seek to 1 second in for a representative frame
                                player.Position = TimeSpan.FromSeconds(1);
                            }
                            catch
                            {
                                tcs.TrySetResult(string.Empty);
                            }
                        };

                        player.MediaFailed += (s, e) =>
                        {
                            Debug.WriteLine($"[ThumbnailService] MediaPlayer failed: {e.ErrorException?.Message}");
                            tcs.TrySetResult(string.Empty);
                        };

                        // Use a timer to capture frame after seek
                        var timer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(500)
                        };
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            try
                            {
                                if (player.NaturalVideoWidth > 0 && player.NaturalVideoHeight > 0)
                                {
                                    // Calculate thumbnail dimensions maintaining aspect ratio
                                    double scale = (double)IThumbnailService.ThumbnailWidth / player.NaturalVideoWidth;
                                    int thumbWidth = IThumbnailService.ThumbnailWidth;
                                    int thumbHeight = (int)(player.NaturalVideoHeight * scale);

                                    var rtb = new RenderTargetBitmap(thumbWidth, thumbHeight, 96, 96,
                                        System.Windows.Media.PixelFormats.Pbgra32);

                                    var dv = new System.Windows.Media.DrawingVisual();
                                    using (var dc = dv.RenderOpen())
                                    {
                                        dc.DrawVideo(player,
                                            new System.Windows.Rect(0, 0, thumbWidth, thumbHeight));
                                    }
                                    rtb.Render(dv);

                                    var encoder = new JpegBitmapEncoder
                                    {
                                        QualityLevel = IThumbnailService.ThumbnailQuality
                                    };
                                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                                    // Ensure directory exists
                                    var dir = Path.GetDirectoryName(jpegOutputPath);
                                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                                        Directory.CreateDirectory(dir);

                                    using var stream = File.Create(jpegOutputPath);
                                    encoder.Save(stream);

                                    Debug.WriteLine($"[ThumbnailService] Generated JPEG fallback thumbnail: {jpegOutputPath}");
                                    tcs.TrySetResult(jpegOutputPath);
                                }
                                else
                                {
                                    Debug.WriteLine("[ThumbnailService] MediaPlayer reported zero video dimensions");
                                    tcs.TrySetResult(string.Empty);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[ThumbnailService] JPEG fallback frame capture error: {ex.Message}");
                                tcs.TrySetResult(string.Empty);
                            }
                            finally
                            {
                                player.Close();
                                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(
                                    System.Windows.Threading.DispatcherPriority.Background);
                            }
                        };

                        player.Open(new Uri(sourcePath, UriKind.Absolute));
                        timer.Start();

                        // Run dispatcher to process events
                        System.Windows.Threading.Dispatcher.Run();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ThumbnailService] JPEG fallback thread error: {ex.Message}");
                        tcs.TrySetResult(string.Empty);
                    }
                });

                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();

                // Wait with a timeout
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(10000, ct));
                if (completedTask == tcs.Task)
                {
                    return tcs.Task.Result;
                }
                else
                {
                    Debug.WriteLine("[ThumbnailService] JPEG fallback timed out");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] JPEG fallback error: {ex.Message}");
                return string.Empty;
            }
        }

        // ==================== Utility ====================

        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            try
            {
                File.Delete(path);
                Debug.WriteLine($"[ThumbnailService] Deleted thumbnail: {path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] Error deleting thumbnail: {ex.Message}");
            }
        }
    }
}
