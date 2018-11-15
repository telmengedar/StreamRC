using System.Drawing;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core.Http;
using StreamRC.Streaming.Text;

namespace StreamRC.Streaming.Games {

    [Module(AutoCreate = true)]
    public class GamesHttpModule : IHttpService {
        readonly CurrentlyPlayedModule currentlyplayed;
        readonly TextModule text;

        public GamesHttpModule(HttpServiceModule httpservice, CurrentlyPlayedModule currentlyplayed, TextModule text) {
            this.currentlyplayed = currentlyplayed;
            this.text = text;
            httpservice.AddServiceHandler("/streamrc/images/games/current", this);
            httpservice.AddServiceHandler("/streamrc/images/games/epithet", this);
        }

        public void ProcessRequest(HttpClient client, HttpRequest request) {
            float size = 32.0f;
            if(request.HasParameter("size"))
                size = request.GetParameter<float>("size");

            Color textcolor = request.HasParameter("color") ? request.GetParameter<Color>("color") : Color.White;
            int outlinethickness = request.GetParameter<int>("outlinethickness");
            Color outlinecolor = request.HasParameter("outlinecolor") ? request.GetParameter<Color>("outlinecolor") : Color.Black;

            CurrentlyPlayedGame game = currentlyplayed.CurrentGame;
            string gamename = request.Resource.EndsWith("current") ? game.Game : game.Epithet;
            client.ServeData(text.CreateTextImage(gamename, size, textcolor, outlinecolor, outlinethickness), ".png");
        }
    }
}