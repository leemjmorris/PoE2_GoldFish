using System;
using System.IO;
using System.Windows;
using Serilog;

namespace PoE2Overlay
{
    public partial class App : Application
    {
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

            Log.Information("PoE2 Overlay started");
            new MainWindow().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("PoE2 Overlay shutting down");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
