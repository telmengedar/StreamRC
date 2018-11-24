using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.RPG.Emotions {

    [Module(AutoCreate = true)]
    public class EmotionImageModule : IHttpService {
        readonly Dictionary<string, string> imagecache = new Dictionary<string, string>();

        public EmotionImageModule(IHttpServiceModule httpservice)
        {
            httpservice.AddServiceHandler("/streamrc/image/emotion", this);
        }

        /// <summary>
        /// get path under which image for emotion is served (or null when no image exists)
        /// </summary>
        /// <param name="emotion">name of emotion</param>
        /// <returns>path to image on server</returns>
        public string GetImagePath(EmotionType emotion)
        {
            if (!imagecache.TryGetValue(emotion.ToString().ToLower(), out string path))
            {
                string resourcepath = GetType().Namespace + ".Images." + emotion.ToString().ToLower() + ".png";
                if (ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
                    path = $"http://localhost/streamrc/image/emotion?name={emotion.ToString().ToLower().URLEncode()}";

                imagecache[emotion.ToString().ToLower()] = path;
            }

            return path;
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response)
        {
            switch (request.Resource)
            {
                case "/streamrc/image/emotion":
                    ServeImage(request, response);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not handled by this module");
            }
        }

        void ServeImage(IHttpRequest request, IHttpResponse response)
        {
            string itemname = request.GetParameter<string>("name");

            string resourcepath = GetType().Namespace + ".Images." + itemname.ToLower() + ".png";

            if (!ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath)) {
                response.Status = 404;
                return;
            }

            byte[] imagedata = ResourceAccessor.GetResource<byte[]>(resourcepath);

            response.ServeData(imagedata, ".png");
        }

    }
}