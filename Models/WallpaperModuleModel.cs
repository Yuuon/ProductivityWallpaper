using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;

namespace ProductivityWallpaper.Models
{
    // --- Wallpaper Module Models ---

    public enum MediaType { Image, Video, Web, Interactive }

    public class MediaItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public MediaType Type { get; set; }

        [JsonIgnore]
        public string DisplayName => Type == MediaType.Interactive
            ? new DirectoryInfo(FilePath).Name  // 如果是互动包，显示文件夹名
            : System.IO.Path.GetFileName(FilePath);

        [JsonIgnore]
        public string DisplayThumbnail => !string.IsNullOrEmpty(ThumbnailPath) && System.IO.File.Exists(ThumbnailPath)
            ? ThumbnailPath
            : (Type == MediaType.Interactive
                ? "/Resources/Images/interactive_thumb.png" // 以后可以换成互动壁纸专用图标
                : "/Resources/Images/default_thumb.png");

        // 新增：如果是互动壁纸，这里存储配置对象
        [JsonIgnore]
        public InteractiveConfig? InteractiveConfig { get; set; }
    }

    public class LibraryMetadata
    {
        public string FolderPath { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public List<MediaItem> Items { get; set; } = new();
    }

    public class MonitorInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int Index { get; set; }
        public string DisplayText => IsPrimary ? $"Display {Index + 1} (Primary)" : $"Display {Index + 1}";
    }

    // --- 互动壁纸配置模型 (config.wallpaper) ---
    public class InteractiveConfig
    {
        public string IdleVideo { get; set; } = string.Empty; // 待机视频
        public List<InteractionTrigger> Triggers { get; set; } = new();
    }

    public class InteractionTrigger
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ActionVideo { get; set; } = string.Empty; // 触发后播放的视频

        // 触发区域/UI描述
        // 这里我们设计得通用一点，既可以是隐形热区，也可以是显示的按钮
        public string ButtonText { get; set; } = string.Empty; // 如果有字，就是一个按钮

        // 相对坐标 (0.0 - 1.0)，基于屏幕百分比
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        // UI 样式定义 (可选，简单实现)
        public string BackgroundHex { get; set; } = "#80000000"; // 半透明黑
        public string TextHex { get; set; } = "#FFFFFF";
    }
}