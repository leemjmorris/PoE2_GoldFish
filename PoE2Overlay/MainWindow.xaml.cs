using System;
using System.Windows;
using System.Windows.Interop;
using PoE2Overlay.Core;
using PoE2Overlay.Features.Memo;
using PoE2Overlay.Features.Screenshot;
using PoE2Overlay.Features.Trade;

namespace PoE2Overlay
{
    public partial class MainWindow : Window
    {
        private HotkeyManager _hotkeyManager;
        private ClipboardListener _clipboardListener;
        private MemoOverlay _memoOverlay;
        private ScreenshotOverlay _screenshotOverlay;
        private TradeOverlay _tradeOverlay;

        public MainWindow()
        {
            InitializeComponent();

            // WH_KEYBOARD_LL 훅은 HWND 불필요 — 바로 초기화
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Register(ModKeys.None, VKeys.F2, ToggleMemo);
            _hotkeyManager.Register(ModKeys.None, VKeys.F3, ToggleScreenshot);

            // ClipboardListener는 HWND 필요 → SourceInitialized에서 초기화
            SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            _clipboardListener = new ClipboardListener(this);
            _clipboardListener.ClipboardTextChanged += OnClipboardChanged;
        }

        private void OnClipboardChanged(string text)
        {
            // PoE2 아이템 텍스트 감지
            if (text.Contains("Item Class:") && text.Contains("--------"))
            {
                if (_tradeOverlay == null)
                    _tradeOverlay = new TradeOverlay();

                _tradeOverlay.ProcessClipboardText(text);
            }
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

        private void OnTradeClick(object sender, RoutedEventArgs e)
        {
            if (_tradeOverlay == null)
                _tradeOverlay = new TradeOverlay();

            _tradeOverlay.Toggle();
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings - coming soon!", "PoE2 Overlay");
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _hotkeyManager?.Dispose();
            _clipboardListener?.Dispose();
            TrayIcon?.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyManager?.Dispose();
            _clipboardListener?.Dispose();
            TrayIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
