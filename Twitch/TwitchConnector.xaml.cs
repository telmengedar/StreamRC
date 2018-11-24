using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using StreamRC.Core.Http;
using StreamRC.Twitch.Chat;

namespace StreamRC.Twitch
{
    /// <summary>
    /// Interaction logic for TwitchConnector.xaml
    /// </summary>
    public partial class TwitchConnector : Window, IHttpService {
        readonly TwitchBotModule botmodule;
        readonly TwitchChatModule chatmodule;
        readonly IHttpServiceModule httpservice;

        const int port = 40299;
        /// <summary>
        /// creates a new <see cref="TwitchConnector"/>
        /// </summary>
        public TwitchConnector(TwitchBotModule botmodule, TwitchChatModule chatmodule, IHttpServiceModule httpservice) {
            this.botmodule = botmodule;
            this.chatmodule = chatmodule;
            this.httpservice = httpservice;
            InitializeComponent();
            httpservice.AddServiceHandler("/streamrc/twitch/token", this);
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            httpservice.RemoveServiceHandler("/streamrc/twitch/token");
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            if (request.HasParameter("access_token")) {
                string token = request.GetParameter<string>("access_token");

                Dispatcher.Invoke(() => {
                    if(chkBot.IsChecked ?? false)
                        botmodule.SetCredentials(txtAccount.Text, token);
                    else chatmodule.SetCredentials(txtAccount.Text, token);
                    Close();
                });
            }
            else if(!request.Query.HasKeys()) {
                response.ServeResource(GetType().Assembly, "StreamRC.Twitch.Resources.AuthorisationResponse.html");
            }
        }

        void OnAccountKeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter) {
                lblStatus.Content = "Waiting for OAuth token";
                Process.Start($"https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id={TwitchConstants.ClientID}&redirect_uri=http://localhost:{port}/twitchrc/&scope={TwitchConstants.RequiredScopes}");
                txtAccount.IsEnabled = false;
            }
        }
    }
}
