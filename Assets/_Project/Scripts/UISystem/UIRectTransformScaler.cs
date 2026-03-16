using SaintsField.Playa;
using SoraTehk.Prepare;
using UnityEngine;

namespace SoraTehk.E7Helper {
#if UNITY_EDITOR
    public partial class UIRectTransformScaler : IPreparable {
        public bool Prepare() {
            bool isDirty = false;

            if (RectTransform == null) {
                isDirty |= TryGetComponent(out RectTransform!);
            }
            if (ParentRectTransform == null) {
                ParentRectTransform = GetComponentInParent<RectTransform>();
                isDirty |= ParentRectTransform != null;
            }

            return isDirty;
        }
    }
#endif

    [RequireComponent(typeof(RectTransform))]
    public partial class UIRectTransformScaler : MonoBehaviour {
        [LayoutStart("Scene", ELayout.FoldoutBox)]
        public RectTransform RectTransform = null!;
        public RectTransform ParentRectTransform = null!;

        [LayoutStart("Runtime", ELayout.FoldoutBox)]
        public Vector2 InitialAnchoredPositionRatio;
        public Vector2 InitialSizeRatio;

        private void Awake() {
            var parentSize = ParentRectTransform.rect.size;
            var rectSize = RectTransform.rect.size;
            var anchor = (RectTransform.anchorMin + RectTransform.anchorMax) * 0.5f;

            // Convert anchoredPosition to a bottom-left corner in the parent space (anchor/pivot agnostic)
            var pivotPos = RectTransform.anchoredPosition + anchor * parentSize;
            var bottomLeft = pivotPos - RectTransform.pivot * rectSize;

            InitialSizeRatio = rectSize / parentSize;
            InitialAnchoredPositionRatio = bottomLeft / parentSize;
        }

        private void Update() {
            var parentSize = ParentRectTransform.rect.size;
            var rectSize = parentSize * InitialSizeRatio;
            var anchor = (RectTransform.anchorMin + RectTransform.anchorMax) * 0.5f;

            // Reconstruct anchoredPosition from a bottom-left ratio accounting for current anchor and pivot
            var bottomLeft = InitialAnchoredPositionRatio * parentSize;
            var pivotPos = bottomLeft + RectTransform.pivot * rectSize;
            RectTransform.anchoredPosition = pivotPos - anchor * parentSize;

            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectSize.x);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectSize.y);
        }

        public float GetAnchoredX() {
            return RectTransform.anchoredPosition.x;
        }
        public float GetAnchoredY() {
            return RectTransform.anchoredPosition.y;
        }
        public float GetWidth() {
            return RectTransform.rect.width;
        }
        public float GetHeight() {
            return RectTransform.rect.height;
        }
    }
}