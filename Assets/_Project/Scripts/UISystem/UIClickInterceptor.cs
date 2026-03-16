using SaintsField.Playa;
using UnityEngine;

namespace SoraTehk.E7Helper {
    public class UIClickInterceptor : MonoBehaviour {
        [LayoutStart("Config", ELayout.FoldoutBox)]
        public bool Inherit = true;
        public bool IsClickThrough = false;
    }
}