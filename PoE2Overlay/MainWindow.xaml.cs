using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using PoE2Overlay.Core;
using PoE2Overlay.Features.Memo;
using PoE2Overlay.Features.Screenshot;
using PoE2Overlay.Features.Trade;

namespace PoE2Overlay
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_C = 0x43;

        private HotkeyManager _hotkeyManager;
        private MemoOverlay _memoOverlay;
        private ScreenshotOverlay _screenshotOverlay;
        private TradeOverlay _tradeOverlay;

        public MainWindow()
        {
            InitializeComponent();

            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Register(ModKeys.None, VKeys.F2, ToggleMemo);
            _hotkeyManager.Register(ModKeys.None, VKeys.F3, ToggleScreenshot);
            _hotkeyManager.Register(ModKeys.Ctrl, VKeys.D, OnPriceCheck, suppress: true);
        }

        /// <summary>
        /// Ctrl+D → D를 게임에 전달 차단 → Ctrl+C 시뮬레이션 → 클립보드 읽기 → 시세 조회
        /// Sidekick 방식과 동일.
        /// </summary>
        private void OnPriceCheck()
        {
            _ = Dispatcher.InvokeAsync(async () =>
            {
                // Ctrl+C 시뮬레이션 (SendInput API 사용)
                var inputs = new INPUT[]
                {
                    MakeKeyInput(VK_CONTROL, false),
                    MakeKeyInput(VK_C, false),
                    MakeKeyInput(VK_C, true),
                    MakeKeyInput(VK_CONTROL, true),
                };
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

                // 클립보드 업데이트 대기
                await Task.Delay(150);

                // 클립보드 읽기
                string text = Clipboard.GetText();

                if (!string.IsNullOrEmpty(text) &&
                    text.Contains("Item Class:") &&
                    text.Contains("--------"))
                {
                    if (_tradeOverlay == null)
                        _tradeOverlay = new TradeOverlay();

                    _tradeOverlay.ProcessClipboardText(text);
                }
            });
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

        private static INPUT MakeKeyInput(ushort vk, bool keyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                union = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                    }
                }
            };
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
            TrayIcon?.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyManager?.Dispose();
            TrayIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
