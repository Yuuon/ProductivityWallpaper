using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace ProductivityWallpaper.Models
{
    // --- AI Module Models ---

    public class PromptPreset
    {
        public string Name { get; set; } = string.Empty;
        public List<PromptSegmentData> Segments { get; set; } = new();
    }

    public class PromptSegmentData
    {
        public string UserText { get; set; } = string.Empty;
        public string SelectedKeyword { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // To remember which dropdown
    }

    public class KeywordConfig
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
    }
}