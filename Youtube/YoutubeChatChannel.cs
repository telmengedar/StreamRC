using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using NightlyCode.Core.Helpers;
using NightlyCode.Core.Logs;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Youtube {

    /// <summary>
    /// provides chat data for a broadcast to streaming module
    /// </summary>
    public class YoutubeChatChannel : IBotChatChannel, ITimerService {
        readonly YouTubeService service;
        readonly LiveBroadcast broadcast;

        const string FIELDS = "items(authorDetails(channelId,displayName,isChatModerator,isChatOwner,isChatSponsor,"
                              + "profileImageUrl),snippet(displayMessage,superChatDetails,publishedAt)),"
                              + "nextPageToken,pollingIntervalMillis";

        string pagetoken;
        double cooldown = 0.0f;

        /// <summary>
        /// creates a new <see cref="YoutubeChatChannel"/>
        /// </summary>
        /// <param name="service">access to youtube api</param>
        /// <param name="broadcast">access to broadcast metadata</param>
        public YoutubeChatChannel(YouTubeService service, LiveBroadcast broadcast) {
            this.service = service;
            this.broadcast = broadcast;
        }

        public event Action<IChatChannel, UserInformation> UserJoined;
        public event Action<IChatChannel, UserInformation> UserLeft;
        public event Action<IChatChannel, ChatMessage> ChatMessage;

        public string Service => YoutubeConstants.ServiceName;

        public string Name => broadcast.Snippet.LiveChatId;

        public ChannelFlags Flags => ChannelFlags.Chat | ChannelFlags.LineBreaks | ChannelFlags.Game | ChannelFlags.UserChat;

        public IEnumerable<string> Users
        {
            get { yield break; }
        }

        public void SendMessage(string message) {
            service.LiveChatMessages.Insert(new LiveChatMessage {
                Snippet = new LiveChatMessageSnippet {
                    Type = "textMessageEvent",
                    LiveChatId = broadcast.Snippet.LiveChatId,
                    TextMessageDetails = new LiveChatTextMessageDetails {
                        MessageText = message
                    }
                }
            }, "snippet");
        }

        public void Initialize() {
        }

        public void Process(double time) {
            cooldown -= time;
            if(cooldown <= 0.0) {
                cooldown = 5.0;

                LiveChatMessagesResource.ListRequest request = service.LiveChatMessages.List(broadcast.Snippet.LiveChatId, "id,snippet,authorDetails");
                request.Fields = FIELDS;

                LiveChatMessageListResponse response;
                if (!string.IsNullOrEmpty(pagetoken))
                    request.PageToken = pagetoken;
                else {
                    response = request.Execute();
                    pagetoken = response.NextPageToken;
                    if (response.PollingIntervalMillis.HasValue)
                        cooldown = TimeSpan.FromMilliseconds(response.PollingIntervalMillis.Value).TotalSeconds;
                    return;
                }
                
                response = request.Execute();
                foreach(LiveChatMessage message in response.Items) {
                    ProcessMessage(message);
                }

                pagetoken = response.NextPageToken;
                if(response.PollingIntervalMillis.HasValue)
                    cooldown = TimeSpan.FromMilliseconds(response.PollingIntervalMillis.Value).TotalSeconds;
            }
        }

        void ProcessMessage(LiveChatMessage message) {
            if (message.Snippet.DisplayMessage.StartsWith("!"))
            {
                try
                {
                    int indexof = message.Snippet.DisplayMessage.IndexOf(' ');

                    if (indexof == -1)
                    {
                        CommandReceived?.Invoke(this, new StreamCommand
                        {
                            Service = Service,
                            Channel = Name,
                            IsWhispered = false,
                            User = message.AuthorDetails.DisplayName,
                            Command = message.Snippet.DisplayMessage.Substring(1),
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
                            User = message.AuthorDetails.DisplayName,
                            Command = message.Snippet.DisplayMessage.Substring(1, indexof - 1),
                            Arguments = Commands.SplitArguments(message.Snippet.DisplayMessage.Substring(indexof + 1)).ToArray()
                        });
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(this, "Error triggering CommandReceived event", e);
                }
            }
            else if (message.Snippet.DisplayMessage.StartsWith("$"))
            {
                CommandReceived?.Invoke(this, new StreamCommand
                {
                    Service = Service,
                    Channel = Name,
                    User = message.AuthorDetails.DisplayName,
                    Command = message.Snippet.DisplayMessage.Substring(1),
                    IsSystemCommand = true
                });
            }
            else
            {
                ChatMessage?.Invoke(this, new ChatMessage
                {
                    Service = YoutubeConstants.ServiceName,
                    Channel = Name,
                    User = message.AuthorDetails.DisplayName,
                    AvatarLink = message.AuthorDetails.ProfileImageUrl,
                    Message = message.Snippet.DisplayMessage
                });

            }
        }

        /// <inheritdoc />
        public event Action<IBotChatChannel, StreamCommand> CommandReceived;
    }
}