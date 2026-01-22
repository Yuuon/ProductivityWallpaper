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
                // Create default en-US for safety
                var defaultDict = new Dictionary<string, string>
                {
                    { "AppTitle", "Productivity Wallpaper" },
                    { "Menu_Wallpaper", "Wallpaper" },
                    { "Menu_AI", "AI Studio" },
                    { "Menu_Settings", "Settings" },
                    { "Tray_Show", "Show Window" },
                    { "Tray_Exit", "Exit" },
                    { "Btn_Generate", "Generate Prompt" },
                    { "Label_Language", "Interface Language" },
                    { "Msg_ExitConfirm", "Are you sure you want to exit?" }
                };
                File.WriteAllText(Path.Combine(LanguageFolder, "en-US.json"), JsonSerializer.Serialize(defaultDict));
            }

            var files = Directory.GetFiles(LanguageFolder, "*.json");
            foreach (var file in files)
            {
                AvailableLanguages.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        public void Initialize()
        {
            // Auto-detect culture or fallback to English
            var culture = CultureInfo.CurrentCulture.Name; // e.g., "zh-CN"
            if (AvailableLanguages.Contains(culture))
            {
                LoadLanguage(culture);
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

                // Create a dynamic resource dictionary
                var resDict = new ResourceDictionary();
                foreach (var kvp in dict)
                {
                    resDict.Add(kvp.Key, kvp.Value);
                }

                // Swap in App.xaml
                var appResources = System.Windows.Application.Current.Resources;
                var oldDict = appResources.MergedDictionaries.FirstOrDefault(d => d.Contains("IsLocalizationDictionary"));

                if (oldDict != null) appResources.MergedDictionaries.Remove(oldDict);

                resDict["IsLocalizationDictionary"] = true; // Marker
                appResources.MergedDictionaries.Add(resDict);

                CurrentLanguageCode = languageCode;
            }
            catch { /* Handle error */ }
        }
    }
}