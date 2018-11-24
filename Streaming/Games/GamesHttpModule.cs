using System.Drawing;
using NightlyCode.Modules;
using StreamRC.Core.Http;
using StreamRC.Streaming.Text;

namespace StreamRC.Streaming.Games {

    [Module(AutoCreate = true)]
    public class GamesHttpModule : IHttpService {
        readonly CurrentlyPlayedModule currentlyplayed;
        readonly TextModule text;

        public GamesHttpModule(IHttpServiceModule httpservice, CurrentlyPlayedModule currentlyplayed, TextModule text) {
            this.currentlyplayed = currentlyplayed;
            this.text = text;
            httpservice.AddServiceHandler("/streamrc/images/games/current", this);
            httpservice.AddServiceHandler("/streamrc/images/games/epithet", this);
        }

        public void ProcessRequest(IHttpRequest request, IHttpResponse response) {
            float size = 32.0f;
            if(request.HasParameter("size"))
                size = request.GetParameter<float>("size");

            Color textcolor = request.HasParameter("color") ? request.GetParameter<Color>("color") : Color.White;
            int outlinethickness = request.GetParameter<int>("outlinethickness");
            Color outlinecolor = request.HasParameter("outlinecolor") ? request.GetParameter<Color>("outlinecolor") : Color.Black;

            CurrentlyPlayedGame game = currentlyplayed.CurrentGame;
            string gamename = request.Resource.EndsWith("current") ? game.Game : game.Epithet;
            byte[] data = text.CreateTextImage(gamename, size, textcolor, outlinecolor, outlinethickness);
            response.ContentType = MimeTypes.GetMimeType(".png");
            response.Content.Write(data, 0, data.Length);
        }
    }
}