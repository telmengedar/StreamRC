using System;
using System.Collections.Generic;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
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
        public VideoServiceModule(IHttpServiceModule httpservice) {
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

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/video/videoplayer":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Video.videoplayer.html"), ".html");
                    break;
                case "/streamrc/video/videoplayer.css":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Video.videoplayer.css"), ".css");
                    break;
                case "/streamrc/video/videoplayer.js":
                    response.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Video.videoplayer.js"), ".js");
                    break;
                case "/streamrc/video/videos":
                    ServeVideos(response);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeVideos(IHttpResponse response) {
            lock(videolock) {
                response.ServeJSON(new VideoResponse {
                    Timestamp = DateTime.Now,
                    Videos = videos.ToArray()
                });
                videos.Clear();
            }
        }
    }
}