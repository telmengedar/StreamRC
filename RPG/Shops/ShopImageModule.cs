using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.RPG.Shops {

    /// <summary>
    /// serves images for items
    /// </summary>
    [Dependency(nameof(HttpServiceModule))]
    public class ShopImageModule : IRunnableModule, IHttpService {
        readonly Context context;
        readonly Dictionary<string, string> imagecache=new Dictionary<string, string>();

        public ShopImageModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// get path under which image for item is server (or null when no image exists)
        /// </summary>
        /// <returns>path to image on server</returns>
        public string GetKeeperImage() {

            string path;
            if(!imagecache.TryGetValue("shopkeeper_normal", out path)) {
                string resourcepath = GetType().Namespace + ".Images." + "shopkeeper_normal" + ".png";
                if(ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
                    path = $"http://localhost/streamrc/image/shop/keeper";

                imagecache["shopkeeper_normal"] = path;
            }

            return path;
        }

        void IRunnableModule.Start() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/image/shop/keeper", this);
        }

        void IRunnableModule.Stop() {
            context.GetModule<HttpServiceModule>().RemoveServiceHandler("/streamrc/image/shop/keeper");
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/image/shop/keeper":
                    ServeImage(client, request);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not handled by this module");
            }
        }

        void ServeImage(HttpClient client, HttpRequest request) {

            string resourcepath = GetType().Namespace + ".Images." + "shopkeeper_normal" + ".png";

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