using System;
using System.Collections.Generic;
using SoraTehk.E7Helper.Interop;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoraTehk.E7Helper {
    public class GameWindow : MonoBehaviour {
        internal IntPtr m_HWnd;

        private void Start() {
            if (Application.isEditor) return;

            m_HWnd = User32Interop.GetActiveWindow();
            // Keep the window in front (overlay)
            User32Interop.SetWindowPos(
                m_HWnd,
                Win32Constants.HWND_TOPMOST,
                0,
                0,
                0,
                0,
                0
            );
            var margins = new MARGINS {
                cxLeftWidth = -1
            };
            DwmapiInterop.DwmExtendFrameIntoClientArea(m_HWnd, ref margins);
            // Make the window default as not click-thought
            SetClickThrough(false);
        }

        public void SetClickThrough(bool isClickThrough) {
            if (Application.isEditor) return;

            User32Interop.SetWindowLong(
                m_HWnd,
                Win32Constants.GWL_EXSTYLE,
                isClickThrough
                    ? Win32Constants.WS_EX_LAYERED | Win32Constants.WS_EX_TRANSPARENT
                    : Win32Constants.WS_EX_LAYERED
            );
        }
    }
}