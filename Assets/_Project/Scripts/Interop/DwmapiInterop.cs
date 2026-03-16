using System;
using System.Runtime.InteropServices;

namespace SoraTehk.E7Helper.Interop {
    // Matching struct definition for DwmExtendFrameIntoClientArea
    [StructLayout(LayoutKind.Sequential)]
    internal struct MARGINS {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    internal static class DwmapiInterop {
        [DllImport("dwmapi.dll")] internal static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    }
}