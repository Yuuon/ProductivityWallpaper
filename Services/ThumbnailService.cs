using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Generates and manages thumbnails for media resources.
    /// Supports temp mode (local editing) and export mode (theme packages).
    /// Uses WPF imaging for image thumbnails and MediaPlayer for video frame extraction.
    /// </summary>
    public class ThumbnailService : IThumbnailService
    {
        private const string TempFolderName = "ProductivityWallpaper";
        private const string ThumbnailsFolderName = "Thumbnails";
        private const string ThumbnailSuffix = "_thumb.jpg";

        /// <inheritdoc/>
        public async Task<string> GenerateThumbnailAsync(ResourceEntry resource, bool forExport = false,
            string? exportFolder = null, CancellationToken ct = default)
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
                forExport, exportFolder, ct);

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
            bool forExport = false, string? exportFolder = null, CancellationToken ct = default)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.WriteLine($"[ThumbnailService] Source file not found: {sourcePath}");
                return string.Empty;
            }

            var outputPath = GetThumbnailPath(resourceId, forExport, exportFolder);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                return resourceType switch
                {
                    MediaType.Image => await GenerateImageThumbnailAsync(sourcePath, outputPath, ct),
                    MediaType.Video => await GenerateVideoThumbnailAsync(sourcePath, outputPath, ct),
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
        public string GetThumbnailPath(string resourceId, bool forExport = false, string? exportFolder = null)
        {
            var filename = $"{resourceId}{ThumbnailSuffix}";

            if (forExport && !string.IsNullOrEmpty(exportFolder))
            {
                return Path.Combine(exportFolder, "thumbnails", filename);
            }

            return Path.Combine(Path.GetTempPath(), TempFolderName, ThumbnailsFolderName, filename);
        }

        /// <inheritdoc/>
        public bool ThumbnailExists(string resourceId, bool forExport = false, string? exportFolder = null)
        {
            var path = GetThumbnailPath(resourceId, forExport, exportFolder);
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public void DeleteThumbnail(string resourceId, bool forExport = false, string? exportFolder = null)
        {
            var path = GetThumbnailPath(resourceId, forExport, exportFolder);
            if (File.Exists(path))
            {
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

        // ==================== Private Generation Methods ====================

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
        /// Generates a thumbnail from a video file by extracting a frame using WPF MediaPlayer.
        /// Must run on the UI thread (STA) for WPF media operations.
        /// </summary>
        private async Task<string> GenerateVideoThumbnailAsync(string sourcePath, string outputPath, CancellationToken ct)
        {
            // Video thumbnail extraction requires STA thread with dispatcher
            // Use a simple approach: try to load as image first (some video containers have embedded thumbnails)
            // If that fails, create a placeholder
            try
            {
                var tcs = new TaskCompletionSource<string>();

                // Create STA thread for WPF media operations
                var thread = new Thread(() =>
                {
                    try
                    {
                        var player = new MediaPlayer();
                        player.Open(new Uri(sourcePath, UriKind.Absolute));
                        player.ScrubbingEnabled = true;

                        // Wait for media to be ready
                        player.MediaOpened += (s, e) =>
                        {
                            try
                            {
                                // Seek to 1 second or 10% of duration
                                if (player.NaturalDuration.HasTimeSpan)
                                {
                                    var seekTime = TimeSpan.FromSeconds(
                                        Math.Min(1, player.NaturalDuration.TimeSpan.TotalSeconds * 0.1));
                                    player.Position = seekTime;
                                }

                                // Wait a moment for the frame to render
                                Thread.Sleep(300);

                                // Render frame to bitmap
                                var width = player.NaturalVideoWidth > 0 ? player.NaturalVideoWidth : IThumbnailService.ThumbnailWidth;
                                var height = player.NaturalVideoHeight > 0 ? player.NaturalVideoHeight : IThumbnailService.ThumbnailHeight;

                                // Scale to thumbnail size
                                var scale = Math.Min(
                                    (double)IThumbnailService.ThumbnailWidth / width,
                                    (double)IThumbnailService.ThumbnailHeight / height);
                                var renderWidth = (int)(width * scale);
                                var renderHeight = (int)(height * scale);

                                var visual = new DrawingVisual();
                                using (var context = visual.RenderOpen())
                                {
                                    context.DrawVideo(player, new Rect(0, 0, renderWidth, renderHeight));
                                }

                                var bitmap = new RenderTargetBitmap(
                                    renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);
                                bitmap.Render(visual);

                                var encoder = new JpegBitmapEncoder
                                {
                                    QualityLevel = IThumbnailService.ThumbnailQuality
                                };
                                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                                using var stream = File.Create(outputPath);
                                encoder.Save(stream);

                                player.Close();
                                tcs.TrySetResult(outputPath);
                            }
                            catch (Exception ex)
                            {
                                player.Close();
                                tcs.TrySetResult(string.Empty);
                                Debug.WriteLine($"[ThumbnailService] Video frame extraction failed: {ex.Message}");
                            }
                        };

                        player.MediaFailed += (s, e) =>
                        {
                            tcs.TrySetResult(string.Empty);
                            Debug.WriteLine($"[ThumbnailService] Media open failed: {e.ErrorException?.Message}");
                        };

                        // Pump messages for a reasonable time
                        System.Windows.Threading.Dispatcher.Run();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetResult(string.Empty);
                        Debug.WriteLine($"[ThumbnailService] STA thread error: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();

                // Wait with timeout
                var result = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10), ct));
                if (result == tcs.Task)
                {
                    return tcs.Task.Result;
                }

                Debug.WriteLine("[ThumbnailService] Video thumbnail generation timed out");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] Video thumbnail error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
