using System;
using System.Runtime.InteropServices;
using Serilog;

namespace PoE2Overlay.Core
{
    /// <summary>
    /// PoE2 게임 윈도우의 포커스 상태를 감지하고,
    /// 오버레이 표시 후 게임으로 포커스를 복원합니다.
    /// </summary>
    public static class GameFocusManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static IntPtr _lastGameWindow = IntPtr.Zero;

        /// <summary>
        /// 오버레이를 표시하기 전에 호출합니다.
        /// 현재 포그라운드 윈도우 핸들을 저장합니다.
        /// </summary>
        public static void CaptureCurrentFocus()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                    _lastGameWindow = hwnd;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "CaptureCurrentFocus failed");
            }
        }

        /// <summary>
        /// 오버레이를 닫은 후 호출합니다.
        /// 저장된 게임 윈도우로 포커스를 복원합니다.
        /// </summary>
        public static void RestoreFocusToGame()
        {
            try
            {
                if (_lastGameWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(_lastGameWindow);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "RestoreFocusToGame failed");
            }
        }
    }
}
