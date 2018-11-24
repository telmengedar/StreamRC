using System;
using System.Drawing;
using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Text {


    [Module(AutoCreate = true)]
    public class TextHttpModule : IHttpService {
        readonly TextModule textmodule;

        public TextHttpModule(IHttpServiceModule httpservice, TextModule textmodule) {
            this.textmodule = textmodule;
            httpservice.AddServiceHandler("/streamrc/images/text", this);
        }

        public void ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/images/text":
                    CreateText(request, response);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not served by this service");
            }
        }

        void CreateText(IHttpRequest request, IHttpResponse response) {
            string text = request.GetParameter<string>("text");
            float size = request.HasParameter("size") ? request.GetParameter<float>("size") : 32.0f;
            Color color = request.HasParameter("color") ? request.GetParameter<Color>("color"):Color.White;
            Color outlinecolor = request.HasParameter("outlinecolor") ? request.GetParameter<Color>("outlinecolor") : Color.Black;
            int outlinethickness = request.GetParameter<int>("outlinethickness");
            response.ServeData(textmodule.CreateTextImage(text, size, color, outlinecolor, outlinethickness), ".png");
        }
    }
}