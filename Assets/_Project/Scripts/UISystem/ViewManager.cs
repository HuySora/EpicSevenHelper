using UnityEngine;
using VContainer;

namespace SoraTehk.E7Helper {
    public class ViewManager : MonoBehaviour {
        #region Injected
        [Inject] private GameWindow m_GameWindow = null!;
        [Inject] private InputManager m_InputManager = null!;
        #endregion

        private void OnEnable() {
            m_InputManager.OnClickThroughChanged += HandleClickThroughChanged;
        }

        private void OnDisable() {
            m_InputManager.OnClickThroughChanged -= HandleClickThroughChanged;
        }

        private void HandleClickThroughChanged(bool isClickThrough) {
            m_GameWindow.SetClickThrough(isClickThrough);
        }
    }
}