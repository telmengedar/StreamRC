using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.Twitch.Chat;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Chat;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;
using ChatMessage = StreamRC.Streaming.Stream.Chat.ChatMessage;

namespace StreamRC.Twitch.Chat {

    /// <summary>
    /// user chat in twitch
    /// </summary>
    public class TwitchUserChat : IChatChannel {
        readonly ChatChannel channel;
        readonly UserModule usermodule;
        readonly ImageCacheModule imagecache;

        readonly Dictionary<string, Color> fallbackcolor = new Dictionary<string, Color>();

        readonly Regex linkparser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// creates a new <see cref="TwitchUserChat"/>
        /// </summary>
        /// <param name="channel">channel access</param>
        /// <param name="usermodule">access to usermodule</param>
        /// <param name="imagecache">access to image cache</param>
        public TwitchUserChat(ChatChannel channel, UserModule usermodule, ImageCacheModule imagecache) {
            this.channel = channel;
            this.usermodule = usermodule;
            this.imagecache = imagecache;
            channel.UserJoined += OnUserJoined;
            channel.UserLeft += OnUserLeft;
            channel.MessageReceived += OnMessageReceived;
            channel.Subscription += s => Subscription?.Invoke(s);
        }

        public event Action<Subscription> Subscription;

        void OnUserLeft(ChatChannel twitchchannel, string user) {
            try
            {
                UserLeft?.Invoke(this, new UserInformation
                {
                    Service = Service,
                    Username = user
                });
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering UserLeft event", e);
            }
        }

        void OnUserJoined(ChatChannel twitchchannel, string user) {
            try
            {
                UserJoined?.Invoke(this, new UserInformation
                {
                    Service = Service,
                    Username = user
                });
            }
            catch (Exception e)
            {
                Logger.Error(this, "Error triggering UserJoined event", e);
            }
        }

        Color GetFallbackColor(string user)
        {
            Color color;
            if (!fallbackcolor.TryGetValue(user, out color))
                fallbackcolor[user] = color = Color.FromRgb((byte)(128 + RNG.XORShift64.NextInt(128)), (byte)(128 + RNG.XORShift64.NextInt(128)), (byte)(128 + RNG.XORShift64.NextInt(128)));
            return color;
        }

        Color GetColor(string user, string color)
        {
            if (string.IsNullOrEmpty(color))
                return GetFallbackColor(user);

            try {
                object value = ColorConverter.ConvertFromString(color);
                if(value == null)
                    return GetFallbackColor(user);

                return (Color)value;
            }
            catch (Exception)
            {
                return Colors.White;
            }
        }

        protected virtual bool FilterMessage(NightlyCode.Twitch.Chat.ChatMessage message) {
            return message.Message.StartsWith("!") || message.User == "jtv" || message.User.ToLower() == "ncstreamrc";
        }

        ChatMessage CreateChatMessage(NightlyCode.Twitch.Chat.ChatMessage message) {
            return new ChatMessage {
                Service = Service,
                User = message.User,
                Message = message.Message,
                AvatarLink = usermodule.GetUser(Service, message.User).Avatar,
                UserColor = GetColor(message.User, message.Color),
                IsWhisper = false,
                Emotes = message.Emotes?.Select(e => new ChatEmote {
                    StartIndex = e.FirstIndex,
                    EndIndex = e.LastIndex,
                    ImageID = imagecache.AddImage(e.GetUrl(3), $"{Service}.emote.{e.ID}"),
                }).ToArray() ?? new ChatEmote[0]
            };
        }

        IEnumerable<MessageAttachement> CreateAttachements(IEnumerable<string> urls) {
            foreach(string url in urls) {
                MessageAttachement attachement = null;

                using(HeadClient wc = new HeadClient()) {
                    try {
                        wc.DownloadString(url);
                        switch(wc.ResponseHeaders[HttpResponseHeader.ContentType]) {
                            case "image/png":
                            case "image/jpeg":
                            case "image/gif":
                            case "image/svg+xml":
                                attachement = new MessageAttachement {
                                    Type = AttachmentType.Image,
                                    OriginalSource = url,
                                    URL = imagecache.CreateCacheUrl(imagecache.AddImage(url, DateTime.Now + TimeSpan.FromMinutes(5.0)))
                                };
                                break;
                            default:
                                attachement = new MessageAttachement {
                                    Type = AttachmentType.Unknown,
                                    OriginalSource = url,
                                    URL = url
                                };
                                break;
                        }
                    }
                    catch(Exception e) {
                        Logger.Warning(this, $"Unable to get information about '{url}'", e);
                        attachement = new MessageAttachement
                        {
                            Type = AttachmentType.Unknown,
                            OriginalSource = url,
                            URL = url
                        };
                    }
                }

                if(attachement != null)
                    yield return attachement;
            }
        }

        string ReplaceAttachementText(string text, IEnumerable<MessageAttachement> attachements, string name) {
            int index = 1;
            foreach(MessageAttachement attachement in attachements) {
                text = text.Replace(attachement.OriginalSource, $"[{name}{index++}]");
            }
            return text;
        }

        bool ScanForAttachements(NightlyCode.Twitch.Chat.ChatMessage message) {
            Match[] links = linkparser.Matches(message.Message).Cast<Match>().ToArray();
            if(links.Length == 0)
                return false;

            Logger.Info(this, "Found possible attachements in message", string.Join("\r\n", links.Select(l => l.Value)));

            ChatMessage chatmessage = CreateChatMessage(message);
            chatmessage.Attachements = CreateAttachements(links.Select(l => l.Value)).ToArray();
            chatmessage.Message = ReplaceAttachementText(chatmessage.Message, chatmessage.Attachements.Where(a => a.Type == AttachmentType.Image), "Image");
            ChatMessage?.Invoke(this, chatmessage);
            return true;
        }

        void OnMessageReceived(NightlyCode.Twitch.Chat.ChatMessage message) {
            if(FilterMessage(message))
                return;

            if(ScanForAttachements(message))
                return;

            ChatMessage?.Invoke(this, CreateChatMessage(message));
        }

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
        public string Service => TwitchConstants.ServiceKey;

        /// <summary>
        /// name of chat channel
        /// </summary>
        public string Name => channel.Name;

        /// <summary>
        /// channel flags
        /// </summary>
        public virtual ChannelFlags Flags => ChannelFlags.Chat;

        /// <summary>
        /// users currently in chat
        /// </summary>
        public IEnumerable<string> Users => channel.Users;

        /// <summary>
        /// sends a message to the channel
        /// </summary>
        /// <param name="message">message to send</param>
        public void SendMessage(string message) {
            Logger.Info(this, "Sending message to channel", message);
            string[] messages = message.SplitMessage(500).ToArray();

            foreach (string chunk in messages)
                channel.SendMessage(chunk);
        }

        /// <summary>
        /// initializes the channel after it was added
        /// </summary>
        public void Initialize() {
            foreach(string user in channel.Users)
                OnUserJoined(channel, user);
        }
    }
}