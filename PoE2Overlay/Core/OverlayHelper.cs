using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PoE2Overlay.Core
{
    /// <summary>
    /// Win32 SetWindowPos를 통해 오버레이 창의 Z-order를 강제로 유지한다.
    /// WPF Topmost=True만으로는 전체화면 게임이 포커스를 가져갈 때 가려질 수 있어서,
    /// Deactivated 이벤트 시 이 헬퍼를 호출해 HWND_TOPMOST를 재주장한다.
    /// </summary>
    public static class OverlayHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private const uint SWP_NOMOVE    = 0x0002;
        private const uint SWP_NOSIZE    = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        public static void AssertTopmost(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch { }
        }
    }
}
