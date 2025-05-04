using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class OverlayWindow : MonoBehaviour {
    private struct MARGINS {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }
    
    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private IntPtr m_hWnd;
    
    private void Start() {
        if (Application.isEditor) {
            return;
        }

        m_hWnd = GetActiveWindow();
        SetWindowPos(m_hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
        SetWindowLong(m_hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        MARGINS margins = new MARGINS {
            cxLeftWidth = -1
        };
        DwmExtendFrameIntoClientArea(m_hWnd, ref margins);
    }

    // private void Update() {
    //     bool isOverUi = EventSystem.current.IsPointerOverGameObject();
    //     SetClickThrough(!isOverUi);
    // }

    private void SetClickThrough(bool isClickThrough) {
        if (Application.isEditor) {
            return;
        }
        
        if (isClickThrough) {
            SetWindowLong(m_hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
        else {
            SetWindowLong(m_hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
        }
    }
}