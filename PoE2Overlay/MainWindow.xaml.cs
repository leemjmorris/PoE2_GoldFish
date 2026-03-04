using System;
using System.Windows;
using PoE2Overlay.Core;
using PoE2Overlay.Features.Memo;
using PoE2Overlay.Features.Screenshot;

namespace PoE2Overlay
{
    public partial class MainWindow : Window
    {
        private readonly HotkeyManager _hotkeyManager;

        private MemoOverlay _memoOverlay;
        private ScreenshotOverlay _screenshotOverlay;

        public MainWindow()
        {
            InitializeComponent();

            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Register(ModKeys.None, VKeys.F2, ToggleMemo);
            _hotkeyManager.Register(ModKeys.None, VKeys.F3, ToggleScreenshot);
        }

        private void ToggleMemo()
        {
            if (_memoOverlay == null)
                _memoOverlay = new MemoOverlay();

            _memoOverlay.Toggle();
        }

        private void ToggleScreenshot()
        {
            if (_screenshotOverlay == null)
                _screenshotOverlay = new ScreenshotOverlay();

            _screenshotOverlay.Toggle();
        }

        private void OnMemoClick(object sender, RoutedEventArgs e) => ToggleMemo();

        private void OnScreenshotClick(object sender, RoutedEventArgs e) => ToggleScreenshot();

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings - coming soon!", "PoE2 Overlay");
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            CleanupOverlays();
            _hotkeyManager?.Dispose();
            TrayIcon?.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupOverlays();
            _hotkeyManager?.Dispose();
            TrayIcon?.Dispose();
            base.OnClosed(e);
        }

        private void CleanupOverlays()
        {
            _memoOverlay?.Close();
            _screenshotOverlay?.Close();
        }
    }
}
