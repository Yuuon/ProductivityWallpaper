using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Services;
using ProductivityWallpaper.ViewModels;
using ProductivityWallpaper.Views;
using LibVLCSharp.Shared;

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

            // Services
            services.AddSingleton<LocalizationService>();
            services.AddSingleton<WallpaperService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<MouseHookService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<WallpaperViewModel>();
            services.AddSingleton<AiToolViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // New ViewModels
            services.AddSingleton<WorkshopViewModel>();
            services.AddSingleton<MyThemeViewModel>();
            services.AddTransient<DesktopBackgroundViewModel>();
            services.AddTransient<MouseClickViewModel>();
            services.AddTransient<DesktopClockViewModel>();
            services.AddTransient<PomodoroViewModel>();

            // System Event ViewModels
            services.AddTransient<ShutdownViewModel>();
            services.AddTransient<BootRestartViewModel>();
            services.AddTransient<ScreenWakeViewModel>();

            // Anniversary ViewModel
            services.AddTransient<AnniversaryViewModel>();

            // Views
            services.AddTransient<DesktopClockView>();
            services.AddTransient<PomodoroView>();
            services.AddTransient<AnniversaryView>();
            services.AddTransient<ShutdownView>();
            services.AddTransient<BootRestartView>();
            services.AddTransient<ScreenWakeView>();

            // CreatorViewModel with factory injection
            services.AddSingleton<CreatorViewModel>(serviceProvider =>
            {
                return new CreatorViewModel(
                    () => serviceProvider.GetRequiredService<DesktopBackgroundViewModel>(),
                    () => serviceProvider.GetRequiredService<MouseClickViewModel>(),
                    () => serviceProvider.GetRequiredService<DesktopClockViewModel>(),
                    () => serviceProvider.GetRequiredService<PomodoroViewModel>(),
                    () => serviceProvider.GetRequiredService<AnniversaryViewModel>(),
                    () => serviceProvider.GetRequiredService<ShutdownViewModel>(),
                    () => serviceProvider.GetRequiredService<BootRestartViewModel>(),
                    () => serviceProvider.GetRequiredService<ScreenWakeViewModel>());
            });

            // Views
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize VLC core
            Core.Initialize();

            // Initialize ConfigService to ensure config file exists
            var config = Services.GetRequiredService<ConfigService>();

            var loc = Services.GetRequiredService<LocalizationService>();
            loc.Initialize();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
