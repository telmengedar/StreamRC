using System.Windows;

namespace StreamRC.Discord.Configuration
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class DiscordSettings : Window
    {

        /// <summary>
        /// creates new <see cref="DiscordSettings"/> (for designer)
        /// </summary>
        public DiscordSettings()
        {
            InitializeComponent();
        }

        /// <summary>
        /// creates new <see cref="DiscordSettings"/>
        /// </summary>
        /// <param name="bottoken">token used to connect to bot</param>
        /// <param name="chatchannel">id of channel displayed in chat</param>
        public DiscordSettings(string bottoken, string chatchannel)
            : this()
        {
            txtToken.Text = bottoken;
            txtChannel.Text = chatchannel;
        }

        /// <summary>
        /// token used to connect to bot
        /// </summary>
        public string BotToken => txtToken.Text;

        /// <summary>
        /// id of channel displayed in chat
        /// </summary>
        public string ChatChannel => txtChannel.Text;
    }
}
