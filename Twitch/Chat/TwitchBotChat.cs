using System;
using System.Linq;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Logs;
using NightlyCode.Twitch.Chat;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;
using ChatMessage = NightlyCode.Twitch.Chat.ChatMessage;

namespace StreamRC.Twitch.Chat {
    public class TwitchBotChat : TwitchUserChat, IBotChatChannel {

        /// <summary>
        /// creates a new <see cref="TwitchBotChat"/>
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="usermodule"></param>
        /// <param name="imagecache"></param>
        public TwitchBotChat(ChatChannel channel, UserModule usermodule, ImageCacheModule imagecache)
            : base(channel, usermodule, imagecache) {}

        /// <summary>
        /// a command was received
        /// </summary>
        public event Action<IBotChatChannel, StreamCommand> CommandReceived;

        protected override bool FilterMessage(ChatMessage message) {
            if (message.Message.StartsWith("!"))
            {
                try
                {
                    int indexof = message.Message.IndexOf(' ');

                    if (indexof == -1)
                    {
                        CommandReceived?.Invoke(this, new StreamCommand
                        {
                            Service = Service,
                            Channel = Name,
                            IsWhispered = false,
                            User = message.User,
                            Command = message.Message.Substring(1),
                            Arguments = new string[0]
                        });
                    }
                    else
                    {
                        CommandReceived?.Invoke(this, new StreamCommand
                        {
                            Service = Service,
                            Channel = Name,
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
                return true;
            }

            if(message.Message.StartsWith("$"))
            {
                CommandReceived?.Invoke(this, new StreamCommand
                {
                    Service = Service,
                    Channel = Name,
                    User = message.User,
                    Command = message.Message.Substring(1),
                    IsSystemCommand = true
                });

                return true;
            }
            return message.User == "jtv" || message.User.ToLower() == "ncstreamrc";
        }

        /// <summary>
        /// channel flags
        /// </summary>
        public override ChannelFlags Flags => ChannelFlags.Major | ChannelFlags.Chat | ChannelFlags.Bot | ChannelFlags.Game | ChannelFlags.Notification;
    }
}