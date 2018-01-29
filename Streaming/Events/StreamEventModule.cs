using System;
using System.Collections.Generic;
using System.Drawing;
using NightlyCode.Core.Logs;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Events {

    /// <summary>
    /// module managing stream events
    /// </summary>
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    public class StreamEventModule : IInitializableModule, IRunnableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="StreamEventModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public StreamEventModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<StreamEvent>();
        }

        void IRunnableModule.Start() {
            StreamModule streammodule = context.GetModule<StreamModule>();
            streammodule.Hosted += OnHosted;
            streammodule.MicroPresent += OnMicroPresent;
            streammodule.NewFollower += OnFollow;
            streammodule.NewSubscriber += OnSubscription;
        }

        void IRunnableModule.Stop()
        {
            StreamModule streammodule = context.GetModule<StreamModule>();
            streammodule.Hosted -= OnHosted;
            streammodule.MicroPresent -= OnMicroPresent;
            streammodule.NewFollower -= OnFollow;
            streammodule.NewSubscriber -= OnSubscription;
        }

        void OnSubscription(SubscriberInformation subscriber) {
            AddEvent(UserTitle(subscriber.Service, subscriber.Username), new MessageBuilder().Text("Subscription").BuildMessage());
        }

        void OnFollow(UserInformation user) {
            AddEvent(UserTitle(user.Service, user.Username), new MessageBuilder().Text("Follow").BuildMessage());
        }

        void OnMicroPresent(MicroPresent present) {
            AddEvent(UserTitle(present.Service, present.Username), new MessageBuilder().Text("Donated ").Text($"{present.Amount} {present.Currency}", Color.FromArgb(253,255,105), FontWeight.Bold).BuildMessage());
        }

        Message UserTitle(string service, string username) {
            try {
                User user = context.GetModule<UserModule>().GetUser(service, username);
                if(!string.IsNullOrEmpty(user.Avatar)) {
                    string imageid = context.GetModule<ImageCacheModule>().AddImage(user.Avatar).ToString();
                    return new MessageBuilder().Image(imageid).Bold().Color(user.Color).Text(username).BuildMessage();
                }
                return new MessageBuilder().Bold().Color(user.Color).Text(username).BuildMessage();
            }
            catch(Exception e) {
                Logger.Error(this, "Failed to get user for event title", e);
                return new Message(new[] {
                    new MessageChunk(MessageChunkType.Text, username, null, FontWeight.Bold)
                });
            }
        }

        void OnHosted(HostInformation host) {
            MessageBuilder content = new MessageBuilder().Text("Host");
            if(host.Viewers > 0)
                content.Text($" ({host.Viewers}) ");

            AddEvent(UserTitle(host.Service, host.Channel), content.BuildMessage());
        }

        public void AddEvent(Message title, Message message) {
            context.Database.Insert<StreamEvent>().Columns(e => e.Title, e => e.Message, e=>e.Timestamp).Values(JSON.WriteString(title), JSON.WriteString(message), DateTime.Now).Execute();
        }

        public IEnumerable<StreamEvent> GetLastEvents(int count) {
            return context.Database.LoadEntities<StreamEvent>().OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => e.Timestamp), false)).Limit(count).Execute();
        }
    }
}