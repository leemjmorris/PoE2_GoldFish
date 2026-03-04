using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoE2Overlay.Core;
using Serilog;

namespace PoE2Overlay
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Serilog 초기화
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PoE2Overlay", "logs", "app-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 10_000_000)
                .CreateLogger();

            var services = new ServiceCollection();

            // 로깅
            services.AddLogging(builder => builder.AddSerilog());

            // Core
            services.AddSingleton<AppSettings>(_ => AppSettings.Instance);
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            Log.Information("PoE2 Overlay started");
            Services.GetRequiredService<MainWindow>().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("PoE2 Overlay shutting down");
            (Services as IDisposable)?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
