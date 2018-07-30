using System.Drawing;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;
using StreamRC.Streaming.Text;

namespace StreamRC.Streaming.Games {

    [Dependency(nameof(CurrentlyPlayedModule))]
    [Dependency(nameof(TextModule))]
    [Dependency(nameof(HttpServiceModule))]
    public class GamesHttpModule : IRunnableModule, IHttpService {
        readonly Context context;

        public GamesHttpModule(Context context) {
            this.context = context;
        }

        void IRunnableModule.Start() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/images/games/current", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/images/games/epithet", this);
        }

        void IRunnableModule.Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/images/games/current");
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/images/games/epithet");
        }

        public void ProcessRequest(HttpClient client, HttpRequest request) {
            float size = 32.0f;
            if(request.HasParameter("size"))
                size = request.GetParameter<float>("size");

            Color textcolor = request.HasParameter("color") ? request.GetParameter<Color>("color") : Color.White;
            int outlinethickness = request.GetParameter<int>("outlinethickness");
            Color outlinecolor = request.HasParameter("outlinecolor") ? request.GetParameter<Color>("outlinecolor") : Color.Black;

            CurrentlyPlayedGame game = context.GetModule<CurrentlyPlayedModule>().CurrentGame;
            string gamename = request.Resource.EndsWith("current") ? game.Game : game.Epithet;
            client.ServeData(context.GetModule<TextModule>().CreateTextImage(gamename, size, textcolor, outlinecolor, outlinethickness), ".png");
        }
    }
}