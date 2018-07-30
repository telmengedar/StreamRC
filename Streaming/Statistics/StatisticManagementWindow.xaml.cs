using System.Windows;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Streaming.Statistics
{
    /// <summary>
    /// Interaction logic for PollManagementWindow.xaml
    /// </summary>
    [Dependency(ModuleKeys.MainWindow, SpecifierType.Key)]
    public partial class StatisticManagementWindow : Window {
        readonly StatisticModule statisticmodule;

        /// <summary>
        /// creates a new <see cref="StatisticManagementWindow"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StatisticManagementWindow(StatisticModule statisticmodule) {
            this.statisticmodule = statisticmodule;
            InitializeComponent();
        }

        private void cmdDemise_Click(object sender, RoutedEventArgs e) {
            statisticmodule.Increase("Deaths");
        }

        private void cmdBackseat_Click(object sender, RoutedEventArgs e)
        {
            statisticmodule.Increase("Backseats");
        }
    }
}
