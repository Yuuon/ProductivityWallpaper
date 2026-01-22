using System.IO;
using System.Text.Json;

namespace ProductivityWallpaper.Services
{
    public class AppConfig
    {
        public string LastLibraryPath { get; set; } = "";
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
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFile);
                    Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    Config = new AppConfig();
                }
            }
            else
            {
                Config = new AppConfig();
            }
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(ConfigFile, JsonSerializer.Serialize(Config, options));
            }
            catch { /* Handle save error */ }
        }
    }
}