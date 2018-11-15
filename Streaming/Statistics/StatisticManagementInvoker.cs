using NightlyCode.Modules;
using StreamRC.Core.UI;

namespace StreamRC.Streaming.Statistics {

    [Module(AutoCreate = true)]
    public class StatisticManagementInvoker {

        public StatisticManagementInvoker(IMainWindow mainwindow, StatisticModule statistics) {
            mainwindow.AddMenuItem("Manage.Statistics", (sender, args) => new StatisticManagementWindow(statistics).Show());
        }
    }
}