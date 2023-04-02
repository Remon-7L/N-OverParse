using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OverParse
{
    /// <summary>DLLImport用クラス</summary>
    internal static class NativeMethods
    {
        //HotKey.cs
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RegisterHotKey(IntPtr hWnd, int id, int MOD_KEY, int VK);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int UnregisterHotKey(IntPtr hWnd, int id);

        //OverParse
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int GWL_EXSTYLE = (-20);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool SetForegroundWindow(IntPtr hwnd);

        public static string GetActiveWindowTitle()
        {
            int chars = 256;
            StringBuilder buff = new StringBuilder(chars);
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) { return null; }
            return GetWindowText(handle, buff, chars) > 0 ? buff.ToString() : null;
        }

        /*
        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
        */

        //SnapWindow
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool GetWindowRect(IntPtr hwnd, ref RECT winrect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool GetClientRect(IntPtr hwnd, ref RECT lprect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool ClientToScreen(IntPtr hwnd, ref POINT ppt);


        //SetDarkTheme
        [DllImport("Uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern bool SetWindowTheme(IntPtr hwnd, string str, IntPtr nptr);
    }

    public struct RECT { public int left, top, right, bottom; }
    public struct POINT { public int x, y; }

}
