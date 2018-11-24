using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using StreamRC.Core.Http;

namespace StreamRC.RPG.Items {

    /// <summary>
    /// serves images for items
    /// </summary>
    [Module(AutoCreate = true)]
    public class ItemImageModule : IHttpService {
        readonly Dictionary<string, string> imagecache=new Dictionary<string, string>();

        public ItemImageModule(IHttpServiceModule httpservice) {
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
                if (ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath))
                    path = $"http://localhost/streamrc/image/item?name={itemname.ToLower().URLEncode()}";

                imagecache[itemname.ToLower()] = path;
            }

            return path;
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/image/item":
                    ServeImage(request, response);
                    break;
                default:
                    throw new Exception($"'{request.Resource}' not handled by this module");
            }
        }

        void ServeImage(IHttpRequest request, IHttpResponse response) {
            string itemname = request.GetParameter<string>("name");
            // TODO: do that right ...
            itemname = itemname.Replace("+", " ");

            string resourcepath = GetType().Namespace + ".Images." + itemname.ToLower() + ".png";

            if(!ResourceAccessor.ContainsResource(GetType().Assembly, resourcepath)) {
                response.Status = 404;
                return;
            }

            byte[] imagedata = ResourceAccessor.GetResource<byte[]>(resourcepath);

            response.ServeData(imagedata, ".png");
        }
    }
}