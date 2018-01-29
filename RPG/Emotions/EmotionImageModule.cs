using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.RPG.Emotions {
    public class EmotionImageModule : IRunnableModule, IHttpService {
        readonly Context context;
        readonly Dictionary<string, string> imagecache = new Dictionary<string, string>();

        public EmotionImageModule(Context context)
        {
            this.context = context;
        }

        /// <summary>
        /// get path under which image for emotion is served (or null when no image exists)
        /// </summary>
        /// <param name="emotion">name of emotion</param>
        /// <returns>path to image on server</returns>
        public string GetImagePath(EmotionType emotion)
        {

            string path;
            if (!imagecache.TryGetValue(emotion.ToString().ToLower(), out path))
            {
                string resourcepath = GetType().Namespace + ".Images." + emotion.ToString().ToLower() + ".png";
                if (ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
                    path = $"http://localhost/streamrc/image/emotion?name={HttpExtensions.URLEncode(emotion.ToString().ToLower())}";

                imagecache[emotion.ToString().ToLower()] = path;
            }

            return path;
        }

        void IRunnableModule.Start()
        {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/image/emotion", this);
        }

        void IRunnableModule.Stop()
        {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/image/emotion");
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request)
        {
            switch (request.Resource)
            {
                case "/streamrc/image/emotion":
                    ServeImage(client, request);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not handled by this module");
            }
        }

        void ServeImage(HttpClient client, HttpRequest request)
        {
            string itemname = request.GetParameter("name");

            string resourcepath = GetType().Namespace + ".Images." + itemname.ToLower() + ".png";

            if (!ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
            {
                client.WriteStatus(404, "Image not found");
                client.EndHeader();
                return;
            }

            byte[] imagedata = ResourceAccessor.GetResource<byte[]>(resourcepath);

            client.WriteStatus(200, "OK");
            client.WriteHeader("Content-Type", MimeTypes.GetMimeType(".png"));
            client.WriteHeader("Content-Length", imagedata.Length.ToString());
            client.EndHeader();
            using (System.IO.Stream stream = client.GetStream())
                stream.Write(imagedata, 0, imagedata.Length);

        }

    }
}