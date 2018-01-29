using System;
using System.Drawing;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Text {

    [Dependency(nameof(TextModule), DependencyType.Type)]
    [Dependency(nameof(HttpServiceModule), DependencyType.Type)]
    public class TextHttpModule : IRunnableModule, IHttpService {
        readonly Context context;

        public TextHttpModule(Context context) {
            this.context = context;
        }

        public void Start() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/images/text", this);
        }

        public void Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/images/text");
        }

        public void ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/images/text":
                    CreateText(client, request);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not served by this service");
            }
        }

        void CreateText(HttpClient client, HttpRequest request) {
            string text = request.GetParameter<string>("text");
            float size = request.HasParameter("size") ? request.GetParameter<float>("size") : 32.0f;
            Color color = request.HasParameter("color") ? request.GetParameter<Color>("color"):Color.White;
            Color outlinecolor = request.HasParameter("outlinecolor") ? request.GetParameter<Color>("outlinecolor") : Color.Black;
            int outlinethickness = request.GetParameter<int>("outlinethickness");
            client.ServeData(context.GetModule<TextModule>().CreateTextImage(text, size, color, outlinecolor, outlinethickness), ".png");
        }
    }
}