using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Streaming.Statistics {

    [Dependency(nameof(StatisticModule), SpecifierType.Type)]
    [Dependency(ModuleKeys.MainWindow, SpecifierType.Key)]
    public class StatisticManagementInvoker : IModule, IRunnableModule {
        readonly Context context;

        public StatisticManagementInvoker(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Manage.Statistics", (sender, args) => new StatisticManagementWindow(context.GetModule<StatisticModule>()).Show());
        }

        void IRunnableModule.Stop() {
        }
    }
}