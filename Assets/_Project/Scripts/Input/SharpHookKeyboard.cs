using System.Collections.Concurrent;
using QFSW.QC;
using SharpHook;
using SoraTehk.Collections;
using SoraTehk.Diagnostic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using SHEventType = SharpHook.Data.EventType;
using SHKeyCode = SharpHook.Data.KeyCode;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SoraTehk.E7Helper {
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [InputControlLayout(stateType = typeof(KeyboardState), isGenericTypeOfDevice = true, canRunInBackground = true)]
    public class SharpHookKeyboard : Keyboard {
        static SharpHookKeyboard() {
            InputSystem.RegisterLayout<SharpHookKeyboard>(matches: new InputDeviceMatcher().WithInterface("SharpHookKeyboard"));
            // This ran in the editor, so we need to avoid duplicate
            if (InputSystem.GetDevice<SharpHookKeyboard>() == null!) {
                InputSystem.AddDevice(
                    new InputDeviceDescription {
                        interfaceName = "SharpHookKeyboard", product = "SharpHookInput"
                    }
                );
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad() {
        }

        [Command($"{nameof(SharpHookKeyboard)}.{nameof(m_LoggingKeyboardState)}", MonoTargetType.Registry)]
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        internal bool m_LoggingKeyboardState = false;

        internal readonly IGlobalHook m_Hook;
        internal readonly ConcurrentHashSet<Key> m_PressedKeys;
        internal readonly ConcurrentQueue<Key[]> m_StateQueue;

        public SharpHookKeyboard() : this(new SimpleGlobalHook()) {
        }

        private SharpHookKeyboard(IGlobalHook hook) {
            m_Hook = hook;
            m_PressedKeys = new ConcurrentHashSet<Key>();
            m_StateQueue = new ConcurrentQueue<Key[]>();
        }

        protected override void OnAdded() {
            base.OnAdded();
            InputSystem.onBeforeUpdate += HandleBeforeUpdate;

            m_Hook.KeyPressed += HandleKeyPressed;
            m_Hook.KeyReleased += HandleKeyUp;
            m_Hook.RunAsync();
        }

        protected override void OnRemoved() {
            base.OnRemoved();
            InputSystem.onBeforeUpdate -= HandleBeforeUpdate;

            m_Hook.KeyPressed -= HandleKeyPressed;
            m_Hook.KeyReleased -= HandleKeyUp;
            m_Hook.Stop();
        }

        private void HandleBeforeUpdate() {
            while (m_StateQueue.TryDequeue(out var stateKeys)) {
                if (m_LoggingKeyboardState && stateKeys.Length > 0) {
                    SorLog.LogInfo($".m_StateQueue.Dequeue={string.Join(", ", stateKeys)}");
                }

                InputSystem.QueueStateEvent(this, new KeyboardState(stateKeys));
            }
        }

        private void HandleKeyPressed(object sender, KeyboardHookEventArgs e) {
            // Make sure we have a valid KeyControl equivalent
            var control = (KeyControl?)this[GetUnityKey(e.Data.KeyCode)];
            if (control == null) return;

            // Adding success means it hasn't been pressed before
            if (m_PressedKeys.Add(control.keyCode)) HandleKeyDown(sender, e);
        }

        private void HandleKeyDown(object sender, KeyboardHookEventArgs e) {
            // Make sure we have a valid KeyControl equivalent
            var control = (KeyControl?)this[GetUnityKey(e.Data.KeyCode)];
            if (control == null) return;

            m_PressedKeys.Add(control.keyCode);
            m_StateQueue.Enqueue(m_PressedKeys.ToArray());
        }

        private void HandleKeyUp(object sender, KeyboardHookEventArgs e) {
            // Make sure we have a valid KeyControl equivalent
            var control = (KeyControl?)this[GetUnityKey(e.Data.KeyCode)];
            if (control == null) return;

            if (m_PressedKeys.TryRemove(control.keyCode)) m_StateQueue.Enqueue(m_PressedKeys.ToArray());
        }


        internal static Key GetUnityKey(SHKeyCode keyCode) {
            return keyCode switch {
                // Letters
                SHKeyCode.VcA => Key.A,
                SHKeyCode.VcB => Key.B,
                SHKeyCode.VcC => Key.C,
                SHKeyCode.VcD => Key.D,
                SHKeyCode.VcE => Key.E,
                SHKeyCode.VcF => Key.F,
                SHKeyCode.VcG => Key.G,
                SHKeyCode.VcH => Key.H,
                SHKeyCode.VcI => Key.I,
                SHKeyCode.VcJ => Key.J,
                SHKeyCode.VcK => Key.K,
                SHKeyCode.VcL => Key.L,
                SHKeyCode.VcM => Key.M,
                SHKeyCode.VcN => Key.N,
                SHKeyCode.VcO => Key.O,
                SHKeyCode.VcP => Key.P,
                SHKeyCode.VcQ => Key.Q,
                SHKeyCode.VcR => Key.R,
                SHKeyCode.VcS => Key.S,
                SHKeyCode.VcT => Key.T,
                SHKeyCode.VcU => Key.U,
                SHKeyCode.VcV => Key.V,
                SHKeyCode.VcW => Key.W,
                SHKeyCode.VcX => Key.X,
                SHKeyCode.VcY => Key.Y,
                SHKeyCode.VcZ => Key.Z,

                // Numbers (top row)
                SHKeyCode.Vc0 => Key.Digit0,
                SHKeyCode.Vc1 => Key.Digit1,
                SHKeyCode.Vc2 => Key.Digit2,
                SHKeyCode.Vc3 => Key.Digit3,
                SHKeyCode.Vc4 => Key.Digit4,
                SHKeyCode.Vc5 => Key.Digit5,
                SHKeyCode.Vc6 => Key.Digit6,
                SHKeyCode.Vc7 => Key.Digit7,
                SHKeyCode.Vc8 => Key.Digit8,
                SHKeyCode.Vc9 => Key.Digit9,

                // Function keys
                SHKeyCode.VcF1 => Key.F1,
                SHKeyCode.VcF2 => Key.F2,
                SHKeyCode.VcF3 => Key.F3,
                SHKeyCode.VcF4 => Key.F4,
                SHKeyCode.VcF5 => Key.F5,
                SHKeyCode.VcF6 => Key.F6,
                SHKeyCode.VcF7 => Key.F7,
                SHKeyCode.VcF8 => Key.F8,
                SHKeyCode.VcF9 => Key.F9,
                SHKeyCode.VcF10 => Key.F10,
                SHKeyCode.VcF11 => Key.F11,
                SHKeyCode.VcF12 => Key.F12,
                SHKeyCode.VcF13 => Key.F13,
                SHKeyCode.VcF14 => Key.F14,
                SHKeyCode.VcF15 => Key.F15,
                SHKeyCode.VcF16 => Key.F16,
                SHKeyCode.VcF17 => Key.F17,
                SHKeyCode.VcF18 => Key.F18,
                SHKeyCode.VcF19 => Key.F19,
                SHKeyCode.VcF20 => Key.F20,

                // Arrows
                SHKeyCode.VcUp => Key.UpArrow,
                SHKeyCode.VcDown => Key.DownArrow,
                SHKeyCode.VcLeft => Key.LeftArrow,
                SHKeyCode.VcRight => Key.RightArrow,

                // Special keys
                SHKeyCode.VcSpace => Key.Space,
                SHKeyCode.VcEnter => Key.Enter,
                SHKeyCode.VcEscape => Key.Escape,
                SHKeyCode.VcTab => Key.Tab,
                SHKeyCode.VcBackspace => Key.Backspace,
                SHKeyCode.VcDelete => Key.Delete,
                SHKeyCode.VcInsert => Key.Insert,
                SHKeyCode.VcHome => Key.Home,
                SHKeyCode.VcEnd => Key.End,
                SHKeyCode.VcPageUp => Key.PageUp,
                SHKeyCode.VcPageDown => Key.PageDown,
                SHKeyCode.VcPrintScreen => Key.PrintScreen,
                SHKeyCode.VcPause => Key.Pause,
                //SHKeyCode.VcApps => Key.Menu,

                // Modifiers
                SHKeyCode.VcLeftShift => Key.LeftShift,
                SHKeyCode.VcRightShift => Key.RightShift,
                SHKeyCode.VcLeftControl => Key.LeftCtrl,
                SHKeyCode.VcRightControl => Key.RightCtrl,
                SHKeyCode.VcLeftAlt => Key.LeftAlt,
                SHKeyCode.VcRightAlt => Key.RightAlt,
                SHKeyCode.VcLeftMeta => Key.LeftMeta,
                SHKeyCode.VcRightMeta => Key.RightMeta,
                SHKeyCode.VcCapsLock => Key.CapsLock,
                SHKeyCode.VcNumLock => Key.NumLock,
                SHKeyCode.VcScrollLock => Key.ScrollLock,

                // Numpad
                SHKeyCode.VcNumPad0 => Key.Numpad0,
                SHKeyCode.VcNumPad1 => Key.Numpad1,
                SHKeyCode.VcNumPad2 => Key.Numpad2,
                SHKeyCode.VcNumPad3 => Key.Numpad3,
                SHKeyCode.VcNumPad4 => Key.Numpad4,
                SHKeyCode.VcNumPad5 => Key.Numpad5,
                SHKeyCode.VcNumPad6 => Key.Numpad6,
                SHKeyCode.VcNumPad7 => Key.Numpad7,
                SHKeyCode.VcNumPad8 => Key.Numpad8,
                SHKeyCode.VcNumPad9 => Key.Numpad9,
                SHKeyCode.VcNumPadEnter => Key.NumpadEnter,
                SHKeyCode.VcNumPadAdd => Key.NumpadPlus,
                SHKeyCode.VcNumPadSubtract => Key.NumpadMinus,
                SHKeyCode.VcNumPadMultiply => Key.NumpadMultiply,
                SHKeyCode.VcNumPadDivide => Key.NumpadDivide,
                SHKeyCode.VcNumPadDecimal => Key.NumpadPeriod,
                //SHKeyCode.VcNumPadComma => Key.NumpadComma,
                SHKeyCode.VcNumPadEquals => Key.NumpadEquals,

                // Symbols / punctuation
                SHKeyCode.VcMinus => Key.Minus,
                SHKeyCode.VcEquals => Key.Equals,
                SHKeyCode.VcBackslash => Key.Backslash,
                SHKeyCode.VcOpenBracket => Key.LeftBracket,
                SHKeyCode.VcCloseBracket => Key.RightBracket,
                SHKeyCode.VcSemicolon => Key.Semicolon,
                SHKeyCode.VcQuote => Key.Quote,
                SHKeyCode.VcComma => Key.Comma,
                SHKeyCode.VcPeriod => Key.Period,
                SHKeyCode.VcSlash => Key.Slash,
                SHKeyCode.VcBackQuote => Key.Backquote,

                // Media / system keys
                //SHKeyCode.VcVolumeUp => Key.VolumeUp,
                //SHKeyCode.VcVolumeDown => Key.VolumeDown,
                //SHKeyCode.VcVolumeMute => Key.Mute,
                //SHKeyCode.VcMediaPlay => Key.MediaPlayPause,
                //SHKeyCode.VcMediaStop => Key.MediaStop,
                //SHKeyCode.VcMediaNext => Key.MediaNextTrack,
                //SHKeyCode.VcMediaPrevious => Key.MediaPreviousTrack,

                // OEM / extra keys
                //SHKeyCode.VcOEM1 => Key.OEM1,
                //SHKeyCode.VcOEM2 => Key.OEM2,
                //SHKeyCode.VcOEM3 => Key.OEM3,
                //SHKeyCode.VcOEM4 => Key.OEM4,
                //SHKeyCode.VcOEM5 => Key.OEM5,
                //SHKeyCode.VcOEM6 => Key.OEM6,
                //SHKeyCode.VcOEM7 => Key.OEM7,
                //SHKeyCode.VcOEM8 => Key.OEM8,

                _ => Key.None
            };
        }
    }
}