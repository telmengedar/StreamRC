using System;
using System.Collections.Generic;
using NightlyCode.Core.Logs;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Events {

    /// <summary>
    /// module managing stream events
    /// </summary>
    [Dependency(nameof(UserModule))]
    [Dependency(nameof(StreamModule))]
    [ModuleKey("events")]
    public class StreamEventModule : IInitializableModule, IRunnableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="StreamEventModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public StreamEventModule(Context context) {
            this.context = context;
        }

        public event Action<long, int> EventValue;

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<StreamEvent>();
        }

        void IRunnableModule.Start() {
            StreamModule streammodule = context.GetModule<StreamModule>();
            streammodule.Hosted += OnHosted;
            streammodule.Raid += OnRaid;
            streammodule.MicroPresent += OnMicroPresent;
            streammodule.NewFollower += OnFollow;
            streammodule.NewSubscriber += OnSubscription;
            context.GetModule<UserModule>().UserFlagsChanged += OnUserFlagsChanged;
        }

        void OnUserFlagsChanged(User user) {
            if(user.Flags.HasFlag(UserFlags.Bot)) {
                context.Database.Delete<StreamEvent>().Where(u => u.UserID == user.ID && (u.Type == StreamEventType.Follow || u.Type == StreamEventType.Host)).Execute();
            }
        }

        void IRunnableModule.Stop()
        {
            StreamModule streammodule = context.GetModule<StreamModule>();
            streammodule.Hosted -= OnHosted;
            streammodule.Raid -= OnRaid;
            streammodule.MicroPresent -= OnMicroPresent;
            streammodule.NewFollower -= OnFollow;
            streammodule.NewSubscriber -= OnSubscription;
            context.GetModule<UserModule>().UserFlagsChanged -= OnUserFlagsChanged;
        }

        void OnSubscription(SubscriberInformation subscriber) {
            AddSubscription(context.GetModule<UserModule>().GetExistingUser(subscriber.Service, subscriber.Username).ID, subscriber.Status);
        }

        void OnFollow(UserInformation user) {
            AddFollow(context.GetModule<UserModule>().GetExistingUser(user.Service, user.Username).ID);
        }

        void OnMicroPresent(MicroPresent present) {
            // user doesn't necessarily exist here (could be his first appearance with an immediate donation)
            AddDonation(context.GetModule<UserModule>().GetUser(present.Service, present.Username).ID, present.Amount);
        }

        void OnHosted(HostInformation host) {
            AddHost(host.Service, host.Channel, host.Viewers);
        }

        void OnRaid(RaidInformation raid)
        {
            AddRaid(raid.Service, raid.Login, raid.RaiderCount);
        }

        /// <summary>
        /// adds a subscription information for a user
        /// </summary>
        /// <param name="service">service user is registered at</param>
        /// <param name="username">name of user</param>
        /// <param name="type">subscription type</param>
        public void AddSubscription(string service, string username, UserStatus type) {
            AddSubscription(context.GetModule<UserModule>().GetExistingUser(service, username).ID, type);
        }

        public void AddSubscription(long userid, UserStatus type) {
            long value = 0;
            switch(type) {
                case UserStatus.Subscriber:
                    value = 499;
                    break;
                case UserStatus.BigSubscriber:
                    value = 999;
                    break;
                case UserStatus.PremiumSubscriber:
                    value = 2499;
                    break;
            }
            context.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp).Values(StreamEventType.Subscription, userid, value, DateTime.Now).Execute();
            EventValue?.Invoke(userid, (int)(value * 10));
        }

        public void AddFollow(long userid)
        {
            context.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Timestamp).Values(StreamEventType.Follow, userid, DateTime.Now).Execute();
            EventValue?.Invoke(userid, 500);
        }

        public void AddDonation(string service, string username, long amount) {
            AddDonation(context.GetModule<UserModule>().GetExistingUser(service, username).ID, amount);
        }

        public void AddDonation(string service, string username, long amount, string currency) {
            AddDonation(context.GetModule<UserModule>().GetExistingUser(service, username).ID, amount, 2.0f, currency);
        }

        public void AddBugReport(string service, string username, int severity, string excerpt) {
            AddBugReport(context.GetModule<UserModule>().GetExistingUser(service, username).ID, severity, excerpt);
        }

        public void AddBugReport(long userid, int severity, string excerpt)
        {
            context.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp, e => e.Multiplicator, e => e.Argument).Values(StreamEventType.BugReport, userid, severity, DateTime.Now, 10.0f, excerpt).Execute();
            EventValue?.Invoke(userid, severity * 500);
        }

        /// <summary>
        /// adds a donation event
        /// </summary>
        /// <param name="userid">id of user who donated</param>
        /// <param name="amount">donated amount</param>
        /// <param name="multiplicator">multiplicator to use for evaluation</param>
        /// <param name="currency">currency donation was made in</param>
        public void AddDonation(long userid, long amount, float multiplicator=1.0f, string currency=null) {
            context.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp, e => e.Multiplicator, e => e.Argument).Values(StreamEventType.Donation, userid, amount, DateTime.Now, multiplicator, currency).Execute();
            EventValue?.Invoke(userid, (int)(amount * multiplicator * 10));
        }

        /// <summary>
        /// adds a host event
        /// </summary>
        /// <param name="service">service where user is registered</param>
        /// <param name="user">username</param>
        /// <param name="viewers">number of viewers channel is hosted to</param>
        public void AddHost(string service, string user, int viewers) {
            AddHost(context.GetModule<UserModule>().GetExistingUser(service, user).ID, viewers);
        }

        public void AddHost(long userid, int viewers) {
            double value = viewers <= 0 ? 0.5 : viewers;
            context.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e=>e.Value, e => e.Timestamp).Values(StreamEventType.Host, userid, value, DateTime.Now).Execute();
            EventValue?.Invoke(userid, (int)(value * 10));
        }

        /// <summary>
        /// adds a raid event
        /// </summary>
        /// <param name="service">service where user is registered</param>
        /// <param name="user">username</param>
        /// <param name="raiders">number of viewers channel is hosted to</param>
        public void AddRaid(string service, string user, int raiders)
        {
            AddRaid(context.GetModule<UserModule>().GetExistingUser(service, user).ID, raiders);
        }

        public void AddRaid(long userid, int raiders) {
            double value = raiders <= 0 ? 0.8 : raiders * 1.3;
            context.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp).Values(StreamEventType.Raid, userid, value, DateTime.Now).Execute();
            EventValue?.Invoke(userid, (int)(value * 10));
        }

        public void AddCustomEvent(Message title, Message message) {
            context.Database.Insert<StreamEvent>().Columns(e => e.Title, e => e.Message, e=>e.Timestamp).Values(JSON.WriteString(title), JSON.WriteString(message), DateTime.Now).Execute();
        }

        public IEnumerable<StreamEvent> GetLastEvents(int count) {
            return context.Database.LoadEntities<StreamEvent>().OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => e.Timestamp), false)).Limit(count).Execute();
        }

        public long GetBiggestHoster() {
            DateTime lastmonth = DateTime.Now - TimeSpan.FromDays(30);
            return context.Database.Load<StreamEvent>(e => e.UserID).Where(e => e.Type == StreamEventType.Host && e.Timestamp>lastmonth).GroupBy(EntityField.Create<StreamEvent>(f => f.UserID)).OrderBy(new OrderByCriteria(Aggregate.Sum<EntityField>(EntityField.Create<StreamEvent>(s => s.Value)), false)).Limit(1).ExecuteScalar<long>();
        }

        public long GetBiggestDonor() {
            DateTime lastmonth = DateTime.Now - TimeSpan.FromDays(30);
            return context.Database.Load<StreamEvent>(e => e.UserID).Where(e => (e.Type == StreamEventType.Subscription || e.Type==StreamEventType.Donation) && e.Timestamp > lastmonth).GroupBy(EntityField.Create<StreamEvent>(f => f.UserID)).OrderBy(new OrderByCriteria(Aggregate.Sum<EntityField>(EntityField.Create<StreamEvent>(s => s.Value*s.Multiplicator)), false)).Limit(1).ExecuteScalar<long>();
        }

        public long GetUserOfTheMonth() {
            DateTime lastmonth = DateTime.Now - TimeSpan.FromDays(30);
            return context.Database.Load<StreamEvent>(e => e.UserID).Where(e => e.Timestamp > lastmonth).GroupBy(EntityField.Create<StreamEvent>(f => f.UserID)).OrderBy(new OrderByCriteria(Aggregate.Sum<EntityField>(EntityField.Create<StreamEvent>(s => s.Value * s.Multiplicator)), false)).Limit(1).ExecuteScalar<long>();
        }
    }
}