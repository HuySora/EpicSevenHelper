using System;
using System.Runtime.InteropServices;

namespace SoraTehk.E7Helper.Interop {
    internal static class User32Interop {
        [DllImport("user32.dll")] internal static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}