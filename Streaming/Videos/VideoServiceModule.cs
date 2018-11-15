using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using StreamRC.Core.Http;

namespace StreamRC.Streaming.Videos {

    [Module(Key="video", AutoCreate = true)]
    public class VideoServiceModule : IHttpService {
        readonly List<StreamVideo> videos=new List<StreamVideo>();
        readonly object videolock = new object();

        /// <summary>
        /// creates a new <see cref="VideoServiceModule"/>
        /// </summary>
        /// <param name="httpservice">access to http service</param>
        public VideoServiceModule(HttpServiceModule httpservice) {
            httpservice.AddServiceHandler("/streamrc/video/videoplayer", this);
            httpservice.AddServiceHandler("/streamrc/video/videoplayer.css", this);
            httpservice.AddServiceHandler("/streamrc/video/videoplayer.js", this);
            httpservice.AddServiceHandler("/streamrc/video/videos", this);
        }

        /// <summary>
        /// adds a video to be played back in overlay
        /// </summary>
        /// <param name="id">id of video to add</param>
        public void AddVideo(string id) {
            AddVideo(id, 0, 0, 0);
        }

        /// <summary>
        /// adds a video to be played back in overlay
        /// </summary>
        /// <param name="id">id of video to add</param>
        /// <param name="startseconds">seconds when to start playing video</param>
        /// <param name="endseconds">time in seconds when to stop playing the video</param>
        /// <param name="volume">volume of video to play</param>
        public void AddVideo(string id, double startseconds, double endseconds, int volume) {
            lock(videolock) {
                if(videos.Count > 128) {
                    Logger.Warning(this, $"Video '{id}' not added. Queue overflow.");
                    return;
                }

                videos.Add(new StreamVideo {
                    Id = id,
                    Timestamp = DateTime.Now,
                    StartSeconds = startseconds,
                    EndSeconds = endseconds,
                    Volume = volume
                });
            }
        }

        public void AddVideo(string service, string user, string id, double startseconds, double endseconds) {
            if(endseconds - startseconds > 30.0) {
                Logger.Warning(this, "Ignoring video call because it seems to be longer than 30 seconds");
                return;
            }

            AddVideo(id, startseconds, endseconds, 0);
        }

        public void StopVideo() {
            lock(videolock) {
                videos.Add(new StreamVideo {
                    Id = "!stop"
                });
            }
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