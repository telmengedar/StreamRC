using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using NightlyCode.Core.Logs;
using NightlyCode.Discord.Data;
using NightlyCode.Discord.Data.Channels;
using NightlyCode.Discord.Rest;
using StreamRC.Streaming.Chat;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Discord {

    /// <summary>
    /// chat channel in discord
    /// </summary>
    public class DiscordChatChannel : IBotChatChannel {
        readonly DiscordRest discord;

        bool isconnected;
        string lastmessageid;

        readonly ChannelFlags flags;

        /// <summary>
        /// creates a new <see cref="DiscordChatChannel"/>
        /// </summary>
        /// <param name="discord"></param>
        /// <param name="key"></param>
        /// <param name="flags">additional flags for channel (<see cref="ChannelFlags.Bot"/> is automatically set)</param>
        public DiscordChatChannel(DiscordRest discord, string key, ChannelFlags flags=ChannelFlags.None) {
            this.discord = discord;
            this.flags = flags;
            Key = key;
        }

        public string Key { get; set; }

        /// <summary>
        /// user has joined channel
        /// </summary>
        public event Action<IChatChannel, UserInformation> UserJoined;

        /// <summary>
        /// user has left channel
        /// </summary>
        public event Action<IChatChannel, UserInformation> UserLeft;

        /// <summary>
        /// a message was received
        /// </summary>
        public event Action<IChatChannel, ChatMessage> ChatMessage;

        /// <summary>
        /// name of service where channel is connected to
        /// </summary>
        public string Service => DiscordConstants.Service;

        /// <summary>
        /// name of chat channel
        /// </summary>
        public string Name => Key;

        /// <summary>
        /// channel flags
        /// </summary>
        public ChannelFlags Flags => ChannelFlags.Bot | flags;

        /// <summary>
        /// users currently in chat
        /// </summary>
        public IEnumerable<string> Users
        {
            get
            {
                yield break;
            }
        }

        /// <summary>
        /// sends a message to the channel
        /// </summary>
        /// <param name="message">message to send</param>
        public void SendMessage(string message) {
            discord.CreateMessage(Key, new CreateMessageBody {
                Content = message
            });
        }

        /// <summary>
        /// initializes the channel after it was added
        /// </summary>
        void IChatChannel.Initialize() {
            Logger.Info(this, "Getting channel information");
            try
            {
                Channel channel = discord.GetChannel(Key);
                lastmessageid = channel.LastMessageID;
                isconnected = true;
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error getting channel information", e);
            }
            
        }

        public void ProcessMessage(Message message) {
            if (message.Content.StartsWith("!"))
            {
                string[] arguments = message.Content.Substring(1).Split(' ');
                CommandReceived?.Invoke(this, new StreamCommand
                {
                    Service = DiscordConstants.Service,
                    Channel = Key,
                    User = message.Author.Username,
                    Command = arguments[0],
                    Arguments = arguments.Skip(1).ToArray()
                });
            }
            else ChatMessage?.Invoke(this, new ChatMessage
            {
                Service = DiscordConstants.Service,
                Channel = Key,
                User = message.Author.Username,
                AvatarLink = message.Author.Avatar != null ? DataExtensions.GetAvatarURL(message.Author.ID, message.Author.Avatar) : null,
                Message = message.Content,
                Emotes = new ChatEmote[0],
                Attachements = ProcessAttachements(message.Attachments).ToArray(),
                IsWhisper = false,
                UserColor = Colors.White
            });
        }

        IEnumerable<MessageAttachement> ProcessAttachements(Attachment[] attachments) {
            foreach(Attachment attachment in attachments) {
                if(attachment.Width > 0 && attachment.Height > 0) {
                    yield return new MessageAttachement {
                        Type = AttachmentType.Image,
                        Width = attachment.Width,
                        Height = attachment.Height,
                        URL = attachment.URL
                    };
                }
            }
        }

        public void Process() {
            if(!isconnected)
                return;

            Message[] messages = discord.GetMessages(Key, lastmessageid != null ? new GetMessagesParameter
            {
                After = lastmessageid
            } : null);

            if (messages.Length > 0)
            {
                foreach (Message message in messages)
                    ProcessMessage(message);
                lastmessageid = messages.Last().ID;
            }
        }

        /// <summary>
        /// a command was received
        /// </summary>
        public event Action<IBotChatChannel, StreamCommand> CommandReceived;

        /// <summary>
        /// this channel is hosted by someone
        /// </summary>
        public event Action<IBotChatChannel, HostInformation> Hosted;

        /// <summary>
        /// a micropresent was received
        /// </summary>
        public event Action<IBotChatChannel, MicroPresent> MicroPresent;
    }
}