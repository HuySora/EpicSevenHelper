using System;
using System.Runtime.InteropServices;

namespace SoraTehk.E7Helper.Interop {
    public class Msvcrt {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);
    }
}