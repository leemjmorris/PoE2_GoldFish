using System;
using System.Runtime.InteropServices;
using System.Text;
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private const string PoE2WindowTitle = "Path of Exile 2";
        private const string PoE1WindowTitle = "Path of Exile";

        private static IntPtr _lastGameWindow = IntPtr.Zero;
        private static DateTime _lastFocusCheck = DateTime.MinValue;
        private static bool _lastFocusResult;

        /// <summary>
        /// PoE2(또는 PoE1)가 현재 포그라운드에 있는지 확인합니다.
        /// 성능 최적화: 3초 이내 재호출 시 캐시된 결과를 반환합니다.
        /// </summary>
        public static bool IsGameInFocus
        {
            get
            {
                if ((DateTime.UtcNow - _lastFocusCheck).TotalSeconds < 3)
                    return _lastFocusResult;

                _lastFocusResult = CheckGameFocus();
                _lastFocusCheck = DateTime.UtcNow;
                return _lastFocusResult;
            }
        }

        private static bool CheckGameFocus()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return false;

                var sb = new StringBuilder(256);
                if (GetWindowText(hwnd, sb, 256) > 0)
                {
                    var title = sb.ToString();
                    if (title.Contains(PoE2WindowTitle, StringComparison.OrdinalIgnoreCase) ||
                        title.Contains(PoE1WindowTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        _lastGameWindow = hwnd;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "GameFocusManager check failed");
            }
            return false;
        }

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
