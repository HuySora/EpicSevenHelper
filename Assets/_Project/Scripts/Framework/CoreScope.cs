using QFSW.QC;
using SaintsField.Playa;
using SoraTehk.Extensions;
using SoraTehk.Prepare;
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
            if (ViewManager == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out ViewManager!);
            }
            if (InputManager == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out InputManager!);
            }

            return isDirty;
        }
    }
#endif
    public partial class CoreScope : LifetimeScope {
        [LayoutStart("Scene", ELayout.FoldoutBox)]
        public CoreEntryPoint CoreEntryPoint = null!;
        public GameWindow GameWindow = null!;
        public ViewManager ViewManager = null!;
        public InputManager InputManager = null!;

        protected override void Configure(IContainerBuilder builder) {
            // System
            builder.RegisterComponent(GameWindow);
            builder.RegisterComponent(ViewManager);
            builder.RegisterComponent(InputManager);

            // Console commands
            QuantumRegistry.RegisterObject(GameWindow);
            QuantumRegistry.RegisterObject(ViewManager);
            QuantumRegistry.RegisterObject(InputManager);

            // Entry point
            builder.RegisterComponent(CoreEntryPoint).AsImplementedInterfaces();
        }
    }
}