using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Twitch.Chat;

namespace StreamRC.Twitch
{
    /// <summary>
    /// Interaction logic for TwitchConnector.xaml
    /// </summary>
    public partial class TwitchConnector : Window {
        const int port = 40299;
        readonly HttpServer server = new HttpServer(IPAddress.Any, port);

        readonly Context context;

        /// <summary>
        /// creates a new <see cref="TwitchConnector"/>
        /// </summary>
        public TwitchConnector(Context context) {
            this.context = context;
            InitializeComponent();
            server.Request += OnRequest;
            server.Start();
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            server.Stop();
        }

        void OnRequest(HttpClient client, HttpRequest request) {
            client.WriteStatus(200, "OK");

            if (request.Parameters.Any(p => p.Key == "access_token")) {
                string token = request.GetParameter("access_token");
                client.WriteHeader("Content-Length", "0");
                client.EndHeader();

                Dispatcher.Invoke(() => {
                    if(chkBot.IsChecked ?? false)
                        context.GetModule<TwitchBotModule>().SetCredentials(txtAccount.Text, token);
                    else context.GetModule<TwitchChatModule>().SetCredentials(txtAccount.Text, token);
                    Close();
                });
            }
            else if(!request.Parameters.Any()) {
                string page = ResourceAccessor.GetResource<string>("StreamRC.Twitch.Resources.AuthorisationResponse.html");

                client.WriteHeader("Content-Type", "text/html; charset=utf-8");
                client.WriteHeader("Content-Length", page.Length.ToString());
                client.EndHeader();
                using(StreamWriter writer = new StreamWriter(client.GetStream()))
                    writer.Write(page);
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
