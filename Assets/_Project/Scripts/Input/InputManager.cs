using System;
using System.Collections.Generic;
using QFSW.QC;
using SaintsField.Playa;
using SoraTehk.Diagnostic;
using SoraTehk.Extensions;
using SoraTehk.Prepare;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SoraTehk.E7Helper {
#if UNITY_EDITOR
    public partial class InputManager : IPreparable {
        public bool Prepare() {
            bool isDirty = false;

            if (InputActionAsset == null) {
                isDirty |= AssetDatabaseX.E_TryFindFirstAsset(out InputActionAsset!, "IA_Default.inputactions");
            }

            return isDirty;
        }
    }
#endif
    public partial class InputManager : MonoBehaviour {
        [Command($"{nameof(InputManager)}.{nameof(m_LoggingClickThroughChange)}", MonoTargetType.Registry)]
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        internal bool m_LoggingClickThroughChange = false;

        [LayoutStart("Assets", ELayout.FoldoutBox)]
        public InputActionAsset InputActionAsset = null!;

        public event Action<bool>? OnClickThroughChanged;

        internal bool m_IsClickThrough = false;

        private void Update() {
            // Default as click-through
            var isClickThroughThisFrame = true;

            // Check for interceptors
            var eventData = new PointerEventData(EventSystem.current) {
                // position = Mouse.current.position.ReadValue()
                // "New" input system doesn't work here
                position = Input.mousePosition,
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results) {
                if (!IsClickThroughInHierarchy(result.gameObject)) {
                    isClickThroughThisFrame = false;
                    break;
                }
            }

            if (m_IsClickThrough != isClickThroughThisFrame) {
                m_IsClickThrough = isClickThroughThisFrame;
                if (m_LoggingClickThroughChange) {
                    SorLog.LogInfo($".{nameof(m_IsClickThrough)}={m_IsClickThrough}");
                }
                OnClickThroughChanged?.Invoke(m_IsClickThrough);
            }
            // Local functions
            bool IsClickThroughInHierarchy(GameObject go) {
                var currTransform = go.transform;

                while (currTransform != null) {
                    if (currTransform.TryGetComponent(out UIClickInterceptor uiInterceptor)) {
                        // If any node in the hierarchy is not click-through, stop here
                        if (!uiInterceptor.IsClickThrough) return false;
                    }

                    currTransform = currTransform.parent;
                }

                // No interceptor found in hierarchy, stay click-through
                return true;
            }
        }
    }
}