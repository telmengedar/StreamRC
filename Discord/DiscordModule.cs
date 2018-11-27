using System;
using System.Collections.Generic;
using System.IO;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Logs;
using NightlyCode.Discord.Data;
using NightlyCode.Discord.Rest;
using NightlyCode.Discord.Websockets;
using NightlyCode.Modules;
using StreamRC.Core.Settings;
using StreamRC.Core.UI;
using StreamRC.Discord.Configuration;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Discord
{

    /// <summary>
    /// module providing discord interaction to <see cref="StreamModule"/>
    /// </summary>
    [Module(Key="discord", AutoCreate = true)]
    public class DiscordModule : IStreamServiceModule {
        readonly IStreamModule streammodule;
        readonly ISettings settings;
        string bottoken;
        string chatchannel;

        DiscordRest discord;
        DiscordWebsocket websocket;

        readonly Dictionary<string, DiscordChatChannel> channels = new Dictionary<string, DiscordChatChannel>();

        string rpgchannel;
        string commandchannel = "493839621252317184";

        /// <summary>
        /// creates a new <see cref="DiscordModule"/>
        /// </summary>
        /// <param name="streammodule">access to modules</param>
        public DiscordModule(IStreamModule streammodule, ISettings settings, IMainWindow mainwindow) {
            this.streammodule = streammodule;
            this.settings = settings;

            rpgchannel = settings.Get<string>(this, "rpgchannel");
            chatchannel = settings.Get<string>(this, "chatchannel");
            SetToken(settings.Get<string>(this, "bottoken"));

            streammodule.AddService(DiscordConstants.Service, this);
            Connected?.Invoke();

            mainwindow.AddMenuItem("Profiles.Discord.Connect", (sender, args) => {
                DiscordSettings discordsettings = new DiscordSettings(bottoken, chatchannel);
                discordsettings.Closed += OnSettingsClosed;
                discordsettings.ShowDialog();
            });

        }

        void OnMessage(Message message) {
            // don't process bot messages or messages coming from a channel not flagged for display
            if(message.Author.Bot || (message.ChannelID != chatchannel && message.ChannelID != rpgchannel && message.ChannelID!=commandchannel))
                return;

            if(!channels.TryGetValue(message.ChannelID, out DiscordChatChannel channel)) {
                ChannelFlags flags = ChannelFlags.None;
                if(message.ChannelID == chatchannel)
                    flags |= ChannelFlags.Chat | ChannelFlags.UserChat;
                if(message.ChannelID == rpgchannel)
                    flags |= ChannelFlags.Game;
                if(message.ChannelID == commandchannel)
                    flags |= ChannelFlags.Command;
                channel = new DiscordChatChannel(discord, message.ChannelID, flags);
                channels[message.ChannelID] = channel;
                streammodule.AddChannel(channel);
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
            settings.Set(this, "rpgchannel", key);
            Logger.Info(this, $"Grabbed rpg channel was changed to '{key}'");
        }

        public void SetChatChannel(string key) {
            if(chatchannel == key)
                return;
            chatchannel = key;
            settings.Set(this, "chatchannel", key);
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
            settings.Set(this, "bottoken", token);

            if(!string.IsNullOrEmpty(bottoken)) {
                websocket = new DiscordWebsocket(bottoken);
                websocket.Connected += OnConnected;
                websocket.MessageCreated += OnMessage;

                discord = new DiscordRest(bottoken, true);
                websocket.Connect();
            }
        }

        void OnSettingsClosed(object sender, EventArgs e) {
            DiscordSettings settings = (DiscordSettings)sender;
            SetToken(settings.BotToken);
            chatchannel = settings.ChatChannel;
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