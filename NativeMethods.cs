using System;
using System.Runtime.InteropServices;

namespace FoldVision
{
    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED  = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;

        public const int WM_POWERBROADCAST      = 0x0218;
        public const int PBT_POWERSETTINGCHANGE = 0x8013;

        /// <summary>GUID para notificaciones de apertura/cierre de tapa de laptop.</summary>
        public static readonly Guid GUID_LIDSWITCH_STATE_CHANGE =
            new Guid(0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79, 0xE6, 0xA0, 0xF3);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid  PowerSetting;
            public uint  DataLength;
            public byte  Data;   // 0 = tapa cerrada, 1 = tapa abierta
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        /// <summary>
        /// Registra una ventana (HWND) para recibir notificaciones de energía (WM_POWERBROADCAST).
        /// Usar DEVICE_NOTIFY_WINDOW_HANDLE = 0 como dwFlags para ventanas Win32.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterPowerSettingNotification(
            IntPtr hRecipient, ref Guid PowerSettingGuid, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        // ── Monitor / Desktop helpers (usados por GpuCaptureService y D3D9Interop) ──

        public const uint MONITOR_DEFAULTTOPRIMARY = 1;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public const int DESKTOPHORZRES = 118;
        public const int DESKTOPVERTRES = 117;
    }
}
