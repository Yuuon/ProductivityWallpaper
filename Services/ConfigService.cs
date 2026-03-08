using System.IO;
using System.Text.Json;

namespace ProductivityWallpaper.Services
{
    public class AppConfig
    {
        public string LastLibraryPath { get; set; } = "";
        public string MediaLibraryPath { get; set; } = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
    }

    public class ConfigService
    {
        private const string ConfigFile = "user_config.json";
        public AppConfig Config { get; private set; }

        public ConfigService()
        {
            Load();
        }

        public void Load()
        {
            // Start with default values
            Config = new AppConfig();
            
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFile);
                    var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json);
                    if (loadedConfig != null)
                    {
                        // Only override if value is not empty
                        if (!string.IsNullOrEmpty(loadedConfig.LastLibraryPath))
                            Config.LastLibraryPath = loadedConfig.LastLibraryPath;
                        if (!string.IsNullOrEmpty(loadedConfig.MediaLibraryPath))
                            Config.MediaLibraryPath = loadedConfig.MediaLibraryPath;
                    }
                }
                catch
                {
                    // Use defaults if load fails
                }
            }
            
            // Save to ensure file exists with current values
            Save();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                File.WriteAllText(ConfigFile, JsonSerializer.Serialize(Config, options));
            }
            catch { /* Handle save error */ }
        }
    }
}