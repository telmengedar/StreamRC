using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Conversion;
using NightlyCode.Database.Entities.Operations;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Japi.Json;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Events
{

    /// <summary>
    /// module managing stream events
    /// </summary>
    [Module(Key = "events", AutoCreate = true)]
    public class StreamEventModule {
        readonly DatabaseModule database;
        readonly UserModule users;

        /// <summary>
        /// creates a new <see cref="StreamEventModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public StreamEventModule(DatabaseModule database, StreamModule streammodule, UserModule users) {
            this.database = database;
            this.users = users;
            database.Database.UpdateSchema<StreamEvent>();

            streammodule.Hosted += OnHosted;
            streammodule.Raid += OnRaid;
            streammodule.MicroPresent += OnMicroPresent;
            streammodule.NewFollower += OnFollow;
            streammodule.NewSubscriber += OnSubscription;
            streammodule.ChatMessage += OnChatMessage;

            users.UserFlagsChanged += OnUserFlagsChanged;
        }

        public event Action<long, int> EventValue;

        void OnChatMessage(IChatChannel channel, ChatMessage message) {
            long userid = users.GetUserID(message.Service, message.User);
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp, e => e.Multiplicator).Values(StreamEventType.Chat, userid, 1, DateTime.Now, 0.02).Execute();
        }

        void OnUserFlagsChanged(User user) {
            if(user.Flags.HasFlag(UserFlags.Bot)) {
                database.Database.Delete<StreamEvent>().Where(u => u.UserID == user.ID && (u.Type == StreamEventType.Follow || u.Type == StreamEventType.Host)).Execute();
            }
        }

        void OnSubscription(SubscriberInformation subscriber) {
            AddSubscription(users.GetExistingUser(subscriber.Service, subscriber.Username).ID, subscriber.Status);
        }

        void OnFollow(UserInformation user) {
            AddFollow(users.GetExistingUser(user.Service, user.Username).ID);
        }

        void OnMicroPresent(MicroPresent present) {
            // user doesn't necessarily exist here (could be his first appearance with an immediate donation)
            AddDonation(users.GetUser(present.Service, present.Username).ID, present.Amount);
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
            AddSubscription(users.GetExistingUser(service, username).ID, type);
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
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e=>e.Multiplicator, e => e.Timestamp).Values(StreamEventType.Subscription, userid, value, 2.0, DateTime.Now).Execute();
            EventValue?.Invoke(userid, (int)(value * 10));
        }

        public void AddFollow(long userid)
        {
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Timestamp).Values(StreamEventType.Follow, userid, DateTime.Now).Execute();
            EventValue?.Invoke(userid, 500);
        }

        public void AddDonation(string service, string username, long amount) {
            AddDonation(users.GetExistingUser(service, username).ID, amount);
        }

        public void AddDonation(string service, string username, long amount, string currency) {
            AddDonation(users.GetExistingUser(service, username).ID, amount, 2.0f, currency);
        }

        public void AddBugReport(string service, string username, int severity, string excerpt) {
            AddBugReport(users.GetExistingUser(service, username).ID, severity, excerpt);
        }

        public void AddBugReport(long userid, int severity, string excerpt)
        {
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp, e => e.Multiplicator, e => e.Argument).Values(StreamEventType.BugReport, userid, severity, DateTime.Now, 10.0f, excerpt).Execute();
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
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e => e.Timestamp, e => e.Multiplicator, e => e.Argument).Values(StreamEventType.Donation, userid, amount, DateTime.Now, multiplicator, currency).Execute();
            EventValue?.Invoke(userid, (int)(amount * multiplicator * 10));
        }

        /// <summary>
        /// adds a host event
        /// </summary>
        /// <param name="service">service where user is registered</param>
        /// <param name="user">username</param>
        /// <param name="viewers">number of viewers channel is hosted to</param>
        public void AddHost(string service, string user, int viewers) {
            AddHost(users.GetExistingUser(service, user).ID, viewers);
        }

        public void AddHost(long userid, int viewers) {
            double value = viewers <= 0 ? 0.5 : viewers;
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e=>e.Value, e=>e.Multiplicator, e => e.Timestamp).Values(StreamEventType.Host, userid, value, 1.0, DateTime.Now).Execute();
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
            AddRaid(users.GetExistingUser(service, user).ID, raiders);
        }

        public void AddRaid(long userid, int raiders) {
            double value = raiders <= 0 ? 0.8 : raiders * 1.3;
            database.Database.Insert<StreamEvent>().Columns(e => e.Type, e => e.UserID, e => e.Value, e=>e.Multiplicator, e => e.Timestamp).Values(StreamEventType.Raid, userid, value, 1.0, DateTime.Now).Execute();
            EventValue?.Invoke(userid, (int)(value * 10));
        }

        public void AddCustomEvent(Message title, Message message) {
            database.Database.Insert<StreamEvent>().Columns(e => e.Title, e => e.Message, e=>e.Timestamp).Values(JSON.WriteString(title), JSON.WriteString(message), DateTime.Now).Execute();
        }

        /// <summary>
        /// get the last events which occured in this stream
        /// </summary>
        /// <param name="count">number of events to return</param>
        /// <returns>stream events</returns>
        public IEnumerable<StreamEvent> GetLastEvents(int count, params StreamEventType[] types) {
            if(types.Length == 0) {
                return database.Database.LoadEntities<StreamEvent>()
                    .Where(e => e.Type != StreamEventType.Chat)
                    .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => e.Timestamp), false))
                    .Limit(count)
                    .Execute();
            }
            return database.Database.LoadEntities<StreamEvent>()
                .Where(e => types.Contains(e.Type))
                .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => e.Timestamp), false))
                .Limit(count)
                .Execute();
        }

        public long GetBiggestHoster() {
            DateTime lastmonth = DateTime.Now - TimeSpan.FromDays(30);
            return database.Database.Load<StreamEvent>(e => e.UserID)
                .Where(e => e.Type == StreamEventType.Host && e.Timestamp>lastmonth)
                .GroupBy(EntityField.Create<StreamEvent>(f => f.UserID))
                .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => DBFunction.Sum(e.Value * e.Multiplicator)), false))
                .Limit(1)
                .ExecuteScalar<long>();
        }

        public long GetBiggestDonor() {
            DateTime lastmonth = DateTime.Now - TimeSpan.FromDays(30);
            return database.Database.Load<StreamEvent>(e => e.UserID)
                .Where(e => (e.Type == StreamEventType.Subscription || e.Type==StreamEventType.Donation) && e.Timestamp > lastmonth)
                .GroupBy(EntityField.Create<StreamEvent>(f => f.UserID))
                .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => DBFunction.Sum(e.Value * e.Multiplicator)), false))
                .Limit(1)
                .ExecuteScalar<long>();
        }

        /// <summary>
        /// get user which scored the highest score the last month
        /// </summary>
        /// <returns>event score for leader of last month</returns>
        public EventScore GetUserOfTheMonth() {
            DateTime now = DateTime.Now;
            DateTime thismonth = new DateTime(now.Year, now.Month, 1);
            now=now.AddMonths(-1);
            DateTime lastmonth = new DateTime(now.Year, now.Month, 1);

            return database.Database.Load<StreamEvent>(e => e.UserID, e => DBFunction.Sum(e.Value * e.Multiplicator))
                .Where(e => e.Timestamp >= lastmonth && e.Timestamp < thismonth)
                .GroupBy(EntityField.Create<StreamEvent>(f => f.UserID))
                .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => DBFunction.Sum(e.Value * e.Multiplicator)), false))
                .Limit(1)
                .ExecuteType(r => new EventScore {
                    UserID = Converter.Convert<long>(r[0]),
                    Score = Converter.Convert<double>(r[1])
                }).FirstOrDefault();
        }

        /// <summary>
        /// get event score for a user leading in score of a event type
        /// </summary>
        /// <param name="type">type of event to evaluate</param>
        /// <returns>event score of last month or null if no data matches</returns>
        public EventScore GetLeader(params StreamEventType[] types) {
            return database.Database.Load<StreamEvent>(e => e.UserID, e => DBFunction.Sum(e.Value * e.Multiplicator))
                .Where(e=>types.Contains(e.Type))
                .GroupBy(EntityField.Create<StreamEvent>(f => f.UserID))
                .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => DBFunction.Sum(e.Value * e.Multiplicator)), false))
                .Limit(1)
                .ExecuteType(r => new EventScore
                {
                    UserID = Converter.Convert<long>(r[0]),
                    Score = Converter.Convert<double>(r[1])
                }).FirstOrDefault();
        }

        /// <summary>
        /// get event score for a user leading in score of a event type of the last month
        /// </summary>
        /// <param name="type">type of event to evaluate</param>
        /// <returns>event score of last month or null if no data matches</returns>
        public EventScore GetLastMonthLeader(params StreamEventType[] types) {
            DateTime now = DateTime.Now;
            DateTime thismonth = new DateTime(now.Year, now.Month, 1);
            now = now.AddMonths(-1);
            DateTime lastmonth = new DateTime(now.Year, now.Month, 1);

            return database.Database.Load<StreamEvent>(e => e.UserID, e => DBFunction.Sum(e.Value * e.Multiplicator))
                .Where(e => types.Contains(e.Type) && e.Timestamp >= lastmonth && e.Timestamp < thismonth)
                .GroupBy(EntityField.Create<StreamEvent>(f => f.UserID))
                .OrderBy(new OrderByCriteria(EntityField.Create<StreamEvent>(e => DBFunction.Sum(e.Value * e.Multiplicator)), false))
                .Limit(1)
                .ExecuteType(r => new EventScore
                {
                    UserID = Converter.Convert<long>(r[0]),
                    Score = Converter.Convert<double>(r[1])
                }).FirstOrDefault();
        }
    }
}