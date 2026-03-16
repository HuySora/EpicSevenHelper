using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoraTehk.E7Helper {
    // TODO: It's odd that SharpHookKeyboard runs exclusively in the background, which means we never receive duplicate input
    public class SharpHookInput : MonoBehaviour {
        // internal bool m_KeyboardDevicesDirty;

        private void OnEnable() {
            InputSystem.onDeviceChange += HandleDeviceChange;
            // DisableOtherKeyboards();
        }
        private void OnDisable() {
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        private void Update() {
            // if (!m_KeyboardDevicesDirty) return;
            // m_KeyboardDevicesDirty = false;
            //
            // DisableOtherKeyboards();
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change) {
            if (device is not Keyboard) return;
            // m_KeyboardDevicesDirty = true;
        }

        // internal void DisableOtherKeyboards() {
        //     // Enable only exact SharpHookKeyboard, disable all other keyboards
        //     var kbs = InputSystem.devices
        //         .OfType<Keyboard>()
        //         //.Where(kb => kb.enabled)
        //         .ToArray();
        //
        //     foreach (var kb in kbs) {
        //         if (kb.GetType() == typeof(SharpHookKeyboard)) {
        //             kb.MakeCurrent();
        //         }
        //         else {
        //             InputSystem.RemoveDevice(kb);
        //         }
        //     }
        // }
    }
}