using System;

namespace SoraTehk.E7Helper.Interop {
    internal class Win32Constants {
        internal const int GWL_EXSTYLE = -20;
        internal const uint WS_EX_LAYERED = 0x00080000;
        internal const uint WS_EX_TRANSPARENT = 0x00000020;
        internal static readonly IntPtr HWND_TOPMOST = new(-1);
    }
}