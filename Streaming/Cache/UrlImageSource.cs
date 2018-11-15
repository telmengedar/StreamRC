using System;
using System.IO;
using System.Net;

namespace StreamRC.Streaming.Cache {

    public class UrlImageSource : IImageSource {

        public UrlImageSource(string url) {
            Key = url;
        }
        public string Key { get; }

        public System.IO.Stream Data
        {
            get
            {
                byte[] data;
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        data = client.DownloadData(Key);
                    }
                }
                catch (Exception)
                {
                    return null;
                }

                return new MemoryStream(data);
            }
        }
    }
}