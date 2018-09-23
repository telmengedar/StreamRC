using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Http;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Videos {

    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(HttpServiceModule))]
    [ModuleKey("video")]
    public class VideoServiceModule : IInitializableModule, IHttpService {
        readonly Context context;

        readonly List<StreamVideo> videos=new List<StreamVideo>();
        readonly object videolock = new object();

        /// <summary>
        /// creates a new <see cref="VideoServiceModule"/>
        /// </summary>
        /// <param name="context">access to module</param>
        public VideoServiceModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// adds a video to be played back in overlay
        /// </summary>
        /// <param name="id">id of video to add</param>
        public void AddVideo(string id) {
            AddVideo(id, 0, 0);
        }

        /// <summary>
        /// adds a video to be played back in overlay
        /// </summary>
        /// <param name="id">id of video to add</param>
        /// <param name="startseconds">seconds when to start playing video</param>
        /// <param name="endseconds">time in seconds when to stop playing the video</param>
        public void AddVideo(string id, double startseconds, double endseconds) {
            lock(videolock) {
                if(videos.Count > 128) {
                    Logger.Warning(this, $"Video '{id}' not added. Queue overflow.");
                    return;
                }

                videos.Add(new StreamVideo {
                    Id = id,
                    Timestamp = DateTime.Now,
                    StartSeconds = startseconds,
                    EndSeconds = endseconds
                });
            }
        }

        void IInitializableModule.Initialize() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/video/videoplayer", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/video/videoplayer.css", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/video/videoplayer.js", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/video/videos", this);
        }

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/video/videoplayer":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Video.videoplayer.html"), ".html");
                    break;
                case "/streamrc/video/videoplayer.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Video.videoplayer.css"), ".css");
                    break;
                case "/streamrc/video/videoplayer.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Video.videoplayer.js"), ".js");
                    break;
                case "/streamrc/video/videos":
                    ServeVideos(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeVideos(HttpClient client, HttpRequest request) {
            lock(videolock) {
                client.ServeData(JSON.WriteString(new VideoResponse {
                    Timestamp = DateTime.Now,
                    Videos = videos.ToArray()
                }), MimeTypes.GetMimeType(".json"));
                videos.Clear();
            }
        }
    }
}