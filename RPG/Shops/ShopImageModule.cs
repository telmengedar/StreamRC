using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.RPG.Shops {

    /// <summary>
    /// serves images for items
    /// </summary>
    [Module(AutoCreate = true)]
    public class ShopImageModule : IHttpService {
        readonly Dictionary<string, string> imagecache=new Dictionary<string, string>();

        public ShopImageModule(IHttpServiceModule httpservice) {
            httpservice.AddServiceHandler("/streamrc/image/shop/keeper", this);
        }

        /// <summary>
        /// get path under which image for item is server (or null when no image exists)
        /// </summary>
        /// <returns>path to image on server</returns>
        public string GetKeeperImage() {
            if(!imagecache.TryGetValue("shopkeeper_normal", out string path)) {
                string resourcepath = GetType().Namespace + ".Images." + "shopkeeper_normal" + ".png";
                if(ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
                    path = $"http://localhost/streamrc/image/shop/keeper";

                imagecache["shopkeeper_normal"] = path;
            }

            return path;
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/image/shop/keeper":
                    ServeImage(request, response);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not handled by this module");
            }
        }

        void ServeImage(IHttpRequest request, IHttpResponse response) {

            string resourcepath = GetType().Namespace + ".Images." + "shopkeeper_normal" + ".png";

            if(!ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath)) {
                response.Status = 404;
                return;
            }

            byte[] imagedata = ResourceAccessor.GetResource<byte[]>(resourcepath);

            response.ServeData(imagedata, ".png");
        }
    }
}