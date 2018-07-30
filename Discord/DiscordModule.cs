using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Discord.Data;
using NightlyCode.Discord.Rest;
using NightlyCode.Discord.Websockets;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Discord {

    /// <summary>
    /// interface to discord
    /// </summary>
    [Dependency(nameof(TimerModule))]
    [Dependency(nameof(StreamModule))]
    [ModuleKey("discord")]
    public class DiscordModule : IRunnableModule, IStreamServiceModule {

        readonly Context context;

        readonly DiscordRest discord = new DiscordRest(DiscordConstants.BotToken, true);
        readonly DiscordWebsocket websocket = new DiscordWebsocket(DiscordConstants.BotToken);

        readonly Dictionary<string, DiscordChatChannel> channels = new Dictionary<string, DiscordChatChannel>();

        string rpgchannel;

        /// <summary>
        /// creates a new <see cref="DiscordModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public DiscordModule(Context context) {
            this.context = context;
            websocket.Connected += OnConnected;
            websocket.MessageCreated += OnMessage;
        }

        void OnMessage(Message message) {
            // don't process bot messages
            if(message.Author.Bot)
                return;

            DiscordChatChannel channel;
            if(!channels.TryGetValue(message.ChannelID, out channel)) {
                channel = new DiscordChatChannel(discord, message.ChannelID, message.ChannelID == rpgchannel ? ChannelFlags.Game : ChannelFlags.Chat);
                channels[message.ChannelID] = channel;
                context.GetModule<StreamModule>().AddChannel(channel);
            }

            channel.ProcessMessage(message);
        }

        void OnConnected() {
            Connected?.Invoke();
        }

        public void SetChatChannel(string key) {
            context.Settings.Set(this, "chatchannel", key);
            Logger.Info(this, $"Grabbed chat channel was changed to '{key}'");
        }

        public void Start() {
            rpgchannel = context.Settings.Get(this, "rpgchannel", SlowWalkConstants.RPGChannel);
            context.GetModule<StreamModule>().AddService(DiscordConstants.Service, this);
            //context.GetModule<TimerModule>().AddService(this, 5.0);
            new Task(websocket.Connect).Start();
            Connected?.Invoke();
        }

        public void Stop() {
            foreach(DiscordChatChannel channel in channels.Values)
                context.GetModule<StreamModule>().RemoveChannel(channel);
            channels.Clear();
            //context.GetModule<TimerModule>().RemoveService(this);
        }

        /// <summary>
        /// triggered when service is connected
        /// </summary>
        public event Action Connected;

        /// <summary>
        /// triggered when a user follows channel
        /// </summary>
        public event Action<UserInformation> NewFollower;

        /// <summary>
        /// triggered when a user has subscribed to channel
        /// </summary>
        public event Action<SubscriberInformation> NewSubscriber;

        public IEnumerable<SubscriberInformation> GetSubscribers() {
            yield break;
        }

        public IEnumerable<UserInformation> GetFollowers() {
            yield break;
        }

        public Stream ServiceIcon => ResourceAccessor.GetResource<Stream>("StreamRC.Discord.Resources.Discord-Logo-White.png");
    }
}