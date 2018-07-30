using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace Discord {
    public class DiscordOAuth2Module : IRunnableModule, IHttpService {
        readonly Context context;

        const string discordurl = "https://discordapp.com/api/oauth2/authorize?client_id=410528978936266764&permissions=8&redirect_uri=http%3A%2F%2Flocalhost%2Fstreamrc%2Fdiscord%2Foauth2&scope=bot";

        public DiscordOAuth2Module(Context context) {
            this.context = context;
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            client.WriteStatus(200, "OK");

            if (request.Parameters.Any(p => p.Key == "access_token"))
            {
                string token = request.GetParameter("access_token");
                client.WriteHeader("Content-Length", "0");
                client.EndHeader();

                /*Dispatcher.Invoke(() => {
                    module.CreateProfile(txtAccount.Text, token);
                    Close();
                });*/
            }
            else if (!request.Parameters.Any())
            {
                string page = ResourceAccessor.GetResource<string>("StreamRC.Twitch.Resources.AuthorisationResponse.html");

                client.WriteHeader("Content-Type", "text/html; charset=utf-8");
                client.WriteHeader("Content-Length", page.Length.ToString());
                client.EndHeader();
                using (StreamWriter writer = new StreamWriter(client.GetStream()))
                    writer.Write(page);
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/discord/oauth2", this);
        }

        void IRunnableModule.Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/discord/oauth2");
        }
    }
}