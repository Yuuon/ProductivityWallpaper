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
    /// Uses WPF imaging for image thumbnails, and FFmpeg for animated video GIF thumbnails.
    /// Falls back to WPF MediaPlayer frame extraction when FFmpeg is unavailable.
    /// </summary>
    public class ThumbnailService : IThumbnailService
    {
        private const string TempFolderName = "ProductivityWallpaper";
        private const string ThumbnailsFolderName = "Thumbnails";
        private const string ThumbnailSuffix = "_thumb.jpg";
        private const string GifThumbnailSuffix = "_thumb.gif";
        private const int GifMaxDurationSeconds = 5;
        private const int GifFps = 8;
        private bool? _ffmpegAvailable;

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
        public string GetGifThumbnailPath(string resourceId, string? themeFolderPath = null)
        {
            var filename = $"{resourceId}{GifThumbnailSuffix}";
            
            if (!string.IsNullOrEmpty(themeFolderPath))
            {
                return Path.Combine(themeFolderPath, "thumbnails", filename);
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

            // Also delete GIF thumbnail from temp folder if it exists
            var gifPath = GetGifThumbnailPath(resourceId);
            if (File.Exists(gifPath))
            {
                try
                {
                    File.Delete(gifPath);
                    Debug.WriteLine($"[ThumbnailService] Deleted GIF thumbnail: {gifPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ThumbnailService] Error deleting GIF thumbnail: {ex.Message}");
                }
            }

            // Also delete GIF thumbnail from theme folder if it exists
            if (!string.IsNullOrEmpty(exportFolder))
            {
                var themeGifPath = GetGifThumbnailPath(resourceId, exportFolder);
                if (File.Exists(themeGifPath))
                {
                    try
                    {
                        File.Delete(themeGifPath);
                        Debug.WriteLine($"[ThumbnailService] Deleted theme GIF thumbnail: {themeGifPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ThumbnailService] Error deleting theme GIF thumbnail: {ex.Message}");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool IsFFmpegAvailable()
        {
            if (_ffmpegAvailable.HasValue)
                return _ffmpegAvailable.Value;

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

            Debug.WriteLine($"[ThumbnailService] FFmpeg available: {_ffmpegAvailable.Value}");
            return _ffmpegAvailable.Value;
        }

        /// <inheritdoc/>
        public async Task<string> GenerateVideoGifThumbnailAsync(string sourcePath, string resourceId,
            string? themeFolderPath = null, CancellationToken ct = default)
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
                Debug.WriteLine("[ThumbnailService] FFmpeg not available, falling back to static thumbnail");
                // Fall back to static JPEG thumbnail in the same folder as the GIF would be
                var staticFilename = $"{resourceId}{ThumbnailSuffix}";
                string staticPath;
                if (!string.IsNullOrEmpty(themeFolderPath))
                {
                    staticPath = Path.Combine(themeFolderPath, "thumbnails", staticFilename);
                }
                else
                {
                    staticPath = GetThumbnailPath(resourceId);
                }
                
                // Ensure directory exists for static path too
                var staticDir = Path.GetDirectoryName(staticPath);
                if (!string.IsNullOrEmpty(staticDir) && !Directory.Exists(staticDir))
                    Directory.CreateDirectory(staticDir);
                    
                if (File.Exists(staticPath))
                    return staticPath;
                return await GenerateVideoThumbnailAsync(sourcePath, staticPath, ct);
            }

            try
            {
                return await GenerateGifViaFFmpegAsync(sourcePath, outputPath, ct);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] GIF generation failed: {ex.Message}, falling back to static thumbnail");
                var staticFilename = $"{resourceId}{ThumbnailSuffix}";
                string staticPath;
                if (!string.IsNullOrEmpty(themeFolderPath))
                {
                    staticPath = Path.Combine(themeFolderPath, "thumbnails", staticFilename);
                }
                else
                {
                    staticPath = GetThumbnailPath(resourceId);
                }
                if (File.Exists(staticPath))
                    return staticPath;
                return await GenerateVideoThumbnailAsync(sourcePath, staticPath, ct);
            }
        }

        // ==================== Private Generation Methods ====================

        /// <summary>
        /// Generates an animated GIF from a video using FFmpeg CLI.
        /// Creates a looping GIF from the first 5 seconds at 8fps, scaled to thumbnail width.
        /// Uses a two-pass palette approach for optimal GIF quality.
        /// </summary>
        private static async Task<string> GenerateGifViaFFmpegAsync(string sourcePath, string outputPath, CancellationToken ct)
        {
            // Validate paths don't contain characters that could break FFmpeg argument parsing
            if (sourcePath.Contains('"') || outputPath.Contains('"'))
            {
                Debug.WriteLine("[ThumbnailService] Path contains quotes, cannot safely pass to FFmpeg");
                return string.Empty;
            }

            // FFmpeg command for high-quality GIF with palette generation:
            // -t 5: max 5 seconds
            // -vf: fps=8, scale to 320px width (maintain aspect), palette generation for quality
            // -loop 0: infinite loop
            var vfFilter = $"fps={GifFps},scale={IThumbnailService.ThumbnailWidth}:-1:flags=lanczos,split[s0][s1];" +
                           $"[s0]palettegen=max_colors=128[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // Use ArgumentList for safe argument passing (no shell interpretation)
            process.StartInfo.ArgumentList.Add("-i");
            process.StartInfo.ArgumentList.Add(sourcePath);
            process.StartInfo.ArgumentList.Add("-t");
            process.StartInfo.ArgumentList.Add(GifMaxDurationSeconds.ToString());
            process.StartInfo.ArgumentList.Add("-vf");
            process.StartInfo.ArgumentList.Add(vfFilter);
            process.StartInfo.ArgumentList.Add("-loop");
            process.StartInfo.ArgumentList.Add("0");
            process.StartInfo.ArgumentList.Add("-y");
            process.StartInfo.ArgumentList.Add(outputPath);

            process.Start();

            var tcs = new TaskCompletionSource<bool>();
            ct.Register(() =>
            {
                try { if (!process.HasExited) { process.Kill(); } }
                catch (Exception ex) { Debug.WriteLine($"[ThumbnailService] Failed to kill FFmpeg process on cancellation: {ex.Message}"); }
            });

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => tcs.TrySetResult(true);

            // Drain stdout/stderr to prevent deadlock
            _ = process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            // Wait with timeout (30 seconds for GIF generation)
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30), ct));

            if (completed != tcs.Task)
            {
                try { if (!process.HasExited) { process.Kill(); } }
                catch (Exception ex) { Debug.WriteLine($"[ThumbnailService] Failed to kill timed-out FFmpeg process: {ex.Message}"); }
                Debug.WriteLine("[ThumbnailService] FFmpeg GIF generation timed out");
                return string.Empty;
            }

            if (process.ExitCode != 0)
            {
                Debug.WriteLine($"[ThumbnailService] FFmpeg GIF generation failed (exit code {process.ExitCode}): {stderr}");
                return string.Empty;
            }

            if (File.Exists(outputPath))
            {
                Debug.WriteLine($"[ThumbnailService] Generated GIF thumbnail: {outputPath}");
                return outputPath;
            }

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
        /// Generates a static thumbnail from a video file by extracting a frame using WPF MediaPlayer.
        /// Must run on an STA thread with dispatcher for WPF media operations.
        /// </summary>
        private async Task<string> GenerateVideoThumbnailAsync(string sourcePath, string outputPath, CancellationToken ct)
        {
            try
            {
                var tcs = new TaskCompletionSource<string>();

                // Create STA thread for WPF media operations
                var thread = new Thread(() =>
                {
                    try
                    {
                        var player = new MediaPlayer();
                        var frame = new System.Windows.Threading.DispatcherFrame();
                        player.Open(new Uri(sourcePath, UriKind.Absolute));
                        player.ScrubbingEnabled = true;

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

                                // Allow time for the frame to render
                                Thread.Sleep(500);

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
                            finally
                            {
                                frame.Continue = false;
                            }
                        };

                        player.MediaFailed += (s, e) =>
                        {
                            tcs.TrySetResult(string.Empty);
                            Debug.WriteLine($"[ThumbnailService] Media open failed: {e.ErrorException?.Message}");
                            frame.Continue = false;
                        };

                        // Pump messages until complete (replaces Dispatcher.Run())
                        System.Windows.Threading.Dispatcher.PushFrame(frame);
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
