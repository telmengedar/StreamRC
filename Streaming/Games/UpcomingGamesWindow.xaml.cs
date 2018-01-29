using System.Windows;

namespace StreamRC.Streaming.Games
{
    /// <summary>
    /// Interaction logic for UpcomingGames.xaml
    /// </summary>
    public partial class UpcomingGamesWindow : Window
    {
        public UpcomingGamesWindow(UpcomingGamesModule module) {
            InitializeComponent();

            grdGames.ItemsSource = module.Games;
        }
    }
}
