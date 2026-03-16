using QFSW.QC;
using SaintsField.Playa;
using SoraTehk.Extensions;
using SoraTehk.Prepare;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace SoraTehk.E7Helper {
#if UNITY_EDITOR
    public partial class CoreScope : IPreparable {
        public bool Prepare() {
            bool isDirty = false;

            if (CoreEntryPoint == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out CoreEntryPoint!);
            }
            if (GameWindow == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out GameWindow!);
            }
            if (TextureManager == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out TextureManager!);
            }
            if (ViewManager == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out ViewManager!);
            }
            if (InputManager == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out InputManager!);
            }
            if (UwcWindowCaptureManager == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out UwcWindowCaptureManager!);
            }
            if (EquipmentOcr == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out EquipmentOcr!);
            }

            return isDirty;
        }
    }
#endif
    public partial class CoreScope : LifetimeScope {
        [LayoutStart("Scene", ELayout.FoldoutBox)]
        public CoreEntryPoint CoreEntryPoint = null!;
        public GameWindow GameWindow = null!;
        public TextureManager TextureManager = null!;
        public ViewManager ViewManager = null!;
        public InputManager InputManager = null!;
        public UwcWindowCaptureManager UwcWindowCaptureManager = null!;
        public EquipmentOcr EquipmentOcr = null!;

        protected override void Configure(IContainerBuilder builder) {
            // System
            builder.RegisterComponent(GameWindow);
            builder.RegisterComponent(TextureManager);
            builder.RegisterComponent(ViewManager);
            builder.RegisterComponent(InputManager);
            builder.RegisterComponent(UwcWindowCaptureManager);
            builder.RegisterComponent(EquipmentOcr);

            // Console commands
            QuantumRegistry.RegisterObject(GameWindow);
            QuantumRegistry.RegisterObject(TextureManager);
            QuantumRegistry.RegisterObject(ViewManager);
            QuantumRegistry.RegisterObject(InputManager);
            QuantumRegistry.RegisterObject(UwcWindowCaptureManager);
            QuantumRegistry.RegisterObject(EquipmentOcr);
            // TODO: SharpHookKeyboard could be null here but it would be a fatal error anyway
            QuantumRegistry.RegisterObject(InputSystem.GetDevice<SharpHookKeyboard>());

            // Entry point
            builder.RegisterComponent(CoreEntryPoint).AsImplementedInterfaces();
        }
    }
}