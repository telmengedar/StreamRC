using System;
using System.Text.RegularExpressions;
using NightlyCode.Core.Logs;
using NightlyCode.Twitch.Chat;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;
using ChatMessage = NightlyCode.Twitch.Chat.ChatMessage;
using HostInformation = StreamRC.Streaming.Stream.HostInformation;

namespace StreamRC.Twitch.Chat {

    /// <summary>
    /// serves as the primary connection to a chat channel
    /// </summary>
    public class TwitchAccountChat : TwitchUserChat, IChannelInfoChannel {

        /// <summary>
        /// creates a new <see cref="TwitchAccountChat"/>
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="usermodule"></param>
        /// <param name="imagecache"></param>
        public TwitchAccountChat(ChatChannel channel, UserModule usermodule, ImageCacheModule imagecache)
            : base(channel, usermodule, imagecache) {
            channel.Raid += notice => Raid?.Invoke(this, new RaidInformation() {
                Service = TwitchConstants.ServiceKey,
                Login = notice.Login,
                DisplayName = notice.DisplayName,
                Color = notice.Color,
                Avatar = notice.Avatar,
                Channel = notice.Channel,
                RaiderCount = notice.RaiderCount,
                RoomID = notice.RoomID,
                SystemMessage = notice.SystemMessage
            });
        }

        /// <summary>
        /// this channel is hosted by someone
        /// </summary>
        public event Action<IChannelInfoChannel, HostInformation> Hosted;

        /// <summary>
        /// this channel is raided by someone
        /// </summary>
        public event Action<IChannelInfoChannel, RaidInformation> Raid;

        /// <summary>
        /// a micropresent was received
        /// </summary>
        public event Action<IChannelInfoChannel, MicroPresent> MicroPresent;

        void ParseJTVMessage(ChatMessage message)
        {
            // jtv: xxx is now hosting you for up to 4 viewers.
            // jtv: xxx is now hosting you.
            Match match = Regex.Match(message.Message, "^(?<user>[^ ]+) is now (auto )?hosting you( for up to (?<viewers>[0-9]+) viewers)?\\.$");
            if (match.Success)
            {
                Hosted?.Invoke(this, new HostInformation
                {
                    Service = Service,
                    Channel = match.Groups["user"].Value.ToLower(),
                    Viewers = match.Groups["viewers"].Success ? int.Parse(match.Groups["viewers"].Value) : -1
                });
                return;
            }

            Logger.Warning(this, "Unprocessed message from jtv", message.ToString());
        }

        protected override bool FilterMessage(ChatMessage message)
        {
            if (message.User == "jtv")
            {
                ParseJTVMessage(message);
                return true;
            }

            if (message.Bits > 0)
            {
                MicroPresent?.Invoke(this, new MicroPresent
                {
                    Currency = "Bits",
                    Amount = message.Bits,
                    Service = Service,
                    Username = message.User,
                    Message = message.Message
                });
            }
            return base.FilterMessage(message);
        }

    }
}