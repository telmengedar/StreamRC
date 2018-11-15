using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;

namespace StreamRC.Youtube {

    /// <summary>
    /// module providing basic connection details for youtube api
    /// </summary>
    [Module(AutoCreate = true)]
    public class YoutubeModule : IStreamServiceModule, ITimerService
    {
        readonly StreamModule stream;
        readonly TimerModule timer;
        readonly HashSet<YoutubeChatChannel> activestreams = new HashSet<YoutubeChatChannel>();

        /// <summary>
        /// creates a new <see cref="YoutubeModule"/>
        /// </summary>
        /// <param name="context">access to context</param>
        public YoutubeModule(StreamModule stream, TimerModule timer) {
            this.stream = stream;
            this.timer = timer;
            stream.AddService(YoutubeConstants.ServiceName, this);
        }

        public void Start() {
            timer.AddService(this, 60.0);
            Connected?.Invoke();
            Process(0.0);
        }

        public void Stop() {
        }

        public event Action Connected;

        public event Action<UserInformation> NewFollower;

        public event Action<SubscriberInformation> NewSubscriber;

        public Stream ServiceIcon => ResourceAccessor.GetResource<Stream>("StreamRC.Youtube.Resources.yt_icon_rgb.png");

        public async void Process(double time)
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(ResourceAccessor.GetResource<Stream>("StreamRC.Youtube.Resources.key.json")).Secrets, 
                new[] {
                    YouTubeService.Scope.YoutubeReadonly,
                    YouTubeService.Scope.Youtube
                }, 
                "max.gooroo@gmail.com", 
                CancellationToken.None);

            YouTubeService service = new YouTubeService(new BaseClientService.Initializer {
                HttpClientInitializer = credential,
                ApplicationName = "StreamRC"
            });

            LiveBroadcastsResource.ListRequest request = service.LiveBroadcasts.List("id,snippet,status");
            request.BroadcastType = LiveBroadcastsResource.ListRequest.BroadcastTypeEnum.Persistent;
            request.BroadcastStatus = LiveBroadcastsResource.ListRequest.BroadcastStatusEnum.Active;
            LiveBroadcastListResponse response = request.Execute();
            foreach (LiveBroadcast broadcast in response.Items) {
                if(activestreams.Any(c => c.Name == broadcast.Snippet.LiveChatId))
                    continue;

                Logger.Info(this, $"Found new broadcast {broadcast.Id}");
                YoutubeChatChannel chatchannel = new YoutubeChatChannel(service, broadcast);
                activestreams.Add(chatchannel);
                stream.AddChannel(chatchannel);
                timer.LateAdd(chatchannel);
            }

            string[] activeids = response.Items.Select(b => b.Snippet.LiveChatId).ToArray();
            
            foreach(YoutubeChatChannel stoppedchannel in activestreams.Where(c => !activeids.Contains(c.Name)).ToArray()) {
                timer.RemoveService(stoppedchannel);
                stream.RemoveChannel(stoppedchannel);
                activestreams.Remove(stoppedchannel);
            }
        }
    }
}