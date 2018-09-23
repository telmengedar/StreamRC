using System;
using System.Collections.Generic;
using System.IO;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Discord.Data;
using NightlyCode.Discord.Rest;
using NightlyCode.Discord.Websockets;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Discord.Configuration;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Discord
{

    /// <summary>
    /// module providing discord interaction to <see cref="StreamModule"/>
    /// </summary>
    [Dependency(nameof(TimerModule))]
    [Dependency(nameof(StreamModule))]
    [ModuleKey("discord")]
    public class DiscordModule : IRunnableModule, IStreamServiceModule {
        readonly Context context;
        string bottoken;
        string chatchannel;

        DiscordRest discord = null;
        DiscordWebsocket websocket = null;

        readonly Dictionary<string, DiscordChatChannel> channels = new Dictionary<string, DiscordChatChannel>();

        string rpgchannel;

        /// <summary>
        /// creates a new <see cref="DiscordModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public DiscordModule(Context context) {
            this.context = context;
        }

        void OnMessage(Message message) {
            // don't process bot messages or messages coming from a channel not flagged for display
            if(message.Author.Bot || (message.ChannelID != chatchannel && message.ChannelID != rpgchannel))
                return;

            DiscordChatChannel channel;
            if(!channels.TryGetValue(message.ChannelID, out channel)) {
                ChannelFlags flags = ChannelFlags.Chat | (message.ChannelID == rpgchannel ? ChannelFlags.Game : ChannelFlags.None);
                channel = new DiscordChatChannel(discord, message.ChannelID, flags);
                channels[message.ChannelID] = channel;
                context.GetModule<StreamModule>().AddChannel(channel);
            }

            channel.ProcessMessage(message);
        }

        void OnConnected() {
            Connected?.Invoke();
        }

        public void SetRPGChannel(string key)
        {
            if (rpgchannel == key)
                return;

            rpgchannel = key;
            context.Settings.Set(this, "rpgchannel", key);
            Logger.Info(this, $"Grabbed rpg channel was changed to '{key}'");
        }

        public void SetChatChannel(string key) {
            if(chatchannel == key)
                return;
            chatchannel = key;
            context.Settings.Set(this, "chatchannel", key);
            Logger.Info(this, $"Grabbed chat channel was changed to '{key}'");
        }

        void SetToken(string token) {
            if(token == bottoken)
                return;

            if(bottoken != null) {
                websocket.Connected -= OnConnected;
                websocket.MessageCreated -= OnMessage;
                websocket.Disconnect();
                discord = null;
            }

            bottoken = token;
            context.Settings.Set(this, "bottoken", token);

            if(!string.IsNullOrEmpty(bottoken)) {
                websocket = new DiscordWebsocket(bottoken);
                websocket.Connected += OnConnected;
                websocket.MessageCreated += OnMessage;

                discord = new DiscordRest(bottoken, true);
                websocket.Connect();
            }
        }

        void IRunnableModule.Start() {
            rpgchannel = context.Settings.Get<string>(this, "rpgchannel");
            chatchannel = context.Settings.Get<string>(this, "chatchannel");
            SetToken(context.Settings.Get<string>(this, "bottoken"));

            context.GetModule<StreamModule>().AddService(DiscordConstants.Service, this);
            Connected?.Invoke();

            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Profiles.Discord.Connect", (sender, args) => {
                DiscordSettings settings = new DiscordSettings(bottoken, chatchannel);
                settings.Closed += OnSettingsClosed;
                settings.ShowDialog();
            });
        }

        void OnSettingsClosed(object sender, EventArgs e) {
            DiscordSettings settings = (DiscordSettings)sender;
            SetToken(settings.BotToken);
            chatchannel = settings.ChatChannel;
        }

        void IRunnableModule.Stop() {
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