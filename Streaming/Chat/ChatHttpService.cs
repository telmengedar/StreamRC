using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Conversion;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.Net.Http;
using NightlyCode.Net.Http.Requests;
using NightlyCode.StreamRC.Modules;
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
    [Dependency(nameof(MessageModule))]
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(HttpServiceModule))]
    [Dependency(nameof(TimerModule))]
    public class ChatHttpService : IInitializableModule, IHttpService, IRunnableModule, ITimerService {
        readonly Context context;

        readonly object messagelock=new object();
        readonly List<ChatHttpMessage> messages = new List<ChatHttpMessage>();

        readonly TimeSpan threshold = TimeSpan.FromMinutes(1.0);

        /// <summary>
        /// creates a new <see cref="ChatHttpService"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public ChatHttpService(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/chat", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/chat.css", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/chat.js", this);
            context.GetModule<HttpServiceModule>().AddServiceHandler("/streamrc/chat/messages", this);

            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
        }

        IEnumerable<MessageChunk> CreateMessageChunks(ChatMessage message) {
            yield return new MessageChunk(MessageChunkType.Emoticon, context.GetModule<ImageCacheModule>().AddImage($"http://localhost/streamrc/services/icon?service={message.Service}").ToString());

            foreach(MessageChunk chunk in CreateUserChunks(message))
                yield return chunk;

            foreach(MessageChunk chunk in CreateMessageParts(message.Message, message.Emotes))
                yield return chunk;
        } 

        void OnChatMessage(ChatMessage message) {
            if(message.IsWhisper || message.Message.StartsWith("!"))
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

                        long imageid = context.GetModule<ImageCacheModule>().ExtractIDFromUrl(attachement.URL);
                        if (imageid == -1)
                            imageid = context.GetModule<ImageCacheModule>().AddImage(attachement.URL, DateTime.Now + TimeSpan.FromMinutes(5.0));

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
                yield return new MessageChunk(MessageChunkType.Emoticon, context.GetModule<ImageCacheModule>().AddImage(message.AvatarLink).ToString());

            if((context.GetModule<UserModule>().GetUser(message.Service, message.User).Flags & UserFlags.Brainy) == UserFlags.Brainy)
                yield return new MessageChunk(MessageChunkType.Emoticon, $"http://localhost/streamrc/users/flag?id=4");
            if ((context.GetModule<UserModule>().GetUser(message.Service, message.User).Flags & UserFlags.Racist) == UserFlags.Racist)
                yield return new MessageChunk(MessageChunkType.Emoticon, $"http://localhost/streamrc/users/flag?id=8");

            yield return new MessageChunk(MessageChunkType.Text, message.User, message.UserColor.FixColor(), FontWeight.Bold);
        } 

        IEnumerable<MessageChunk> CreateMessageParts(string message, ChatEmote[] emotes) {
            if (emotes.Length == 0) {
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

        void IHttpService.ProcessRequest(HttpClient client, HttpRequest request) {
            switch(request.Resource) {
                case "/streamrc/chat":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Chat.chat.html"), ".html");
                    break;
                case "/streamrc/chat.css":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Chat.chat.css"), ".css");
                    break;
                case "/streamrc/chat.js":
                    client.ServeResource(ResourceAccessor.GetResource<System.IO.Stream>("StreamRC.Streaming.Http.Chat.chat.js"), ".js");
                    break;
                case "/streamrc/chat/messages":
                    ServeMessages(client, request);
                    break;
                default:
                    throw new Exception("Resource not managed by this module");
            }
        }

        void ServeMessages(HttpClient client, HttpRequest request) {
            DateTime messagethreshold = Converter.Convert<DateTime>(Converter.Convert<long>(request.GetParameter("timestamp")));

            lock(messagelock) {
                using(MemoryStream ms = new MemoryStream()) {
                    ChatHttpResponse response = new ChatHttpResponse {
                        Timestamp = DateTime.Now,
                        Messages = messages.Where(m => m.Timestamp > messagethreshold).ToArray()
                    };
                    JSON.Write(response, ms);
                    client.ServeData(ms.ToArray(), ".json");
                }
            }
        }

        void IRunnableModule.Start() {
            context.GetModule<TimerModule>().AddService(this, 1.0);
            context.GetModule<MessageModule>().Message += OnSystemMessage;
        }

        void IRunnableModule.Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
            context.GetModule<MessageModule>().Message -= OnSystemMessage;
        }

        void ITimerService.Process(double time) {
#if TEST
            if(RNG.XORShift64.NextFloat() < 0.2f) {
                OnChatMessage(new ChatMessage {
                    User = "Anton",
                    UserColor = Colors.Red,
                    Message = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam"
                });
            }
#endif

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