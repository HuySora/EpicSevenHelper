using SaintsField.Playa;
using UnityEngine;

namespace SoraTehk.E7Helper {
    public class EditorOnly : MonoBehaviour {
        [LayoutStart("Scene", ELayout.FoldoutBox)]
        public MonoBehaviour[] Behaviours = null!;

        private void Start() {
#if !UNITY_EDITOR
            if (Behaviours.Length > 0) {
                foreach (var bh in Behaviours) {
                    bh.enabled = false;
                }
            }
            else {
                gameObject.SetActive(false);
            }
#endif
        }
    }
}