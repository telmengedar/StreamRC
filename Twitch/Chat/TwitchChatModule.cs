using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using NightlyCode.Twitch.Chat;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Twitch.Chat {

    /// <summary>
    /// module connecting userchat
    /// </summary>
    [Dependency(nameof(UserModule))]
    [Dependency(nameof(ImageCacheModule))]
    [Dependency(nameof(StreamModule))]
    [ModuleKey("twitchchat")]
    public class TwitchChatModule : IRunnableModule {
        readonly Context context;
        readonly ChatClient chatclient = new ChatClient();

        string username;
        string accesstoken;

        bool isconnected;
        bool isreconnecting;

        readonly List<TwitchUserChat> channels=new List<TwitchUserChat>();
         
        /// <summary>
        /// creates a new <see cref="TwitchChatModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public TwitchChatModule(Context context) {
            this.context = context;
            chatclient.Disconnected += () => Disconnect(true);
            chatclient.Reconnect += () => {
                Logger.Info(this, "Reconnect message received");
                Disconnect(true);
            };

            chatclient.ChannelJoined += OnChannelJoined;
            chatclient.ChannelLeft += OnChannelLeft;
        }

        public string Username => username;

        public string AccessToken => accesstoken;

        void Disconnect(bool reconnect) {
            context.GetModule<StreamModule>().RemoveChannels(c => c is TwitchUserChat);
            isconnected = false;

            if(reconnect && !isreconnecting)
                new Task(Reconnect).Start();
        }

        void Reconnect() {
            isreconnecting = true;
            Logger.Info(this, "Reconnecting in 5 seconds.");
            Thread.Sleep(5000);
            try {
                Connect();
                isreconnecting = false;
            }
            catch(Exception e) {
                Logger.Error(this, "Error reconnecting", e);
                new Task(Reconnect).Start();
            }
        }

        void OnChannelJoined(ChatChannel channelsource)
        {
            if(channelsource.Name.ToLower() == username.ToLower()) {
                Logger.Info(this, $"Connected to primary user channel '{channelsource.Name}'");
                TwitchAccountChat chat = new TwitchAccountChat(channelsource, context.GetModule<UserModule>(), context.GetModule<ImageCacheModule>());
                chat.Subscription += OnSubscription;
                channels.Add(chat);
                context.GetModule<StreamModule>().AddChannel(chat);
            }
            else {
                Logger.Info(this, $"Connected to '{channelsource.Name}'");
                TwitchUserChat chat = new TwitchUserChat(channelsource, context.GetModule<UserModule>(), context.GetModule<ImageCacheModule>());
                chat.Subscription += OnSubscription;
                channels.Add(chat);
                context.GetModule<StreamModule>().AddChannel(chat);
            }
        }

        UserStatus EvaluateSubscriberPlan(string plan)
        {
            switch (plan)
            {
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

        void OnSubscription(Subscription subscription) {
            context.GetModule<StreamModule>().AddSubscriber(new SubscriberInformation {
                Service = TwitchConstants.ServiceKey,
                Username = subscription.User,
                PlanName = subscription.PlanName,
                Color = subscription.Color,
                Status = EvaluateSubscriberPlan(subscription.Plan)
            });
        }

        void OnChannelLeft(ChatChannel channelsource)
        {
            Logger.Info(this, $"'{channelsource.Name}' disconnected");
            TwitchUserChat chat = channels.FirstOrDefault(c => c.Name == channelsource.Name);
            if(chat != null) {
                chat.Subscription -= OnSubscription;
                channels.Remove(chat);
            }
            context.GetModule<StreamModule>().RemoveChannel(TwitchConstants.ServiceKey, channelsource.Name);
        }

        void IRunnableModule.Start() {
            username = context.Settings.Get<string>(this, "username", null);
            accesstoken = context.Settings.Get<string>(this, "token", null);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(accesstoken))
                Connect();
        }

        void IRunnableModule.Stop() {
            Disconnect(false);
        }

        /// <summary>
        /// set the credentials to use for chat module
        /// </summary>
        /// <param name="chatuser">name of user</param>
        /// <param name="chataccesstoken">token used to access chat</param>
        public void SetCredentials(string chatuser, string chataccesstoken) {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(accesstoken))
                Disconnect(false);

            username = chatuser;
            accesstoken = chataccesstoken;

            context.Settings.Set(this, "username", chatuser);
            context.Settings.Set(this, "token", chataccesstoken);
            if(!string.IsNullOrEmpty(chatuser) && !string.IsNullOrEmpty(chataccesstoken))
                Connect();
        }

        void Connect() {
            if(isconnected) {
                Logger.Warning(this, "Already connected");
                return;
            }

            if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(accesstoken)) {
                Logger.Warning(this, "No credentials set");
                return;
            }

            Logger.Info(this, $"Connecting to @{username} to #{username}");
            chatclient.Connect(username, accesstoken);
            chatclient.Join(username);
            isconnected = true;
        }
    }
}