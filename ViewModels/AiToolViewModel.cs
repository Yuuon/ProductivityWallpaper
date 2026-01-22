using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Models;
using ProductivityWallpaper.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ProductivityWallpaper.ViewModels
{
    public partial class AiToolViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<PromptSegmentViewModel> _segments = new();

        [ObservableProperty]
        private string _fullPrompt = string.Empty;

        public AiToolViewModel()
        {
            // Demo Data
            Segments.Add(new PromptSegmentViewModel { Category = "Style", SelectedKeyword = "Cyberpunk" }); // 初始化时也会触发填充
            Segments.Add(new PromptSegmentViewModel { Category = "Lighting", SelectedKeyword = "Neon" });
        }

        [RelayCommand]
        public void AddSegment()
        {
            Segments.Add(new PromptSegmentViewModel());
        }

        // 新增：移除行功能
        [RelayCommand]
        public void RemoveSegment(PromptSegmentViewModel segment)
        {
            if (Segments.Contains(segment))
            {
                Segments.Remove(segment);
            }
        }

        [RelayCommand]
        public void GeneratePrompt()
        {
            // 逻辑修改：只取 UserText，不取 Keyword，且忽略空行
            var parts = Segments.Select(s => s.UserText.Trim())
                                .Where(s => !string.IsNullOrEmpty(s));
            FullPrompt = string.Join(", ", parts);
        }

        [RelayCommand]
        public void ExecuteAiTask()
        {
            Console.WriteLine($"[AI Service] Executing Image Gen with: {FullPrompt}");
            System.Windows.MessageBox.Show($"Request sent to AI: {FullPrompt}");
        }
    }
}