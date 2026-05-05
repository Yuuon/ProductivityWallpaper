using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Pre-tiles a source image to a screen-sized PNG and caches the result on disk.
    /// 
    /// WHY THIS EXISTS:
    /// Image wallpapers are rendered through VLC's image demuxer (see
    /// <see cref="Views.VideoPlayerWindow.CreateForImage"/>) because WPF's
    /// redirection bitmap is not composited under Progman+WS_EX_NOREDIRECTIONBITMAP
    /// on Win11 24H2 raised desktop. VLC's image demuxer has no native tile mode,
    /// and a WPF <c>ImageBrush.TileMode=Tile</c> overlay would not paint either.
    /// 
    /// The simplest fix that reuses the existing working pipeline is to pre-compute
    /// a screen-sized bitmap that already contains the tiled pattern, then hand
    /// THAT file to VLC and render it Fill. The user-visible result is identical
    /// to a true tiled brush.
    /// 
    /// Cache key combines:
    /// - SHA1 of source path (so different source files don't collide)
    /// - Source file mtime ticks (so edits to the source invalidate the cache)
    /// - Target W×H (so resolution changes regenerate)
    /// 
    /// Output is PNG to keep edges sharp regardless of source format.
    /// </summary>
    public static class ImageTileCache
    {
        private static readonly string CacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProductivityWallpaper",
            "TileCache");

        /// <summary>
        /// Returns a path to a screen-sized PNG that contains <paramref name="sourcePath"/>
        /// tiled to fill <paramref name="screenWidth"/>×<paramref name="screenHeight"/>.
        /// Generates and caches the file on first call, reuses it thereafter.
        /// 
        /// On any failure (source missing, decode error, IO error) returns
        /// <paramref name="sourcePath"/> unchanged so the caller can fall back to
        /// non-tiled rendering rather than fail the wallpaper apply outright.
        /// </summary>
        public static string GetOrCreate(string sourcePath, int screenWidth, int screenHeight)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return sourcePath;
                if (screenWidth <= 0 || screenHeight <= 0) return sourcePath;

                Directory.CreateDirectory(CacheDir);

                var mtime = File.GetLastWriteTimeUtc(sourcePath).Ticks;
                var key = ComputeKey(sourcePath, screenWidth, screenHeight, mtime);
                var outPath = Path.Combine(CacheDir, key + ".png");

                if (File.Exists(outPath)) return outPath;

                // Write to a .tmp sibling first then rename, so a concurrent caller never
                // sees a half-written PNG and treats it as cached.
                var tmpPath = outPath + ".tmp";

                // Decode source. FileStream avoids the Image.FromFile lock-on-source quirk.
                using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var src = Image.FromStream(fs))
                using (var dst = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(dst))
                    {
                        // Pixel-perfect tiling at source's native size — what users expect
                        // from "Tile" mode (cf. Windows classic tiled wallpaper).
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.SmoothingMode = SmoothingMode.None;
                        g.CompositingMode = CompositingMode.SourceCopy;

                        using var brush = new TextureBrush(src, WrapMode.Tile);
                        g.FillRectangle(brush, 0, 0, screenWidth, screenHeight);
                    }

                    dst.Save(tmpPath, ImageFormat.Png);
                }

                // Atomic publish. File.Move handles the rare race where two callers slip
                // past the File.Exists check above.
                if (File.Exists(outPath))
                {
                    try { File.Delete(tmpPath); } catch { }
                }
                else
                {
                    File.Move(tmpPath, outPath);
                }

                return outPath;
            }
            catch
            {
                return sourcePath;
            }
        }

        private static string ComputeKey(string sourcePath, int w, int h, long mtimeTicks)
        {
            using var sha = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes($"{sourcePath.ToLowerInvariant()}|{w}x{h}|{mtimeTicks}");
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
