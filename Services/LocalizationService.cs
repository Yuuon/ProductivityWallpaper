using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProductivityWallpaper.Services
{
    public class LocalizationService
    {
        private const string LanguageFolder = "Resources/Languages";
        private ResourceDictionary _currentResourceDictionary;

        public string CurrentLanguageCode { get; private set; } = "en-US";

        public List<string> AvailableLanguages { get; private set; } = new();

        public LocalizationService()
        {
            LoadAvailableLanguages();
        }

        private void LoadAvailableLanguages()
        {
            // Create folder if not exists
            if (!Directory.Exists(LanguageFolder))
            {
                Directory.CreateDirectory(LanguageFolder);
            }

            // Create default language files if not exist
            EnsureLanguageFileExists("en-US", GetEnglishTranslations());
            EnsureLanguageFileExists("zh-CN", GetChineseTranslations());

            var files = Directory.GetFiles(LanguageFolder, "*.json");
            foreach (var file in files)
            {
                AvailableLanguages.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void EnsureLanguageFileExists(string languageCode, Dictionary<string, string> defaultTranslations)
        {
            var path = Path.Combine(LanguageFolder, $"{languageCode}.json");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonSerializer.Serialize(defaultTranslations, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            }
        }

        public void Initialize()
        {
            // Auto-detect culture or fallback to English
            var culture = CultureInfo.CurrentCulture.Name;
            if (AvailableLanguages.Contains(culture))
            {
                LoadLanguage(culture);
            }
            else if (culture.StartsWith("zh") && AvailableLanguages.Contains("zh-CN"))
            {
                LoadLanguage("zh-CN");
            }
            else
            {
                LoadLanguage("en-US");
            }
        }

        public void LoadLanguage(string languageCode)
        {
            var path = Path.Combine(LanguageFolder, $"{languageCode}.json");
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (dict == null) return;

                // Add all translations to App.Resources
                var appResources = System.Windows.Application.Current.Resources;
                foreach (var kvp in dict)
                {
                    // Remove existing key if present, then add new value
                    if (appResources.Contains(kvp.Key))
                    {
                        appResources.Remove(kvp.Key);
                    }
                    appResources.Add(kvp.Key, kvp.Value);
                }

                CurrentLanguageCode = languageCode;
            }
            catch { /* Handle error */ }
        }

        private Dictionary<string, string> GetEnglishTranslations()
        {
            return new Dictionary<string, string>
            {
                // Application
                { "AppTitle", "Productivity Wallpaper" },
                
                // Navigation
                { "Nav_Workshop", "Workshop" },
                { "Nav_MyThemes", "My Themes" },
                { "Nav_Creator", "Creator" },
                
                // Workshop View
                { "Workshop_SearchPlaceholder", "Search themes..." },
                { "Workshop_AllResolutions", "All Resolutions" },
                { "Workshop_MostPopular", "Most Popular" },
                { "Workshop_Latest", "Latest" },
                { "Workshop_UseNow", "Use Now" },
                { "Workshop_InUse", "In Use" },
                { "Workshop_Edit", "Edit" },
                { "Workshop_SelectThemeHint", "Select a theme to view details" },
                
                // Tags
                { "Tag_DesktopBackground", "Desktop Background" },
                { "Tag_MouseClick", "Mouse Click" },
                { "Tag_Shutdown", "Shutdown" },
                { "Tag_BootRestart", "Boot/Restart" },
                { "Tag_ScreenWake", "Screen Wake" },
                { "Tag_OpenApp", "Open App" },
                { "Tag_DesktopClock", "Desktop Clock" },
                { "Tag_Pomodoro", "Pomodoro" },
                { "Tag_Anniversary", "Anniversary" },
                
                // My Themes View
                { "MyThemes_UsageHistory", "Usage History" },
                { "MyThemes_MyWorks", "My Works" },
                { "MyThemes_EmptyHistoryMessage", "Nothing here yet\nGo find themes you like~" },
                { "MyThemes_EmptyWorksMessage", "Nothing here yet\nGo create your own theme~" },
                { "MyThemes_GoToWorkshop", "Browse Workshop" },
                { "MyThemes_GoCreate", "Start Creating" },
                { "MyThemes_UseNow", "Use Now" },
                { "MyThemes_Edit", "Edit" },
                { "MyThemes_SelectThemeHint", "Select a theme to view details" },
                
                // Creator View
                { "Creator_WelcomeTitle", "Make your desktop, your way" },
                { "Creator_ThemeNamePlaceholder", "Enter theme name..." },
                { "Creator_CreateTheme", "Create Theme" },
                { "Creator_Back", "Back" },
                { "Creator_EmptyPreview", "Nothing here yet\nGo create your exclusive theme~" },
                
                // Feature Names
                { "Creator_Feature_ThemePreview", "Theme Preview" },
                { "Creator_Feature_DesktopBackground", "Desktop Background" },
                { "Creator_Feature_MouseClick", "Mouse Click" },
                { "Creator_Feature_Shutdown", "Shutdown" },
                { "Creator_Feature_BootRestart", "Boot/Restart" },
                { "Creator_Feature_ScreenWake", "Screen Wake" },
                { "Creator_Feature_OpenApp", "Open App" },
                { "Creator_Feature_DesktopClock", "Desktop Clock" },
                { "Creator_Feature_Pomodoro", "Pomodoro" },
                { "Creator_Feature_Anniversary", "Anniversary" },
                
                // Settings
                { "Settings_MediaLibraryPath", "Media Library Location" },
                { "Settings_Browse", "Browse" },
                
                // Legacy
                { "Menu_Wallpaper", "Wallpaper" },
                { "Menu_AI", "AI Studio" },
                { "Menu_Settings", "Settings" },
                { "Tray_Show", "Show Window" },
                { "Tray_Exit", "Exit" },
                { "Btn_Generate", "Generate Prompt" },
                { "Label_Language", "Interface Language" },
                { "Msg_ExitConfirm", "Are you sure you want to exit?" }
            };
        }

        private Dictionary<string, string> GetChineseTranslations()
        {
            return new Dictionary<string, string>
            {
                // Application
                { "AppTitle", "productivity壁纸" },
                
                // Navigation
                { "Nav_Workshop", "创意工坊" },
                { "Nav_MyThemes", "我的主题" },
                { "Nav_Creator", "创作主题" },
                
                // Workshop View
                { "Workshop_SearchPlaceholder", "搜索主题名字..." },
                { "Workshop_AllResolutions", "全部分辨率" },
                { "Workshop_MostPopular", "热度最高" },
                { "Workshop_Latest", "最新" },
                { "Workshop_UseNow", "立即使用" },
                { "Workshop_InUse", "使用中" },
                { "Workshop_Edit", "去修改" },
                { "Workshop_SelectThemeHint", "选择主题查看详情" },
                
                // Tags
                { "Tag_DesktopBackground", "桌面背景" },
                { "Tag_MouseClick", "鼠标点击" },
                { "Tag_Shutdown", "关机" },
                { "Tag_BootRestart", "开机重启" },
                { "Tag_ScreenWake", "屏幕唤醒" },
                { "Tag_OpenApp", "打开应用" },
                { "Tag_DesktopClock", "桌面时钟" },
                { "Tag_Pomodoro", "番茄专注钟" },
                { "Tag_Anniversary", "纪念日" },
                
                // My Themes View
                { "MyThemes_UsageHistory", "使用记录" },
                { "MyThemes_MyWorks", "我的作品" },
                { "MyThemes_EmptyHistoryMessage", "这里空空如也\n去找找你喜欢的主题吧~" },
                { "MyThemes_EmptyWorksMessage", "这里空空如也\n去创作你的专属主题吧~" },
                { "MyThemes_GoToWorkshop", "去瞅瞅" },
                { "MyThemes_GoCreate", "去创作" },
                { "MyThemes_UseNow", "立即使用" },
                { "MyThemes_Edit", "去修改" },
                { "MyThemes_SelectThemeHint", "选择主题查看详情" },
                
                // Creator View
                { "Creator_WelcomeTitle", "把桌面，变成你喜欢的样子" },
                { "Creator_ThemeNamePlaceholder", "输入主题名字..." },
                { "Creator_CreateTheme", "创作主题" },
                { "Creator_Back", "返回" },
                { "Creator_EmptyPreview", "这里空空如也\n去创作你的专属主题吧~" },
                
                // Feature Names
                { "Creator_Feature_ThemePreview", "主题预览" },
                { "Creator_Feature_DesktopBackground", "桌面背景" },
                { "Creator_Feature_MouseClick", "鼠标点击" },
                { "Creator_Feature_Shutdown", "关机" },
                { "Creator_Feature_BootRestart", "开机重启" },
                { "Creator_Feature_ScreenWake", "屏幕唤醒" },
                { "Creator_Feature_OpenApp", "打开应用" },
                { "Creator_Feature_DesktopClock", "桌面时钟" },
                { "Creator_Feature_Pomodoro", "番茄专注钟" },
                { "Creator_Feature_Anniversary", "纪念日" },
                
                // Settings
                { "Settings_MediaLibraryPath", "本地媒体库位置" },
                { "Settings_Browse", "浏览" },
                
                // Legacy
                { "Menu_Wallpaper", "壁纸" },
                { "Menu_AI", "AI工作室" },
                { "Menu_Settings", "设置" },
                { "Tray_Show", "显示窗口" },
                { "Tray_Exit", "退出" },
                { "Btn_Generate", "生成提示词" },
                { "Label_Language", "界面语言" },
                { "Msg_ExitConfirm", "确定要退出吗？" }
            };
        }
    }
}