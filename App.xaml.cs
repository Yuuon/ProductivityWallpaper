using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Services;
using ProductivityWallpaper.ViewModels;
using ProductivityWallpaper.Views;
using LibVLCSharp.Shared; // 引用

namespace ProductivityWallpaper
{
    public partial class App : System.Windows.Application
    {
        public IServiceProvider Services { get; }

        public new static App Current => (App)System.Windows.Application.Current;

        public App()
        {
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<LocalizationService>();
            services.AddSingleton<WallpaperService>();
            services.AddSingleton<ConfigService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<WallpaperViewModel>();
            services.AddSingleton<AiToolViewModel>();
            services.AddSingleton<SettingsViewModel>();

            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化 VLC 核心
            Core.Initialize();

            var loc = Services.GetRequiredService<LocalizationService>();
            loc.Initialize();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}