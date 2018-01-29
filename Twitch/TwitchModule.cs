using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Randoms;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using NightlyCode.Twitch.Api;
using NightlyCode.Twitch.Chat;
using NightlyCode.Twitch.V5;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;
using ChatMessage = StreamRC.Streaming.Stream.ChatMessage;
using HostInformation = StreamRC.Streaming.Stream.HostInformation;
using Logger = NightlyCode.Core.Logs.Logger;
using Subscription = NightlyCode.Twitch.Chat.Subscription;
using User = StreamRC.Streaming.Users.User;

namespace StreamRC.Twitch {

    /// <summary>
    /// interface to twitch
    /// </summary>
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(UserModule), DependencyType.Type)]
    [Dependency(nameof(TimerModule), DependencyType.Type)]
    [Dependency(ModuleKeys.MainWindow, DependencyType.Key)]
    public class TwitchModule : IInitializableModule, IStreamServiceModule, IRunnableModule, ITimerService {
        const string servicekey = "Twitch";

        readonly Context context;
        NightlyCode.Twitch.Api.User userdata;
        Channel channeldata;
        ChatChannel channel;

        TwitchApi twitchapi;
        Channels channels;
        ChatClient chatclient;

        bool followersinitialized;
        bool subscribersinitialized;
        TwitchProfile profile;

        int cooldown;

        readonly Dictionary<string, Color> fallbackcolor=new Dictionary<string, Color>();

        double followercheck;

        /// <summary>
        /// creates a new <see cref="TwitchModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public TwitchModule(Context context) {
            this.context = context;
        }

        public event Action Connected;
        public event Action<UserInformation> UserJoined;
        public event Action<UserInformation> UserLeft;
        public event Action<StreamCommand> CommandReceived;
        public event Action<UserInformation> NewFollower;
        public event Action<SubscriberInformation> NewSubscriber;
        public event Action<HostInformation> Hosted;
        public event Action<ChatMessage> ChatMessage;
        public event Action<MicroPresent> MicroPresent;

        public bool IsConnected { get; private set; }

        /// <summary>
        /// connects to a profile
        /// </summary>
        /// <param name="profilename">name of profile to connect to</param>
        public void ConnectProfile(string profilename) {
            if(profile?.Account == profilename) {
                Logger.Warning(this, $"Already connected to {profilename}");
                return;
            }

            profile = context.Database.LoadEntities<TwitchProfile>().Where(p => p.Account == profilename).Execute().FirstOrDefault();
            if(profile == null) {
                Logger.Warning(this, $"Profile {profilename} not found");
                return;
            }

            context.Settings.Set(this, "lastprofile", profilename);
        }

        public void Disconnect() {
            if (IsConnected)
            {
                chatclient.Disconnect();
                IsConnected = false;
            }
        }

        public void Connect() {
            if(IsConnected)
                Disconnect();

            twitchapi = new TwitchApi(TwitchConstants.ClientID, profile.AccessToken);
            channels = new Channels(TwitchConstants.ClientID, profile.AccessToken);
            chatclient = new ChatClient();

            //Logger.Info(this, "Available scopes", string.Join(", ", TwitchAPI.Settings.Scopes.Select(s => s.ToString())));

            userdata = twitchapi.GetUsersByLogin(profile.Account).Data.FirstOrDefault();
            if(userdata == null)
                Logger.Warning(this, $"No userdata found for '{profile.Account}'");

            channeldata = channels.GetChannel();

            chatclient.Connect(profile.Account, profile.AccessToken);
            chatclient.Disconnected += OnDisconnect;

            chatclient.ChannelJoined += OnChannelJoined;
            chatclient.ChannelLeft += OnChannelLeft;
            chatclient.Join(profile.Account);
            /*client.OnWhisperCommandReceived += (sender, args) => {
                try {
                    CommandReceived?.Invoke(new StreamCommand
                    {
                        Service = servicekey,
                        IsWhispered = true,
                        User = args.WhisperMessage.Username,
                        Command = args.Command,
                        Arguments = args.ArgumentsAsList.ToArray()
                    });
                }
                catch (Exception e) {
                    Logger.Error(this, "Error triggering CommandReceived event", e);
                }
            };
            client.OnWhisperReceived += (sender, args) => {
                try {
                    ChatMessage?.Invoke(new ChatMessage {
                        Service = servicekey,
                        User = args.WhisperMessage.Username,
                        Message = args.WhisperMessage.Message,
                        UserColor = GetColor(args.WhisperMessage.Username, args.WhisperMessage.ColorHex),
                        IsWhisper = true,
                        Emotes = args.WhisperMessage.EmoteSet.Emotes.Select(e => new ChatEmote
                        {
                            StartIndex = e.StartIndex,
                            EndIndex = e.EndIndex,
                            ImageID = context.GetModule<ImageCacheModule>().AddImage(e.ImageUrl, e.Name)
                        }).ToArray()
                    });
                }
                catch(Exception e) {
                    Logger.Error(this, "Error triggering ChatMessage event", e);
                }
            };*/
            IsConnected = true;
            Connected?.Invoke();
        }

        void OnChannelJoined(ChatChannel channelsource) {
            channel = channelsource;
            Logger.Info(this, $"Connected to '{channelsource.Name}'");
            channelsource.Host += OnHost;
            channelsource.MessageReceived += OnMessage;
            channelsource.Notice += OnNotice;
            channelsource.Subscription += OnSubscription;
            channelsource.UserJoined += OnUserJoined;
            channelsource.UserLeft += OnUserLeft;

            foreach(string user in channelsource.Users)
                OnUserJoined(channelsource, user);
        }

        void OnChannelLeft(ChatChannel channelsource) {
            channel = null;
            Logger.Info(this, $"'{channelsource.Name}' disconnected");
            channelsource.Host -= OnHost;
            channelsource.MessageReceived -= OnMessage;
            channelsource.Notice -= OnNotice;
            channelsource.Subscription -= OnSubscription;
            channelsource.UserJoined -= OnUserJoined;
            channelsource.UserLeft -= OnUserLeft;
        }

        void OnUserLeft(ChatChannel channelsource, string user)
        {
            try
            {
                UserLeft?.Invoke(new UserInformation
                {
                    Service = servicekey,
                    Username = user
                });
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering UserLeft event", e);
            }
        }

        void OnUserJoined(ChatChannel channelsource, string user)
        {
            try
            {
                UserJoined?.Invoke(new UserInformation
                {
                    Service = servicekey,
                    Username = user
                });
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering UserJoined event", e);
            }
        }

        void OnSubscription(Subscription subscription)
        {
            try {
                NewSubscriber?.Invoke(new SubscriberInformation {
                    Service = servicekey,
                    Username = subscription.User,
                    Status = EvaluateSubscriberPlan(subscription.Plan),
                    PlanName = subscription.PlanName
                });
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering NewSubscriber event", e);
            }
        }

        void OnNotice(Notice notice) {
            Logger.Info(this, $"Notice for channel '{notice.Channel}'", notice.Message);
        }

        void OnMessage(NightlyCode.Twitch.Chat.ChatMessage message) {
            try {
                if(message.Message.StartsWith("!")) {
                    try {
                        int indexof = message.Message.IndexOf(' ');

                        if(indexof == -1) {
                            CommandReceived?.Invoke(new StreamCommand {
                                Service = servicekey,
                                IsWhispered = false,
                                User = message.User,
                                Command = message.Message.Substring(1),
                                Arguments = new string[0]
                            });
                        }
                        else {
                            CommandReceived?.Invoke(new StreamCommand {
                                Service = servicekey,
                                IsWhispered = false,
                                User = message.User,
                                Command = message.Message.Substring(1, indexof - 1),
                                Arguments = Commands.SplitArguments(message.Message.Substring(indexof + 1)).ToArray()
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(this, "Error triggering CommandReceived event", e);
                    }
                }
                else if(message.User == "jtv") {
                    ParseJTVMessage(message);
                }
                else {
                    if(message.Bits > 0) {
                        MicroPresent?.Invoke(new MicroPresent {
                            Currency = "Bits",
                            Amount = message.Bits,
                            Service = servicekey,
                            Username = message.User,
                            Message = message.Message
                        });
                    }

                    ChatMessage?.Invoke(new ChatMessage
                    {
                        Service = servicekey,
                        User = message.User,
                        Message = message.Message,
                        AvatarLink = context.GetModule<UserModule>().GetUser(servicekey, message.User).Avatar,
                        UserColor = GetColor(message.User, message.Color),
                        IsWhisper = false,
                        Emotes = message.Emotes?.Select(e => new ChatEmote
                        {
                            StartIndex = e.FirstIndex,
                            EndIndex = e.LastIndex,
                            ImageID = context.GetModule<ImageCacheModule>().AddImage(e.GetUrl(3), $"{servicekey}.emote.{e.ID}"),
                        }).ToArray() ?? new ChatEmote[0]
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering ChatMessage event", e);
            }
        }

        void ParseJTVMessage(NightlyCode.Twitch.Chat.ChatMessage message) {
            // jtv: xxx is now hosting you for up to 4 viewers.
            // jtv: xxx is now hosting you.
            Match match = Regex.Match(message.Message, "^(?<user>[^ ]+) is now hosting you( for up to (?<viewers>[0-9]+) viewers)?\\.$");
            if(match.Success) {
                Hosted?.Invoke(new HostInformation {
                    Service = servicekey,
                    Channel = match.Groups["user"].Value.ToLower(),
                    Viewers = match.Groups["viewers"].Success ? int.Parse(match.Groups["viewers"].Value) : -1
                });
                return;
            }

            Logger.Warning(this, "Unprocessed message from jtv", message.ToString());
        }

        void OnHost(NightlyCode.Twitch.Chat.HostInformation host) {
            try {
                Hosted?.Invoke(new HostInformation
                {
                    Service = servicekey,
                    Channel = host.Host,
                    Viewers = host.Viewers
                });
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering Hosted event", e);
            }
        }

        Color GetFallbackColor(string user) {
            Color color;
            if(!fallbackcolor.TryGetValue(user, out color))
                fallbackcolor[user] = color = Color.FromRgb((byte)(128 + RNG.XORShift64.NextInt(128)), (byte)(128 + RNG.XORShift64.NextInt(128)), (byte)(128 + RNG.XORShift64.NextInt(128)));
            return color;
        }

        Color GetColor(string user, string color) {
            if(string.IsNullOrEmpty(color))
                return GetFallbackColor(user);

            try
            {
                return (Color)ColorConverter.ConvertFromString(color);
            }
            catch (Exception)
            {
                return Colors.White;
            }
        }

        /// <summary>
        /// get subscribers of a channel
        /// </summary>
        /// <returns>list of subscribers</returns>
        public IEnumerable<SubscriberInformation> GetSubscribers() {
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
                        Service = servicekey,
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
                yield return context.GetModule<UserModule>().AddOrUpdateUser(servicekey, user.Login, user.ID);
        }

        /// <summary>
        /// get subscribers of a channel
        /// </summary>
        /// <returns>list of subscribers</returns>
        public IEnumerable<UserInformation> GetFollowers()
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
                        Service = servicekey,
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
                    User[] users = context.GetModule<UserModule>().GetUsersByKey(servicekey, response.Data.Select(u => u.FromID).ToArray());

                    string[] unknownids = response.Data.Where(u => users.All(us => us.Key != u.FromID)).Select(u => u.FromID).ToArray();

                    try {
                        users = users.Concat(UpdateUserData(unknownids)).ToArray();
                    }
                    catch(Exception e) {
                        Logger.Error(this, "Unable to update unknown userdata", e);
                    }
                    

                    foreach(UserInformation user in users.Select(f => new UserInformation {
                        Service = servicekey,
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

        void OnDisconnect() {
            IsConnected = false;
        }

        /// <summary>
        /// sends a message to the connected twitch channel
        /// </summary>
        /// <param name="message">message to send</param>
        public void SendMessage(string message) {
            if(!IsConnected) {
                Logger.Warning(this, "Unable to send message, client not connected");
                return;
            }

            Logger.Info(this, $"{channeldata.Name}: {message}");
            if(channel == null) {
                Logger.Warning(this, $"Not connected to channel '{profile.Account}'");
                return;
            }
            channel.SendMessage(message);
        }

        public void SendPrivateMessage(string user, string message) {
            if (!IsConnected)
            {
                Logger.Warning(this, "Unable to send private message, client not connected");
                return;
            }

            Logger.Warning(this, "Whispering currently not implemented");
            //Logger.Info(this, $"{channeldata.Name}->{user}: {message}");
            //client?.SendWhisper(user, message);
        }

        /// <summary>
        /// user which is connected to service
        /// </summary>
        public string ConnectedUser => channeldata?.Name;

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<TwitchProfile>();

#if !TEST
            context.GetModule<StreamModule>().AddService(servicekey, this);
#endif

            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Profiles.Twitch.Connect", (sender, args) => new TwitchConnector(this).ShowDialog());
            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddSeparator("Profiles.Twitch");
            foreach(string profilename in context.Database.Load<TwitchProfile>(p=>p.Account).ExecuteSet<string>())
                context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem($"Profiles.Twitch.{profilename}", (sender, args) => ConnectProfile(profilename));
        }

        void CheckFollowers() {
            if(cooldown > 0) {
                --cooldown;
                return;
            }

            UserModule usermodule = context.GetModule<UserModule>();

            if(!followersinitialized && !subscribersinitialized) {
                Logger.Info(this, "Initializing user status");
                usermodule.ResetUserStatus(servicekey);
            }

            try {
                foreach(UserInformation follower in GetFollowers()) {
                    if(usermodule.GetUserStatus(servicekey, follower.Username) >= UserStatus.Follower)
                        continue;

                    User user = usermodule.GetUser(servicekey, follower.Username);
                    if(!string.IsNullOrEmpty(follower.Avatar) && user.Avatar != follower.Avatar)
                        usermodule.UpdateUserAvatar(user, follower.Avatar);

                    usermodule.SetUserStatus(servicekey, follower.Username, UserStatus.Follower);
                    if(followersinitialized)
                        NewFollower?.Invoke(follower);
                }
                followersinitialized = true;
            }
            catch (WebException e)
            {
                Logger.Warning(this, "Unable to get followers", e.Message);
                cooldown = 5;
            }
            catch (Exception e)
            {
                Logger.Error(this, "Unable to get followers", e);
            }

            try
            {
                foreach(SubscriberInformation subscriber in GetSubscribers()) {
                    if(usermodule.GetUserStatus(servicekey, subscriber.Username) >= subscriber.Status)
                        continue;

                    User user = usermodule.GetUser(servicekey, subscriber.Username);
                    if (!string.IsNullOrEmpty(subscriber.Avatar) && user.Avatar != subscriber.Avatar)
                        usermodule.UpdateUserAvatar(user, subscriber.Avatar);

                    usermodule.SetUserStatus(servicekey, subscriber.Username, subscriber.Status);
                    if(subscribersinitialized)
                        NewSubscriber?.Invoke(subscriber);
                }
                subscribersinitialized = true;
            }
            catch(WebException e) {
                Logger.Warning(this, "Unable to get subscribers", e.Message);
                cooldown = 5;
            }
            catch (Exception e) {
                Logger.Error(this, "Unable to get subscribers", e);
            }

            
        }

        /// <summary>
        /// creates a profile to which to connect
        /// </summary>
        /// <param name="account">name of account</param>
        /// <param name="token">access token used to authenticate</param>
        public void CreateProfile(string account, string token) {
            if(context.Database.Update<TwitchProfile>().Set(p => p.AccessToken == token).Where(p => p.Account == account).Execute() == 0) {
                context.Database.Insert<TwitchProfile>().Columns(p => p.Account, p => p.AccessToken).Values(account, token).Execute();
                context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem($"Profiles.Twitch.{account}", (sender, args) => ConnectProfile(account));
            }
        }

        void IRunnableModule.Start() {
            string lastprofile = context.Settings.Get<string>(this, "lastprofile", null);
            if (!string.IsNullOrEmpty(lastprofile))
                ConnectProfile(lastprofile);

            context.GetModule<TimerModule>().AddService(this, 5.0);
        }

        void IRunnableModule.Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
            followersinitialized = false;
            Disconnect();
        }

        void ITimerService.Process(double time) {
            if(!IsConnected && profile != null) {
                Logger.Info(this, $"Reconnecting to twitch channel '{profile.Account}'");
                Connect();
            }

            if(IsConnected) {
                followercheck -= time;
                if (followercheck <= 0.0)
                {
                    try {
                        CheckFollowers();
                    }
                    catch(Exception e) {
                        Logger.Error(this, "Unable to check for followers", e);
                    }
                    
                    followercheck = 300.0;
                }
            }
        }
    }
}