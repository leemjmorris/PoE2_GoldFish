using System;
using System.IO;
using Newtonsoft.Json;

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

        // Trade
        public double TradeWindowLeft { get; set; } = 300;
        public double TradeWindowTop { get; set; } = 100;
        public double TradeWindowWidth { get; set; } = 420;
        public double TradeWindowHeight { get; set; } = 600;
        public string TradeLeague { get; set; } = "Standard";

        private static AppSettings _instance;

        public static AppSettings Instance => _instance ??= Load();

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
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
