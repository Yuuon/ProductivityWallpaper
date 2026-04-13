using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ProductivityWallpaper.Models;
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
            services.AddSingleton<DesktopBridgeService>();
            services.AddSingleton<PlaybackMonitorService>();
            services.AddSingleton<WallpaperService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<MouseHookService>();

            // Theme and User Settings Services
            services.AddSingleton<ThemeService>();
            services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
            services.AddSingleton<IThumbnailService, ThumbnailService>();
            services.AddSingleton<UserSettingsService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<WallpaperViewModel>();
            services.AddSingleton<AiToolViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // New ViewModels
            services.AddSingleton<WorkshopViewModel>();
            services.AddSingleton<MyThemeViewModel>(sp =>
                new MyThemeViewModel(
                    sp.GetRequiredService<MainViewModel>(),
                    sp.GetRequiredService<IThemeService>(),
                    sp.GetRequiredService<WallpaperService>()));

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

            // Views (unified MediaConfigurationView replaces 4 duplicate views)
            services.AddTransient<MediaConfigurationView>();
            services.AddTransient<MouseClickView>();
            services.AddTransient<DesktopClockView>();
            services.AddTransient<PomodoroView>();
            services.AddTransient<AnniversaryView>();

            // Feature ViewModel Factory (registry pattern replaces 8 individual factories)
            services.AddSingleton<IFeatureViewModelFactory>(sp =>
                new FeatureViewModelFactory(new Dictionary<FeatureType, Func<ObservableObject>>
                {
                    [FeatureType.DesktopBackground] = () => sp.GetRequiredService<DesktopBackgroundViewModel>(),
                    [FeatureType.MouseClick] = () => sp.GetRequiredService<MouseClickViewModel>(),
                    [FeatureType.DesktopClock] = () => sp.GetRequiredService<DesktopClockViewModel>(),
                    [FeatureType.Pomodoro] = () => sp.GetRequiredService<PomodoroViewModel>(),
                    [FeatureType.Anniversary] = () => sp.GetRequiredService<AnniversaryViewModel>(),
                    [FeatureType.Shutdown] = () => sp.GetRequiredService<ShutdownViewModel>(),
                    [FeatureType.BootRestart] = () => sp.GetRequiredService<BootRestartViewModel>(),
                    [FeatureType.ScreenWake] = () => sp.GetRequiredService<ScreenWakeViewModel>(),
                }));

            // CreatorViewModel (receives factory + theme service)
            services.AddSingleton<CreatorViewModel>(sp =>
                new CreatorViewModel(
                    sp.GetRequiredService<IFeatureViewModelFactory>(),
                    sp.GetRequiredService<IThemeService>()));

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

        /// <summary>
        /// Ensures all services are properly disposed on application exit.
        /// Critical for releasing system hooks (mouse hook, WinEvent hooks)
        /// and VLC/LibVLC native resources that persist beyond GC collection.
        /// Disposes in reverse dependency order to prevent callbacks into disposed services.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // WallpaperService owns DesktopBridgeService and PlaybackMonitorService,
                // and its Dispose() will clean them up. Dispose it first.
                var wallpaperService = Services.GetService<WallpaperService>();
                wallpaperService?.Dispose();

                var mouseHook = Services.GetService<MouseHookService>();
                mouseHook?.Dispose();

                // Safety net: dispose these explicitly in case WallpaperService.Dispose() missed them
                var playbackMonitor = Services.GetService<PlaybackMonitorService>();
                playbackMonitor?.Dispose();

                var desktopBridge = Services.GetService<DesktopBridgeService>();
                desktopBridge?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error during OnExit cleanup: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}
