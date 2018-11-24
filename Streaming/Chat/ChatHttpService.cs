using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Conversion;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using StreamRC.Core.Http;
using StreamRC.Core.Messages;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Chat {

    /// <summary>
    /// provides an html window for chat messages
    /// </summary>
    [Module(AutoCreate = true)]
    public class ChatHttpService : IHttpService, ITimerService {
        readonly ImageCacheModule imagecache;
        readonly UserModule users;

        readonly object messagelock=new object();
        readonly List<ChatHttpMessage> messages = new List<ChatHttpMessage>();

        readonly TimeSpan threshold = TimeSpan.FromMinutes(1.0);

        /// <summary>
        /// creates a new <see cref="ChatHttpService"/>
        /// </summary>
        /// <param name="streammodule">access to stream module</param>
        /// <param name="httpservice">access to http service</param>
        /// <param name="imagecache">access to image cache</param>
        /// <param name="timer">access to timer module</param>
        /// <param name="messages">access to system messages</param>
        /// <param name="users">access to user information</param>
        public ChatHttpService(IStreamModule streammodule, IHttpServiceModule httpservice, ImageCacheModule imagecache, TimerModule timer, MessageModule messages, UserModule users) {
            this.imagecache = imagecache;
            this.users = users;
            httpservice.AddServiceHandler("/streamrc/chat", this);
            httpservice.AddServiceHandler("/streamrc/chat.css", this);
            httpservice.AddServiceHandler("/streamrc/chat.js", this);
            httpservice.AddServiceHandler("/streamrc/chat/messages", this);

            streammodule.ChatMessage += OnChatMessage;
            timer.AddService(this, 1.0);
            messages.Message += OnSystemMessage;
        }

        IEnumerable<MessageChunk> CreateMessageChunks(ChatMessage message) {
            yield return new MessageChunk(MessageChunkType.Emoticon, $"http://localhost/streamrc/services/icon?service={message.Service}");

            foreach(MessageChunk chunk in CreateUserChunks(message))
                yield return chunk;

            foreach(MessageChunk chunk in CreateMessageParts(message.Message, message.Emotes))
                yield return chunk;
        } 

        void OnChatMessage(IChatChannel channel, ChatMessage message) {
            if(message.IsWhisper || message.Message.StartsWith("!") || !channel.Flags.HasFlag(ChannelFlags.Chat))
                return;

            lock(messagelock) {
                messages.Add(new ChatHttpMessage {
                    Timestamp = DateTime.Now,
                    Content = CreateMessageChunks(message).ToArray()
                });

                if(message.Attachements != null) {
                    foreach(MessageAttachement attachement in message.Attachements) {
                        if(attachement.Type != AttachmentType.Image)
                            continue;

                        int width = 0;
                        int height = 0;

                        if(attachement.Width > 0 && attachement.Height>0) {
                            width = attachement.Width;
                            height = attachement.Height;
                            if (width > height)
                            {
                                float aspect = (float)height / width;
                                width = 320;
                                height = (int)(width * aspect);
                            }
                            else
                            {
                                float aspect = (float)width / height;
                                height = 180;
                                width = (int)(height * aspect);
                            }
                        }
                        else {
                            width = 320;
                        }

                        long imageid = imagecache.ExtractIDFromUrl(attachement.URL);
                        if (imageid == -1)
                            imageid = imagecache.GetImageByUrl(attachement.URL);

                        messages.Add(new ChatHttpMessage {
                            Timestamp = DateTime.Now,
                            Content = new[] {
                                new MessageChunk(MessageChunkType.Image, imageid.ToString()) {
                                    Width = width,
                                    Height = height
                                }
                            }
                        });
                    }
                }
            }
        }

        void OnSystemMessage(Message message)
        {
            lock(messagelock) {
                messages.Add(new ChatHttpMessage {
                    Timestamp = DateTime.Now,
                    Content = message.Chunks
                });
            }
        }

        IEnumerable<MessageChunk> CreateUserChunks(ChatMessage message) {
            if(!string.IsNullOrEmpty(message.AvatarLink))
                yield return new MessageChunk(MessageChunkType.Emoticon, imagecache.GetImageByUrl(message.AvatarLink).ToString());

            if((users.GetUser(message.Service, message.User).Flags & UserFlags.Brainy) == UserFlags.Brainy)
                yield return new MessageChunk(MessageChunkType.Emoticon, "http://localhost/streamrc/users/flag?id=4");
            if ((users.GetUser(message.Service, message.User).Flags & UserFlags.Racist) == UserFlags.Racist)
                yield return new MessageChunk(MessageChunkType.Emoticon, "http://localhost/streamrc/users/flag?id=8");

            yield return new MessageChunk(MessageChunkType.Text, message.User, message.UserColor.FixColor(), FontWeight.Bold);
        } 

        IEnumerable<MessageChunk> CreateMessageParts(string message, ChatEmote[] emotes) {
            if(emotes == null || emotes.Length == 0) {
                yield return new MessageChunk(MessageChunkType.Text, message);
                yield break;
            }

            int laststart = 0;
            foreach (ChatEmote emote in emotes) {
                if(emote.StartIndex > laststart)
                    yield return new MessageChunk(MessageChunkType.Text, message.Substring(laststart, emote.StartIndex - laststart));

                yield return new MessageChunk(MessageChunkType.Emoticon, emote.ImageID.ToString());
                laststart = emote.EndIndex + 1;
            }

            if(laststart < message.Length)
                yield return new MessageChunk(MessageChunkType.Text, message.Substring(laststart));
        }

        void IHttpService.ProcessRequest(IHttpRequest request, IHttpResponse response) {
            switch(request.Resource) {
                case "/streamrc/chat":
                    response.ContentType = MimeTypes.GetMimeType(".html");
                    ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Chat.chat.html").CopyTo(response.Content);
                    break;
                case "/streamrc/chat.css":
                    response.ContentType = MimeTypes.GetMimeType(".css");
                    ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Chat.chat.css").CopyTo(response.Content);
                    break;
                case "/streamrc/chat.js":
                    response.ContentType = MimeTypes.GetMimeType(".js");
                    ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Chat.chat.js").CopyTo(response.Content);
                    break;
                case "/streamrc/chat/messages":
                    ServeMessages(request, response);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeMessages(IHttpRequest request, IHttpResponse response) {
            DateTime messagethreshold = Converter.Convert<DateTime>(Converter.Convert<long>(request.Query["timestamp"]));

            lock (messagelock) {
                ChatHttpResponse httpresponse = new ChatHttpResponse {
                    Timestamp = DateTime.Now,
                    Messages = messages.Where(m => m.Timestamp > messagethreshold).ToArray()
                };
                response.ContentType = MimeTypes.GetMimeType(".json");
                JSON.Write(httpresponse, response.Content);
            }
        }

        void ITimerService.Process(double time) {
            DateTime now = DateTime.Now;
            lock (messagelock)
            {
                for (int i = messages.Count - 1; i >= 0; --i)
                {
                    if (now - messages[i].Timestamp > threshold)
                        messages.RemoveAt(i);
                }
            }
        }
    }
}