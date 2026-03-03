using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PoE2Overlay.Core
{
    public class ClipboardListener : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private readonly Window _window;
        private HwndSource _source;

        public event Action<string> ClipboardTextChanged;

        public ClipboardListener(Window window)
        {
            _window = window;
            var helper = new WindowInteropHelper(window);
            helper.EnsureHandle();
            var handle = helper.Handle;

            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(WndProc);
            AddClipboardFormatListener(handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                               IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(text))
                            ClipboardTextChanged?.Invoke(text);
                    }
                }
                catch { }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            var handle = new WindowInteropHelper(_window).Handle;
            RemoveClipboardFormatListener(handle);
            _source?.RemoveHook(WndProc);
        }
    }
}
