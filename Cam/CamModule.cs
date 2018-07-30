using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Cam {

    [Dependency(ModuleKeys.MainWindow, SpecifierType.Key)]
    public class CamModule : IInitializableModule {
        readonly Context context;

        public CamModule(Context context) {
            this.context = context;
        }

        public void Initialize() {
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Display.Camera", (sender, args) => ShowWindow());
        }

        void ShowWindow() {
            CaptureWindow window=new CaptureWindow();
            //window.txtFrames = context.Settings.Get(this, "FrameDelay", 0).ToString();
            window.Show();
        }
    }
}