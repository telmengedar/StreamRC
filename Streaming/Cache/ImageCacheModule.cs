using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Database.Entities.Operations.Prepared;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core;
using StreamRC.Core.Http;
using StreamRC.Core.Timer;

namespace StreamRC.Streaming.Cache {

    /// <summary>
    /// module used to cache web images in database
    /// </summary>
    [Module(AutoCreate = true)]
    public class ImageCacheModule : IHttpService, ITimerService {
        readonly DatabaseModule database;

        readonly object imagelock = new object();
        readonly Dictionary<long, ImageCacheItem> imagesbyid=new Dictionary<long, ImageCacheItem>();
        readonly Dictionary<string, ImageCacheItem> imagesbykey=new Dictionary<string, ImageCacheItem>();

        readonly PreparedOperation updateexpiration;
        readonly PreparedLoadEntitiesOperation<ImageCacheItem> loadimage;
        readonly PreparedLoadEntitiesOperation<ImageCacheItem> loadexpired;
        readonly PreparedOperation insertimage;
        readonly PreparedLoadValuesOperation loadimageid;
        readonly PreparedOperation deleteimages;

        static ImageCacheModule() {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// creates a new <see cref="ImageCacheModule"/>
        /// </summary>
        /// <param name="context">context used to access modules</param>
        public ImageCacheModule(DatabaseModule database, HttpServiceModule httpservice, TimerModule timer) {
            this.database = database;
            

            database.Database.UpdateSchema<ImageCacheItem>();
            DateTime expiration = DateTime.Now + TimeSpan.FromMinutes(30.0);
            database.Database.Update<ImageCacheItem>().Set(i => i.Expiration == expiration).Execute();

            updateexpiration = database.Database.Update<ImageCacheItem>()
                .Set(i => i.Expiration == DBParameter<DateTime>.Value)
                .Where(i => i.ID == DBParameter.Int64)
                .Prepare();

            loadimage = database.Database.LoadEntities<ImageCacheItem>().Where(i => i.Key == DBParameter.String).Limit(1).Prepare();
            loadimageid = database.Database.Load<ImageCacheItem>(i => i.ID).Where(i => i.Key == DBParameter.String).Prepare();
            insertimage = database.Database.Insert<ImageCacheItem>().Columns(i => i.Key, i => i.Expiration, i => i.Data).Prepare();
            loadexpired = database.Database.LoadEntities<ImageCacheItem>().Where(i => i.Expiration < DBParameter<DateTime>.Value).Prepare();
            deleteimages = database.Database.Delete<ImageCacheItem>().Where(i => DBParameter<long[]>.Value.Contains(i.ID)).Prepare();

            httpservice.AddServiceHandler("/streamrc/image", this);
            timer.AddService(this, 300.0);
        }

        /// <summary>
        /// get data of an image by specifying its image id
        /// </summary>
        /// <param name="imageid">id of image</param>
        /// <returns>image data</returns>
        public byte[] GetImageData(long imageid) {
            lock(imagelock) {
                DateTime expiration = DateTime.Now + TimeSpan.FromMinutes(5.0);
                if (imagesbyid.TryGetValue(imageid, out ImageCacheItem item)) {
                    updateexpiration.Execute(expiration, item.ID);
                    return item.Data;
                }

                return null;
            }
        }

        public long GetImageByResource(Assembly assembly, string path) {
            return GetImage(new ResourceImage(assembly, path));
        }

        public long GetImageByUrl(string url) {
            if (string.IsNullOrEmpty(url))
                return 0;
            return GetImage(new UrlImageSource(url));
        }

        public long GetImage(IImageSource source) {
            lock (imagelock) {
                DateTime expiration = DateTime.Now + TimeSpan.FromMinutes(5.0);
                if (imagesbykey.TryGetValue(source.Key, out ImageCacheItem item)) {
                    updateexpiration.Execute(expiration, item.ID);
                    return item.ID;
                }

                item = loadimage.Execute(source.Key).FirstOrDefault();
                if (item != null) {
                    updateexpiration.Execute(expiration, item.ID);
                    imagesbyid[item.ID] = item;
                    imagesbykey[item.Key] = item;
                    return item.ID;
                }

                System.IO.Stream stream = source.Data;
                byte[] data=null;
                if(stream!=null)
                    using(MemoryStream ms = new MemoryStream()) {
                        stream.CopyTo(ms);
                        data = ms.ToArray();
                    }

                insertimage.Execute(source.Key, expiration, data);

                item = new ImageCacheItem {
                    ID = loadimageid.ExecuteScalar<long>(source.Key),
                    Key = source.Key,
                    Expiration = expiration,
                    Data = data
                };

                imagesbyid[item.ID] = item;
                imagesbykey[item.Key] = item;
                return item.ID;
            }
        }

        /// <summary>
        /// creates an url to use to access a specific image
        /// </summary>
        /// <param name="id">id of image in cache</param>
        /// <returns>url to be used to access image</returns>
        public string CreateCacheUrl(long id) {
            return $"/streamrc/image?id={id}";
        }

        /// <summary>
        /// extracts the id of an url which points to an image in this cache
        /// </summary>
        /// <param name="url">url under which cache image is stored</param>
        /// <returns>id of image</returns>
        public long ExtractIDFromUrl(string url) {
            Match match = Regex.Match(url, "^/streamrc/image\\?id=(?<id>[0-9]+)$");
            if(!match.Success)
                return -1;

            return long.Parse(match.Groups["id"].Value);
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/image":
                    ServeImage(client, request);
                    break;
            }
        }

        void ServeImage(HttpClient client, HttpRequest request) {
            long id = request.GetParameter<long>("id");

            byte[] data = GetImageData(id);

            if(data == null || data.Length == 0) {
                client.WriteStatus(404, "Not found");
                client.EndHeader();
            }
            else {
                client.WriteStatus(200, "OK");
                client.WriteHeader("Content-Type", MimeTypes.GetMimeType(".png"));
                client.WriteHeader("Content-Length", data.Length.ToString());
                client.EndHeader();
                using (System.IO.Stream stream = client.GetStream())
                    stream.Write(data, 0, data.Length);
            }
        }

        void ITimerService.Process(double time) {
            lock (imagelock) {
                ImageCacheItem[] expired = loadexpired.Execute(DateTime.Now).ToArray();

                if(expired.Length == 0)
                    return;

                foreach (ImageCacheItem item in expired) {
                    imagesbyid.Remove(item.ID);
                    if(!string.IsNullOrEmpty(item.Key))
                        imagesbykey.Remove(item.Key);
                }

                long[] ids = expired.Select(i => i.ID).ToArray();
                deleteimages.Execute(ids);
            }
        }
    }
}