using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core.Http;

namespace StreamRC.RPG.Items {

    /// <summary>
    /// serves images for items
    /// </summary>
    [Module(AutoCreate = true)]
    public class ItemImageModule : IHttpService {
        readonly Dictionary<string, string> imagecache=new Dictionary<string, string>();

        public ItemImageModule(HttpServiceModule httpservice) {
            httpservice.AddServiceHandler("/streamrc/image/item", this);
        }

        /// <summary>
        /// get path under which image for item is server (or null when no image exists)
        /// </summary>
        /// <param name="itemname">name of item</param>
        /// <returns>path to image on server</returns>
        public string GetImagePath(string itemname) {
            if(!imagecache.TryGetValue(itemname.ToLower(), out string path)) {
                string resourcepath = GetType().Namespace + ".Images." + itemname.ToLower() + ".png";
                if(ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
                    path = $"http://localhost/streamrc/image/item?name={HttpExtensions.URLEncode(itemname.ToLower())}";

                imagecache[itemname.ToLower()] = path;
            }

            return path;
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/image/item":
                    ServeImage(client, request);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not handled by this module");
            }
        }

        void ServeImage(HttpClient client, HttpRequest request) {
            string itemname = request.GetParameter("name");
            // TODO: do that right ...
            itemname = itemname.Replace("+", " ");

            string resourcepath = GetType().Namespace + ".Images." + itemname.ToLower() + ".png";

            if(!ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath)) {
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