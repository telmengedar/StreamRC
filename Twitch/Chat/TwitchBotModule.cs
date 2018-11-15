using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Modules;
using NightlyCode.Twitch.Api;
using NightlyCode.Twitch.Chat;
using NightlyCode.Twitch.V5;
using StreamRC.Core;
using StreamRC.Core.Settings;
using StreamRC.Core.Timer;
using StreamRC.Core.UI;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;
using Logger = NightlyCode.Core.Logs.Logger;
using Stream = System.IO.Stream;
using User = StreamRC.Streaming.Users.User;

namespace StreamRC.Twitch.Chat {

    /// <summary>
    /// interface to twitch
    /// </summary>
    [Module(Key="twitchbot", AutoCreate = true)]
    public class TwitchBotModule : IStreamServiceModule, ITimerService, IStreamStatsModule {
        readonly TwitchChatModule chatmodule;
        readonly StreamModule stream;
        readonly UserModule usermodule;
        readonly ISettings settings;
        readonly ImageCacheModule imagecache;

        readonly ChatClient chatclient = new ChatClient();

        string botname;
        string accesstoken;

        bool isconnected;
        bool isreconnecting;

        NightlyCode.Twitch.Api.User userdata;
        Channel channeldata;

        TwitchApi twitchapi;
        Channels channels;

        int cooldown;

        double followercheck;
        double viewercheck = 180.0f;

        bool increaseflag;
        int viewers;

        bool broadcastlive;

        /// <summary>
        /// creates a new <see cref="TwitchBotModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public TwitchBotModule(IMainWindow mainwindow, TwitchChatModule chatmodule, StreamModule stream, DatabaseModule database, UserModule users, ISettings settings, TimerModule timer, ImageCacheModule imagecache) {
            this.chatmodule = chatmodule;
            this.stream = stream;
            this.usermodule = users;
            this.settings = settings;
            this.imagecache = imagecache;
            chatclient.Disconnected += () => Disconnect(true);
            chatclient.Reconnect += () => {
                Logger.Info(this, "Reconnect message received");
                Disconnect(true);
            };

            chatclient.ChannelJoined += OnChannelJoined;
            chatclient.ChannelLeft += OnChannelLeft;

            database.Database.UpdateSchema<TwitchProfile>();

            users.BeginInitialization(TwitchConstants.ServiceKey);

            stream.AddService(TwitchConstants.ServiceKey, this);

            mainwindow.AddMenuItem("Profiles.Twitch.Connect", (sender, args) => new TwitchConnector(this, chatmodule).ShowDialog());

            botname = settings.Get<string>(this, "botname", null);
            accesstoken = settings.Get<string>(this, "token", null);

            if (!string.IsNullOrEmpty(botname) && !string.IsNullOrEmpty(accesstoken))
                Connect();

            timer.AddService(this, 5.0);

        }

        public bool Live
        {
            get => broadcastlive;
            set
            {
                if(broadcastlive == value)
                    return;
                broadcastlive = value;
                OnLiveStatusChanged(value);
            }
        }

        public event Action<bool> LiveStatusChanged;

        public event Action Connected;

        public event Action<UserInformation> NewFollower;

        public event Action<SubscriberInformation> NewSubscriber;

        /// <summary>
        /// access to the twitch api as used by the bot
        /// </summary>
        public TwitchApi Api => twitchapi;

        /// <summary>
        /// data for user the bot is working for
        /// </summary>
        public NightlyCode.Twitch.Api.User UserData => userdata;

        public void Disconnect(bool reconnect) {
            stream.RemoveChannels(c=>c is TwitchBotChat);
            isconnected = false;

            if (reconnect && !isreconnecting)
                new Task(Reconnect).Start();
        }

        void Reconnect()
        {
            isreconnecting = true;
            Logger.Info(this, "Reconnecting in 5 seconds.");
            Thread.Sleep(5000);
            try
            {
                Connect();
                isreconnecting = false;
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error reconnecting", e);
                new Task(Reconnect).Start();
            }
        }

        void OnChannelJoined(ChatChannel channelsource)
        {
            Logger.Info(this, $"Connected to channel '{channelsource.Name}'");
            stream.AddChannel(new TwitchBotChat(channelsource, usermodule, imagecache));
        }

        void OnChannelLeft(ChatChannel channelsource)
        {
            Logger.Info(this, $"Disconnected from channel '{channelsource.Name}'");
            stream.RemoveChannel(TwitchConstants.ServiceKey, channelsource.Name);
        }

        public void Connect() {
            if(isconnected) {
                Logger.Warning(this, "Already connected");
                return;
            }

            string chatchannel = chatmodule.Username;
            string usertoken = chatmodule.AccessToken;
            if(string.IsNullOrEmpty(usertoken)) {
                Logger.Warning(this, "User not connected");
                return;
            }

            twitchapi = new TwitchApi(TwitchConstants.ClientID, usertoken);
            channels = new Channels(TwitchConstants.ClientID, usertoken);
            channeldata = channels.GetChannel();

            userdata = twitchapi.GetUsersByLogin(chatchannel).Data.FirstOrDefault();
            if(userdata == null)
                Logger.Warning(this, $"No userdata found for '{chatchannel}'");
            
            if (string.IsNullOrEmpty(botname) || string.IsNullOrEmpty(accesstoken))
            {
                Logger.Warning(this, "No credentials set");
                return;
            }

            isconnected = true;
            Connected?.Invoke();
        }

        void OnLiveStatusChanged(bool live) {
            if(live) {
                string chatchannel = chatmodule.Username;
                if (!string.IsNullOrEmpty(chatchannel))
                {
                    Logger.Info(this, $"Connecting to @{botname} to #{chatchannel}");
                    chatclient.Connect(botname, accesstoken);
                    chatclient.Join(chatchannel);
                }
            }
            else {
                Logger.Info(this, "Disconnecting bot chat");
                chatclient.Disconnect();
            }
            LiveStatusChanged?.Invoke(live);
        }

        IEnumerable<SubscriberInformation> GetSubscribers() {
            if (channeldata == null)
            {
                Logger.Warning(this, "No channel connected to get subscribers");
                yield break;
            }

            int offset=0;

            do {
                SubscriberResponse response;
                try {
                    response = channels.GetChannelSubscribers(channeldata.ID, 100, offset);
                }
                catch(Exception e) {
                    Logger.Warning(this, $"Unable to get subscribers for channel '{channeldata.ID}'", e.Message);
                    throw;
                }

                if(response != null) {
                    foreach(SubscriberInformation user in response.Subscriptions.Select(s => new SubscriberInformation {
                        Service = TwitchConstants.ServiceKey,
                        Username = s.User.Name,
                        Avatar = s.User.Logo,
                        Status = EvaluateSubscriberPlan(s.SubPlan),
                        PlanName = s.SubPlanName
                    }))
                        yield return user;
                }

                if (response?.Total < 100)
                    break;
                offset += 100;
            }
            while(true);
        }

        UserStatus EvaluateSubscriberPlan(string plan) {
            switch(plan) {
                default:
                case "Prime":
                case "1000":
                    return UserStatus.Subscriber;
                case "2000":
                    return UserStatus.BigSubscriber;
                case "3000":
                    return UserStatus.PremiumSubscriber;
            }
        }

        IEnumerable<User> UpdateUserData(params string[] userids) {

            GetUserResponse userresponse=twitchapi.GetUsersByID(userids);

            foreach(NightlyCode.Twitch.Api.User user in userresponse.Data)
                yield return usermodule.AddOrUpdateUser(TwitchConstants.ServiceKey, user.Login, user.ID);
        }

        IEnumerable<UserInformation> GetFollowers()
        {
            if (channeldata == null)
            {
                Logger.Warning(this, "No channel connected to get subscribers");
                yield break;
            }

            int offset = 0;

            do
            {
                FollowResponse response = null;
                try
                {
                    response = channels.GetChannelFollowers(channeldata.ID, 100, offset);
                }
                catch (Exception e)
                {
                    Logger.Warning(this, $"Unable to get followers for channel '{channeldata.ID}'", e.Message);
                    throw;
                }

                if (response != null)
                {
                    foreach (UserInformation user in response.Follows.Select(s => new UserInformation
                    {
                        Service = TwitchConstants.ServiceKey,
                        Username = s.User.Name,
                        Avatar = s.User.Logo,
                    }))
                        yield return user;
                }

                if (response?.Total < 100)
                    break;
                offset += 100;
            }
            while (true);
        }

        /// <summary>
        /// get followers of the connected channels
        /// </summary>
        /// <returns>enumeration of followers</returns>
        IEnumerable<UserInformation> GetFollowers_Api() {
            if(channeldata == null) {
                Logger.Warning(this, "No channel connected to get followers");
                yield break;
            }

            string pagination = null;

            do {
                GetFollowerResponse response = null;
                try {
                    response = twitchapi.GetFollowers(userdata.ID, 100, pagination);
                }
                catch(Exception e) {
                    Logger.Warning(this, $"Unable to get followers for '{userdata.Login}'", e.Message);
                }

                if(response != null) {
                    User[] users = usermodule.GetUsersByKey(TwitchConstants.ServiceKey, response.Data.Select(u => u.FromID).ToArray());

                    string[] unknownids = response.Data.Where(u => users.All(us => us.Key != u.FromID)).Select(u => u.FromID).ToArray();

                    try {
                        users = users.Concat(UpdateUserData(unknownids)).ToArray();
                    }
                    catch(Exception e) {
                        Logger.Error(this, "Unable to update unknown userdata", e);
                    }
                    

                    foreach(UserInformation user in users.Select(f => new UserInformation {
                        Service = TwitchConstants.ServiceKey,
                        Username = f.Name,
                        Avatar = f.Avatar
                    }))
                        yield return user;
                }
                if (response?.Data?.Length < 100)
                    break;

                pagination = response?.Pagination?.Cursor;
            }
            while(true);
        } 

        /// <summary>
        /// user which is connected to service
        /// </summary>
        public string ConnectedUser => channeldata?.Name;

        public Stream ServiceIcon => ResourceAccessor.GetResource<Stream>("StreamRC.Twitch.Resources.Glitch_White_RGB.png");

        void CheckFollowers() {
            if(cooldown > 0) {
                --cooldown;
                return;
            }

            cooldown = 5;

            try {
                foreach(UserInformation follower in GetFollowers()) {
                    usermodule.SetInitialized(TwitchConstants.ServiceKey, follower.Username);

                    if(!string.IsNullOrEmpty(follower.Avatar)) {
                        User user = usermodule.GetUser(TwitchConstants.ServiceKey, follower.Username);
                        if (user.Avatar != follower.Avatar)
                            usermodule.UpdateUserAvatar(user, follower.Avatar);
                    }

                    if (usermodule.GetUserStatus(TwitchConstants.ServiceKey, follower.Username) >= UserStatus.Follower)
                        continue;

                    if(usermodule.SetUserStatus(TwitchConstants.ServiceKey, follower.Username, UserStatus.Follower))
                        NewFollower?.Invoke(follower);
                }
            }
            catch (WebException e)
            {
                Logger.Warning(this, "Unable to get followers", e.Message);
                return;
            }
            catch (Exception e)
            {
                Logger.Error(this, "Unable to get followers", e);
                return;
            }

            try
            {
                foreach(SubscriberInformation subscriber in GetSubscribers()) {
                    usermodule.SetInitialized(TwitchConstants.ServiceKey, subscriber.Username);

                    if(!string.IsNullOrEmpty(subscriber.Avatar)) {
                        User user = usermodule.GetUser(TwitchConstants.ServiceKey, subscriber.Username);
                        if (user.Avatar != subscriber.Avatar)
                            usermodule.UpdateUserAvatar(user, subscriber.Avatar);
                    }

                    if (usermodule.GetUserStatus(TwitchConstants.ServiceKey, subscriber.Username) >= subscriber.Status)
                        continue;

                    if(usermodule.SetUserStatus(TwitchConstants.ServiceKey, subscriber.Username, subscriber.Status))
                        NewSubscriber?.Invoke(subscriber);
                }
            }
            catch(WebException e) {
                Logger.Warning(this, "Unable to get subscribers", e.Message);
                return;
            }
            catch (Exception e) {
                Logger.Error(this, "Unable to get subscribers", e);
                return;
            }

            usermodule.EndInitialization(TwitchConstants.ServiceKey);
        }
        /// <summary>
        /// set the credentials to use for chat module
        /// </summary>
        /// <param name="chatuser">name of user</param>
        /// <param name="chataccesstoken">token used to access chat</param>
        public void SetCredentials(string chatuser, string chataccesstoken)
        {
            if (!string.IsNullOrEmpty(botname) && !string.IsNullOrEmpty(accesstoken))
                Disconnect(false);

            botname = chatuser;
            accesstoken = chataccesstoken;

            settings.Set(this, "botname", chatuser);
            settings.Set(this, "token", chataccesstoken);
            if (!string.IsNullOrEmpty(chatuser) && !string.IsNullOrEmpty(chataccesstoken))
                Connect();
        }

        void ITimerService.Process(double time) {
            if(!isconnected) return;

            followercheck -= time;
            if(followercheck <= 0.0) {

                try {
                    CheckFollowers();
                }
                catch(Exception e) {
                    Logger.Error(this, "Unable to check for followers", e);
                }

                followercheck = 300.0;
            }

            viewercheck -= time;
            if(viewercheck <= 0.0) {
                if(userdata != null) {
                    GetStreamsResponse response = twitchapi.GetStreams(userdata.ID);
                    TwitchStream stream = response.Data.FirstOrDefault();
                    if(stream == null) {
                        Live = false;
                        viewercheck = 180.0;
                        return;
                    }

                    Live = true;
                    Viewers = stream.ViewerCount;
                    viewercheck = 180.0;
                }
            }
        }

        public event Action<int> ViewersChanged;

        public int Viewers
        {
            get => viewers;
            set
            {
                if (viewers == value)
                    return;

                if (value > viewers)
                {
                    viewers = value;
                    if (increaseflag)
                    {
                        ViewersChanged?.Invoke(value);
                        increaseflag = false;
                    }
                    else increaseflag = true;
                }
                else if (value < viewers)
                {
                    viewers = value;
                    increaseflag = false;
                    ViewersChanged?.Invoke(value);
                }
            }
        }
    }
}