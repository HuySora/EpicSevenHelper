using System;
using System.IO;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using SaintsField.Playa;
using SoraTehk.Diagnostic;
using SoraTehk.E7Helper.Interop;
using SoraTehk.Extensions;
using SoraTehk.Prepare;
using UnityEngine;
using UnityEngine.UI;
using uWindowCapture;
using VContainer;

namespace SoraTehk.E7Helper {
#if UNITY_EDITOR
    public partial class UwcWindowCaptureManager : IPreparable {
        [Button]
        // ReSharper disable once UnusedMember.Local
        private void SaveBufferTextureToPng() {
            var bytes = m_BufferTexture2D.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/_Project/Template.png", bytes);
        }

        public bool Prepare() {
            bool isDirty = false;

            if (WindowCapture == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out WindowCapture!, "uWindowTexture");
            }
            if (CanvasScaler == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out CanvasScaler!, "Canvas");
            }
            if (CanvasGroup == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out CanvasGroup!, "Canvas");
            }
            if (WindowSyncImage == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out WindowSyncImage!, "Image_UwcWindowSync");
            }

            return isDirty;
        }
    }
#endif

    public partial class UwcWindowCaptureManager : MonoBehaviour {
        [Command($"{nameof(UwcWindowCaptureManager)}.{nameof(LogWindowProperties)}", MonoTargetType.Registry)]
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        internal void LogWindowProperties() {
            if (m_CurrWindow == null) return;

            SorLog.LogInfo($"Resolution={m_CurrWindow.width}x{m_CurrWindow.height}({m_CurrWindow.rawWidth}x{m_CurrWindow.rawHeight}), isMaximized={m_CurrWindow.isMaximized}");
        }

        [LayoutStart("Scene", ELayout.FoldoutBox)]
        public UwcWindowTexture WindowCapture = null!;
        public CanvasScaler CanvasScaler = null!;
        public CanvasGroup CanvasGroup = null!;
        public RawImage WindowSyncImage = null!;

        public event Action<Texture2D>? OnCaptured;

        #region Injected
        [Inject] private TextureManager m_TextureManager = null!;
        #endregion

        internal UwcWindow? m_CurrWindow;
        internal bool m_IsBusy;
        internal Texture2D? m_BufferTexture2D;
        internal Color32[]? m_CurrPixels32;
        internal Color32[]? m_FlippedPixels32;
        internal GCHandle m_GcHandle;
        internal IntPtr m_CurrPixels32Ptr = IntPtr.Zero;

        private void Awake() {
            WindowSyncImage.rectTransform.anchorMin = new Vector2(0, 1);
            WindowSyncImage.rectTransform.anchorMax = new Vector2(0, 1);
            WindowSyncImage.rectTransform.pivot = new Vector2(0, 1);
        }

        private void OnEnable() {
            WindowCapture.onWindowChanged.RemoveListener(HandleWindowChanged);
            WindowCapture.onWindowChanged.AddListener(HandleWindowChanged);
        }

        private void OnDisable() {
            WindowCapture.onWindowChanged.RemoveListener(HandleWindowChanged);
            if (m_CurrWindow != null) m_CurrWindow.onCaptured.RemoveListener(HandleCaptured);
        }

        private void Update() {
            CanvasGroup.alpha = m_CurrWindow is { isMinimized: true } ? 0 : 1;
        }

        private void OnDestroy() {
            if (m_GcHandle.IsAllocated) m_GcHandle.Free();
        }

        private void HandleWindowChanged(UwcWindow newWindow, UwcWindow _) {
            m_CurrWindow?.onCaptured.RemoveListener(HandleCaptured);
            m_CurrWindow = newWindow;
            m_CurrWindow.onCaptured.AddListener(HandleCaptured);
        }

        private void HandleCaptured() {
            if (m_CurrWindow == null) return;
            if (m_IsBusy) return;

            Async().Forget();
            // Local functions
            async UniTask Async() {
                m_IsBusy = true;

                var window = m_CurrWindow!;
                var width = window.texture.width;
                var height = window.texture.height;
                var format = window.texture.format;

                if (m_BufferTexture2D == null || width != m_BufferTexture2D.width || height != m_BufferTexture2D.height) {
                    m_TextureManager.Release(m_BufferTexture2D);
                    m_BufferTexture2D = m_TextureManager.Acquire(width, height, format);
                    m_BufferTexture2D.filterMode = FilterMode.Bilinear;
                    m_CurrPixels32 = m_BufferTexture2D.GetPixels32();
                    await UniTask.NextFrame();
                    if (m_GcHandle.IsAllocated) m_GcHandle.Free();
                    m_GcHandle = GCHandle.Alloc(m_CurrPixels32, GCHandleType.Pinned);
                    m_CurrPixels32Ptr = m_GcHandle.AddrOfPinnedObject();
                }
                if (m_CurrPixels32 == null) {
                    m_IsBusy = false;
                    return;
                }

                Msvcrt.memcpy(m_CurrPixels32Ptr, window.buffer, width * height * sizeof(byte) * 4);

                if (m_FlippedPixels32 == null || m_FlippedPixels32.Length != m_CurrPixels32.Length) {
                    m_FlippedPixels32 = new Color32[m_CurrPixels32.Length];
                }
                for (var y = 0; y < height; y++) {
                    var srcRow = y * width;
                    var destRow = (height - 1 - y) * width;
                    for (var x = 0; x < width; x++) m_FlippedPixels32[destRow + x] = m_CurrPixels32[srcRow + x];
                }

                m_BufferTexture2D.SetPixels32(m_FlippedPixels32);
                m_BufferTexture2D.Apply();

                var canvasScaleX = Screen.width / CanvasScaler.referenceResolution.x;
                var canvasScaleY = Screen.height / CanvasScaler.referenceResolution.y;

                // TODO: Hard-coded for now
                const int titleRectHeight = 39;
                const int fullscreenWidthThreshold = 2559;
                const int fullscreenHeightThreshold = 1368;
                // TODO: Find another way to do this
                bool isMaximized = window.isMaximized || window is { width: >= fullscreenWidthThreshold, height: >= fullscreenHeightThreshold };
                bool isFullScreen = window is { width: > fullscreenWidthThreshold, height: > fullscreenHeightThreshold };
                var position = isFullScreen
                    ? new Vector2(0, 0)
                    : isMaximized
                        ? new Vector2(0, -16)
                        : new Vector2(
                            (window.x + 8) / canvasScaleX,
                            (-window.y - titleRectHeight + 8) / canvasScaleY
                        );
                var scale = new Vector2(
                    (float)m_BufferTexture2D.width / Screen.width,
                    (float)m_BufferTexture2D.height / Screen.height
                );

                WindowSyncImage.texture = m_BufferTexture2D;
                WindowSyncImage.rectTransform.anchoredPosition = position;
                WindowSyncImage.rectTransform.localScale = scale;

                OnCaptured?.Invoke(m_BufferTexture2D);

                m_IsBusy = false;
            }
        }
    }
}