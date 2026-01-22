using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ProductivityWallpaper.ViewModels
{
    public partial class PromptSegmentViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _userText = "";

        [ObservableProperty]
        private string _selectedKeyword = "";

        [ObservableProperty]
        private string _category = "General";

        // 模拟数据源，实际应从 ConfigService 读取
        public List<string> AvailableKeywords { get; } = new()
        {
            "Cyberpunk", "Realistic", "Anime", "Oil Painting", "Neon", "Cinematic",
            "Camera Shake 30 deg", "Soft Lighting", "8k Resolution"
        };

        // 监听 SelectedKeyword 变化
        partial void OnSelectedKeywordChanged(string value)
        {
            // 需求2实现：当下拉框选择变化时，直接填充到文本框
            // 用户之后可以随意修改文本框，不会影响下拉框，反之亦然
            if (!string.IsNullOrEmpty(value))
            {
                UserText = value;
            }
        }
    }
}