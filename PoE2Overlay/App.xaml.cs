using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PoE2Overlay.Core;
using PoE2Overlay.Features.Trade.Services;

namespace PoE2Overlay
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var services = new ServiceCollection();
            services.AddSingleton<AppSettings>(_ => AppSettings.Instance);
            services.AddSingleton<StatIdResolver>();
            services.AddSingleton<TradeApiClient>();
            services.AddSingleton<MainWindow>();
            Services = services.BuildServiceProvider();

            Services.GetRequiredService<MainWindow>().Show();
        }
    }
}
