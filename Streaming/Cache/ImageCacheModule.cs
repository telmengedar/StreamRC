using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Threading;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Cache {

    /// <summary>
    /// module used to cache web images in database
    /// </summary>
    [Dependency(nameof(HttpServiceModule), DependencyType.Type)]
    public class ImageCacheModule : IInitializableModule, IHttpService {
        readonly Context context;
        TimeSpan refreshTime;
        readonly PeriodicTimer timer=new PeriodicTimer();

        readonly object imagelock = new object();
        readonly List<ImageCacheEntry> images=new List<ImageCacheEntry>();
        readonly Dictionary<long, ImageCacheEntry> imagesbyid=new Dictionary<long, ImageCacheEntry>();
        readonly Dictionary<string, ImageCacheEntry> imagesbykey=new Dictionary<string, ImageCacheEntry>();
          
        /// <summary>
        /// creates a new <see cref="ImageCacheModule"/>
        /// </summary>
        /// <param name="context">context used to access modules</param>
        public ImageCacheModule(Context context) {
            this.context = context;
            timer.Elapsed += OnTimerElapsed;
        }

        void OnTimerElapsed() {
            lock(imagelock) {
                for(int i = images.Count - 1; i >= 0; --i) {
                    ImageCacheEntry entry = images[i];
                    entry.LifeTime -= 1.0;
                    if(entry.LifeTime <= 0) {
                        imagesbyid.Remove(entry.Image.ID);
                        if(!string.IsNullOrEmpty(entry.Image.Key))
                            imagesbykey.Remove(entry.Image.Key);
                        images.RemoveAt(i);
                    }
                }
            }
        }

        void RefreshImage(ImageCacheEntry entry) {
            if(DateTime.Now - entry.Image.LastUpdate < refreshTime)
                return;

            byte[] data;
            try {
                using(WebClient client = new WebClient()) {
                    data = client.DownloadData(entry.Image.URL);
                }
            }
            catch(Exception e) {
                Logger.Warning(this, $"Unable to refresh image '{entry.Image.URL}'", e.Message);
                return;
            }

            entry.Image.Data = data;
            context.Database.Update<ImageCacheItem>().Set(i => i.Data == data).Where(i => i.ID == entry.Image.ID).Execute();
        }

        /// <summary>
        /// get data of an image by specifying its image id
        /// </summary>
        /// <param name="imageid">id of image</param>
        /// <returns>image data</returns>
        public byte[] GetImageData(long imageid) {
            lock(imagelock) {
                ImageCacheEntry entry;
                if(!imagesbyid.TryGetValue(imageid, out entry)) {
                    entry = new ImageCacheEntry {
                        Image = context.Database.LoadEntities<ImageCacheItem>().Where(i => i.ID == imageid).Execute().FirstOrDefault(),
                        LifeTime = 300.0
                    };
                    images.Add(entry);
                    imagesbyid[imageid] = entry;
                    if(!string.IsNullOrEmpty(entry.Image.Key))
                        imagesbykey[entry.Image.Key] = entry;
                }
                RefreshImage(entry);
                return entry.Image.Data;
            }
        }

        /// <summary>
        /// get data of an image by specifying its image key
        /// </summary>
        /// <param name="key">key of image</param>
        /// <returns>image data</returns>
        public byte[] GetImageData(string key) {
            lock(imagelock) {
                ImageCacheEntry entry;
                if(!imagesbykey.TryGetValue(key, out entry)) {
                    entry = new ImageCacheEntry {
                        Image = context.Database.LoadEntities<ImageCacheItem>().Where(i => i.Key == key).Execute().FirstOrDefault(),
                        LifeTime = 300.0
                    };
                    images.Add(entry);
                    imagesbyid[entry.Image.ID] = entry;
                    imagesbykey[key] = entry;
                }
                RefreshImage(entry);
                return entry.Image.Data;
            }
        }
        /// <summary>
        /// adds an image entry to the cache
        /// </summary>
        /// <param name="url">url to image data</param>
        /// <param name="key">key to retrieve image (optional)</param>
        /// <returns>id of image in databasee</returns>
        public long AddImage(string url, string key=null) {
            if(string.IsNullOrEmpty(url))
                return -1;

            lock(imagelock) {
                if(!string.IsNullOrEmpty(key) && imagesbykey.ContainsKey(key))
                    return imagesbykey[key].Image.ID;

                long id = context.Database.Load<ImageCacheItem>(i => i.ID).Where(i => i.URL == url).ExecuteScalar<long>();
                if(id != 0)
                    return id;

                return context.Database.Insert<ImageCacheItem>().Columns(i => i.Key, i => i.URL, i => i.LastUpdate).Values(key, url, DateTime.Now - TimeSpan.FromDays(2.0)).ReturnID().Execute();
            }
        }


        /// <summary>
        /// time after which image data is refreshed
        /// </summary>
        public TimeSpan RefreshTime
        {
            get { return refreshTime; }
            set
            {
                if(refreshTime == value)
                    return;
                refreshTime = value;
                context.Settings.Set(this, "RefreshTime", value);
            }
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<ImageCacheItem>();
            refreshTime = context.Settings.Get(this, "RefreshTime", TimeSpan.FromDays(1.0));
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/image", this);
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

            client.WriteStatus(200, "OK");
            client.WriteHeader("Content-Type", MimeTypes.GetMimeType(".png"));
            client.WriteHeader("Content-Length", data.Length.ToString());
            client.EndHeader();
            using (System.IO.Stream stream = client.GetStream())
                stream.Write(data, 0, data.Length);
        }
    }
}