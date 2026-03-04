using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace PoE2Overlay.Core
{
    public class AppSettings
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PoE2Overlay");

        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        // Memo
        public double MemoWindowLeft { get; set; } = 100;
        public double MemoWindowTop { get; set; } = 100;
        public double MemoWindowWidth { get; set; } = 350;
        public double MemoWindowHeight { get; set; } = 400;

        // Screenshot
        public string ScreenshotDirectory { get; set; } = "";
        public double ScreenshotWindowLeft { get; set; } = 200;
        public double ScreenshotWindowTop { get; set; } = 100;
        public double ScreenshotWindowWidth { get; set; } = 400;
        public double ScreenshotWindowHeight { get; set; } = 500;

        // General
        public string GameLanguage { get; set; } = "en";

        private static readonly Lazy<AppSettings> _lazy = new(Load);

        public static AppSettings Instance => _lazy.Value;

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex) { Log.Warning(ex, "Settings load failed"); }
            return new AppSettings();
        }

        private static readonly object _saveLock = new();

        public void Save()
        {
            lock (_saveLock)
            {
                try
                {
                    Directory.CreateDirectory(SettingsDir);
                    string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                    File.WriteAllText(SettingsPath, json);
                }
                catch (Exception ex) { Log.Warning(ex, "Settings save failed"); }
            }
        }
    }
}
