using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PoE2Overlay.Core
{
    /// <summary>
    /// WH_KEYBOARD_LL 기반 글로벌 키보드 훅.
    /// HWND 불필요, UI 스레드 메시지 펌프만 있으면 동작.
    /// 관리자 권한 프로세스(PoE2) 위에서도 키 캡처 가능.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int HC_ACTION = 0;

        private const int VK_CONTROL = 0x11;
        private const int VK_SHIFT = 0x10;
        private const int VK_MENU = 0x12; // Alt

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        private readonly List<HotkeyBinding> _bindings = new();

        public HotkeyManager()
        {
            _proc = HookCallback;
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(module.ModuleName), 0);
        }

        public void Register(uint modifiers, uint vkCode, Action callback)
        {
            _bindings.Add(new HotkeyBinding
            {
                VkCode = vkCode,
                Ctrl = (modifiers & ModKeys.Ctrl) != 0,
                Alt = (modifiers & ModKeys.Alt) != 0,
                Shift = (modifiers & ModKeys.Shift) != 0,
                Callback = callback
            });
        }

        private bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= HC_ACTION &&
                (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool ctrl = IsKeyDown(VK_CONTROL);
                bool alt = IsKeyDown(VK_MENU);
                bool shift = IsKeyDown(VK_SHIFT);

                foreach (var binding in _bindings)
                {
                    if (kb.vkCode == binding.VkCode &&
                        ctrl == binding.Ctrl &&
                        alt == binding.Alt &&
                        shift == binding.Shift)
                    {
                        binding.Callback.Invoke();
                        break;
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private struct HotkeyBinding
        {
            public uint VkCode;
            public bool Ctrl;
            public bool Alt;
            public bool Shift;
            public Action Callback;
        }
    }

    public static class ModKeys
    {
        public const uint None = 0x0000;
        public const uint Alt = 0x0001;
        public const uint Ctrl = 0x0002;
        public const uint Shift = 0x0004;
    }

    public static class VKeys
    {
        public const uint F1 = 0x70;
        public const uint F2 = 0x71;
        public const uint F3 = 0x72;
        public const uint F4 = 0x73;
        public const uint F5 = 0x74;
        public const uint D = 0x44;
    }
}
