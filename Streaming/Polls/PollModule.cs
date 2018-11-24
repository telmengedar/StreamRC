using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Conversion;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.Database.Entities.Operations;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using NightlyCode.Modules.Commands;
using StreamRC.Core;
using StreamRC.Core.Scripts;
using StreamRC.Core.Settings;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Collections;
using StreamRC.Streaming.Notifications;
using StreamRC.Streaming.Polls.Commands;
using StreamRC.Streaming.Polls.Notifications;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Ticker;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// manages user polls for stream
    /// </summary>
    [Module(Key="poll")]
    [ModuleCommand("vote", typeof(VoteCommandHandler))]
    [ModuleCommand("pollresult", typeof(PollResultCommandHandler))]
    [ModuleCommand("pollinfo", typeof(PollInfoCommandHandler))]
    public class PollModule : ICommandModule, ITimerService
    {
        readonly DatabaseModule database;
        readonly ISettings settings;
        readonly TickerModule ticker;
        readonly CollectionModule collections;
        float period;

        readonly object availablelock = new object();
        string[] availablepolls;
        int index;
        bool showPollAfterVote;

        readonly PollTickerGenerator tickergenerator;
        PollNotificationGenerator notificationgenerator;

        /// <summary>
        /// creates a new <see cref="PollModule"/>
        /// </summary>
        /// <param name="context">module context</param>
        public PollModule(DatabaseModule database, ISettings settings, StreamModule stream, TimerModule timer, TickerModule ticker, CollectionModule collections, NotificationModule notifications)
        {
            this.database = database;
            this.settings = settings;
            this.ticker = ticker;
            this.collections = collections;

            database.Database.UpdateSchema<Poll>();
            database.Database.UpdateSchema<PollVote>();
            database.Database.UpdateSchema<PollOption>();
            database.Database.UpdateSchema<WeightedVote>();
            database.Database.UpdateSchema<ActivePoll>();

            LoadSettings();

            notificationgenerator = new PollNotificationGenerator(this, notifications);
            ReloadAvailablePolls();

            collections.ItemAdded += OnCollectionItemAdded;
            collections.ItemRemoved += OnCollectionItemRemoved;
            collections.ItemBlocked += OnCollectionItemBlocked;

            timer.AddService(this, Period);

            tickergenerator = new PollTickerGenerator(database, this);
            if (tickergenerator.PollOptionCount > 0)
                ticker.AddSource(tickergenerator);
        }

        public event Action<Poll> PollCreated;

        public event Action<Poll> PollRemoved;

        public event Action<Poll> PollCleared;

        public event Action<PollOption> OptionAdded;

        public event Action<PollOption> OptionRemoved;

        public event Action<PollVote> VoteAdded;

        public event Action<PollVote> VoteRemoved;

        public event Action<PollOption> OptionReset;

        /// <summary>
        /// triggered when a poll is to be shown
        /// </summary>
        public event Action<Poll> PollShown;

        void OnPollCreated(Poll poll)
        {
            ReloadAvailablePolls();
            PollCreated?.Invoke(poll);
        }

        void OnPollRemoved(Poll poll)
        {
            ReloadAvailablePolls();
            PollRemoved?.Invoke(poll);
        }

        void OnOptionAdded(PollOption option)
        {
            OptionAdded?.Invoke(option);
            if (tickergenerator.PollOptionCount == 1)
                ticker.AddSource(tickergenerator);
        }

        void OnOptionRemoved(PollOption option)
        {
            OptionRemoved?.Invoke(option);
            if (tickergenerator.PollOptionCount == 0)
                ticker.RemoveSource(tickergenerator);
        }

        /// <summary>
        /// determines whether a vote automatically shows the corresponding poll in stream
        /// </summary>
        public bool ShowPollAfterVote
        {
            get => showPollAfterVote;
            set
            {
                showPollAfterVote = value;
                settings.Set(this, "showpollaftervote", value);
            }
        }

        /// <summary>
        /// time for next poll to show
        /// </summary>
        public float Period
        {
            get => period;
            set
            {
                period = value;
                settings.Set(this, "period", value);
            }
        }

        void ShowPoll(string pollname = null)
        {
            if (pollname == null)
            {
                lock (availablelock)
                {
                    if (availablepolls.Length > 0)
                        pollname = availablepolls[++index % availablepolls.Length];
                }
            }

            if (pollname != null)
            {
                Poll poll = database.Database.LoadEntities<Poll>().Where(p => p.Name == pollname).Execute().FirstOrDefault();
                PollShown?.Invoke(poll);
            }
        }

        /// <summary>
        /// clears a poll of all votes
        /// </summary>
        /// <param name="pollname"></param>
        public void ClearPoll(string pollname)
        {
            database.Database.Delete<PollVote>().Where(p => p.Poll == pollname).Execute();

            Logger.Info(this, $"'{pollname}' cleared.");
            PollCleared?.Invoke(new Poll {
                Name = pollname
            });
        }

        void ShowCollection(Collection collection = null)
        {
            if (collection == null)
                collection = collections.GetCollections().RandomItem(RNG.XORShift64);

            if (collection == null)
                return;

            WeightedCollectionItem[] items = collections.GetWeightedItems(collection.Name);
            // TODO: Show collection graph
        }

        void ReloadAvailablePolls()
        {
            lock (availablelock)
                availablepolls = database.Database.Load<ActivePoll>(p => p.Name).ExecuteSet<string>().ToArray();
        }

        public void ExecuteVote(string poll, string user, string option) {
            if (database.Database.Update<PollVote>().Set(p => p.Vote == option).Where(p => p.Poll == poll && p.User == user).Execute() == 0)
                database.Database.Insert<PollVote>().Columns(p => p.Poll, p => p.User, p => p.Vote).Values(poll, user, option).Execute();

            if (ShowPollAfterVote)
                ShowPoll(poll);

            // if this is a vote for a new poll, include poll in polls to be shown
            if (availablepolls.All(p => p != poll))
                ReloadAvailablePolls();

            VoteAdded?.Invoke(new PollVote {
                Poll = poll,
                User = user,
                Vote = option
            });
        }

        /// <summary>
        /// determines whether a poll with the specified name exists in database
        /// </summary>
        /// <param name="name">name of poll</param>
        /// <returns>true when a poll named <see cref="name"/> was found, false otherwise</returns>
        public bool ExistsPoll(string name) {
            return database.Database.Load<Poll>(c => DBFunction.Count).Where(p => p.Name == name).ExecuteScalar<long>() > 0;
        }

        /// <summary>
        /// determines whether a poll has a specified option
        /// </summary>
        /// <param name="poll">name of poll</param>
        /// <param name="option">name of option</param>
        /// <returns>true when poll named <see cref="poll"/> has an option named <see cref="option"/>, false otherwise</returns>
        public bool ExistsOption(string poll, string option) {
            return database.Database.Load<PollOption>(c => DBFunction.Count).Where(o => o.Poll == poll && o.Key == option).ExecuteScalar<long>() > 0;
        }

        /// <summary>
        /// determines whether a poll has any predefined options
        /// </summary>
        /// <param name="poll">name of poll</param>
        /// <returns>true when options for the poll are defined, false otherwise</returns>
        public bool HasOptions(string poll) {
            return database.Database.Load<PollOption>(c => DBFunction.Count).Where(o => o.Poll == poll).ExecuteScalar<long>() > 0;
        }

        public PollOption[] FindOptions(string[] arguments) {
            string optionname = string.Join(" ", arguments.Select(a => a.ToLower()));
            PollOption[] options = database.Database.LoadEntities<PollOption>().Where(o => o.Description.ToLower() == optionname).Execute().ToArray();
            if (options.Length == 0)
                options = database.Database.LoadEntities<PollOption>().Where(o => o.Key == optionname).Execute().ToArray();
            return options;
        }

        void OnCollectionItemBlocked(Collection collection, BlockedCollectionItem item)
        {
            if (ShowPollAfterVote)
                ShowCollection(collection);
        }

        void OnCollectionItemRemoved(Collection collection, CollectionItem item)
        {
            if (ShowPollAfterVote)
                ShowCollection(collection);
        }

        void OnCollectionItemAdded(Collection collection, CollectionItem item)
        {
            if (ShowPollAfterVote)
                ShowCollection(collection);
        }

        public ActivePoll GetMostActivePoll() {
            return database.Database.LoadEntities<ActivePoll>().OrderBy(new OrderByCriteria(EntityField.Create<ActivePoll>(p => p.Votes), false)).Limit(1).Execute().FirstOrDefault();
        }

        void ICommandModule.ProcessCommand(string command, string[] arguments)
        {
            switch (command)
            {
                case "create":
                    CreatePoll(arguments[0].ToLower(), arguments.Length > 1 ? arguments[1] : "", arguments.Length > 2 ? arguments[2] : null);
                    break;
                case "remove":
                    RemovePoll(arguments[0].ToLower());
                    break;
                case "addoption":
                    CreatePollOption(arguments[0].ToLower(), arguments[1], arguments[2]);
                    break;
                case "removeoption":
                    RemovePollOption(arguments[0].ToLower(), arguments[1]);
                    break;
                case "resetoption":
                    ClearPollOption(arguments[0].ToLower(), arguments[1].ToLower());
                    break;
                case "changedescription":
                    ChangeDescription(arguments[0].ToLower(), arguments[1]);
                    break;
                case "removevote":
                    RemoveVote(arguments[0].ToLower(), arguments[1]);
                    break;
                case "show":
                    ShowPoll(arguments[0]);
                    break;
                case "showcollection":
                    ShowCollection(collections.GetCollection(arguments[0].ToLower()));
                    break;
                case "showpollaftervote":
                    switch (arguments[0])
                    {
                        case "true":
                            ShowPollAfterVote = true;
                            break;
                        case "false":
                            ShowPollAfterVote = false;
                            break;
                        default:
                            throw new Exception($"Argument '{arguments[0]}' for showpollaftervote not understood: showpollaftervote <true|false>");
                    }
                    break;
                case "period":
                    Period = Converter.Convert<float>(arguments[0]);
                    break;
                default:
                    throw new Exception($"Command '{command}' not implemented by this module");
            }
        }

        void ClearPollOption(string poll, string option) {
            database.Database.Delete<PollVote>().Where(p => p.Poll == poll && p.Vote == option).Execute();
            Logger.Info(this, $"Option '{option}' of poll '{poll}' was reset.");
            OptionReset?.Invoke(new PollOption() {
                Poll = poll,
                Key = option
            });
        }

        void RemoveVote(string poll, string user)
        {
            database.Database.Delete<PollVote>().Where(v => v.Poll == poll && v.User == user).Execute();
            ReloadAvailablePolls();

            Logger.Info(this, $"Vote of user '{user}' removed from '{poll}'");
            VoteRemoved?.Invoke(new PollVote
            {
                Poll = poll,
                User = user
            });
        }

        public void RemovePoll(string poll)
        {
            database.Database.Delete<Poll>().Where(p => p.Name == poll).Execute();
            database.Database.Delete<PollVote>().Where(p => p.Poll == poll).Execute();
            database.Database.Delete<PollOption>().Where(p => p.Poll == poll).Execute();
            ReloadAvailablePolls();

            Logger.Info(this, $"Poll '{poll}' removed.");
            OnPollRemoved(new Poll
            {
                Name = poll
            });
        }

        [Command("myvote", "$user", 0)]
        public IEnumerable<PollVote> GetUserVotes(string user, string poll=null) {
            if(string.IsNullOrEmpty(poll))
                return database.Database.LoadEntities<PollVote>().Where(v => v.User == user).Execute();
            return database.Database.LoadEntities<PollVote>().Where(v => v.User == user && v.Poll == poll).Execute();
        }

        public IEnumerable<PollVote> GetUserVotes(string user, string[] options)
        {
            return database.Database.LoadEntities<PollVote>().Where(v => v.User==user && options.Contains(v.Vote)).Execute();
        }

        public void ChangePoll(string oldname, string newname, string description)
        {
            database.Database.Update<Poll>().Set(p => p.Name == newname, p => p.Description == description).Where(p => p.Name == oldname).Execute();

            if (oldname != newname)
            {
                database.Database.Update<PollOption>().Set(o => o.Poll == newname).Where(o => o.Poll == oldname).Execute();
                database.Database.Update<PollVote>().Set(v => v.Poll == newname).Where(v => v.Poll == oldname).Execute();
            }

            Logger.Info(this, $"Poll '{oldname}' changed.", $"{newname} - {description}");
        }

        public void ChangePollOption(string poll, string oldname, string newname, string description)
        {
            database.Database.Update<PollOption>().Set(p => p.Key == newname, p => p.Description == description).Where(p => p.Poll == poll && p.Key == oldname).Execute();

            if (oldname != newname)
                database.Database.Update<PollVote>().Set(p => p.Vote == newname).Where(p => p.Poll == poll && p.Vote == oldname).Execute();

            Logger.Info(this, $"Polloption '{oldname}' of '{poll}' changed.", $"{newname}, {description}");
        }

        void ChangeDescription(string poll, string description)
        {
            database.Database.Update<Poll>().Set(p => p.Description == description).Where(p => p.Name == poll).Execute();
        }

        public void RemovePollOption(string poll, string option)
        {
            database.Database.Delete<PollOption>().Where(o => o.Poll == poll && o.Key == option).Execute();
            database.Database.Delete<PollVote>().Where(o => o.Poll == poll && o.Vote == option).Execute();
            Logger.Info(this, $"Available options for '{poll}' changed", string.Join(", ", database.Database.Load<PollOption>(o => o.Key).Where(o => o.Poll == poll).ExecuteSet<string>()));
            ReloadAvailablePolls();
            OnOptionRemoved(new PollOption
            {
                Poll = poll,
                Key = option
            });
        }

        public void CreatePollOption(string poll, string option, string description)
        {
            if (database.Database.Load<Poll>(c => DBFunction.Count).Where(p => p.Name == poll).ExecuteScalar<long>() == 0)
                throw new Exception($"No poll named '{poll}' found");

            if (database.Database.Update<PollOption>().Set(o => o.Description == description).Where(o => o.Poll == poll && o.Key == option).Execute() == 0)
                database.Database.Insert<PollOption>().Columns(o => o.Poll, o => o.Key, o => o.Description).Values(poll, option, description).Execute();

            Logger.Info(this, $"Available options for '{poll}' changed", string.Join(", ", database.Database.Load<PollOption>(o => o.Key).Where(o => o.Poll == poll).ExecuteSet<string>()));
            OnOptionAdded(new PollOption
            {
                Poll = poll,
                Key = option,
                Description = description
            });
        }

        /// <summary>
        /// creates a new poll
        /// </summary>
        /// <param name="name">name of poll</param>
        /// <param name="description">poll description</param>
        /// <param name="options">poll options (optional)</param>
        public void CreatePoll(string name, string description, string options = null)
        {
            if (database.Database.Load<Poll>(c => DBFunction.Count).Where(p => p.Name == name).ExecuteScalar<long>() > 0)
            {
                database.Database.Delete<PollVote>().Where(p => p.Poll == name).Execute();
                database.Database.Delete<PollOption>().Where(p => p.Poll == name).Execute();
                return;
            }

            database.Database.Insert<Poll>().Columns(p => p.Name, p => p.Description).Values(name, description).Execute();
            if (options != null)
            {
                foreach (string option in options.Split(';'))
                {
                    string[] kvp = option.Split(',');
                    if (kvp.Length != 2)
                        continue;

                    database.Database.Insert<PollOption>().Columns(o => o.Poll, o => o.Key, o => o.Description).Values(name, kvp[0], kvp[1]).Execute();
                }
            }

            Logger.Info(this, $"Poll '{name}' created.", $"{description} - {options}");
            OnPollCreated(new Poll
            {
                Name = name,
                Description = description
            });
        }

        /// <summary>
        /// get all available options for a poll
        /// </summary>
        /// <param name="pollname">name of poll</param>
        /// <returns>options for a poll</returns>
        public PollOption[] GetOptions(string pollname)
        {
            return database.Database.LoadEntities<PollOption>().Where(p => p.Poll == pollname).Execute().ToArray();
        }

        public bool RevokeVote(string user, string poll) {
            return database.Database.Delete<PollVote>().Where(p => p.Poll == poll && p.User == user).Execute() > 0;
        }

        /// <summary>
        /// get a poll from database
        /// </summary>
        /// <param name="pollname">name of poll</param>
        /// <returns><see cref="Poll"/> object with poll information (not the options and votes)</returns>
        public Poll GetPoll(string pollname) {
            return database.Database.LoadEntities<Poll>().Where(p => p.Name == pollname).Execute().FirstOrDefault();
        }

        /// <summary>
        /// get votes of a poll
        /// </summary>
        /// <param name="pollname">name of poll</param>
        /// <returns>votes which were applied on a poll</returns>
        public PollVote[] GetVotes(string pollname)
        {
            return database.Database.LoadEntities<PollVote>().Where(p => p.Poll == pollname).Execute().ToArray();
        }

        public WeightedVote[] GetWeightedVotes(string pollname) {
            return database.Database.LoadEntities<WeightedVote>().Where(p => p.Poll == pollname).Execute().ToArray();
        }

        /// <summary>
        /// get all polls currently stored in database
        /// </summary>
        /// <returns>polls</returns>
        [Command("polls")]
        public Poll[] GetPolls()
        {
            return database.Database.LoadEntities<Poll>().Execute().ToArray();
        }
    
        /// <summary>
        /// revokes a vote of a user
        /// </summary>
        /// <param name="user">name of user</param>
        /// <param name="poll">poll where to revoke votes</param>
        [Command("revoke", "$user", 0)]
        public void ExecuteRevoke(string user, string poll) {
            database.Database.Delete<PollVote>().Where(p => p.Poll == poll && p.User == user).Execute();
            VoteRemoved?.Invoke(new PollVote {
                Poll = poll,
                User = user
            });
        }

        void LoadSettings()
        {
            Period = settings.Get(this, "period", 1200.0f);
            ShowPollAfterVote = settings.Get(this, "showpollaftervote", true);
        }

        public void LockOption(string poll, string option, bool locked)
        {
            Logger.Info(this, $"Option {option} of poll {poll} now {(locked ? "locked" : "unlocked")}");
            database.Database.Update<PollOption>().Set(o => o.Locked == locked).Where(p => p.Poll == poll && p.Key == option).Execute();
        }

        public void Process(double time) {
            Collection[] collectionitems = collections.GetCollections();
            if (collectionitems.Length > 0 && RNG.XORShift64.NextFloat() < 0.5f)
                ShowCollection(collectionitems.RandomItem(RNG.XORShift64));
            else ShowPoll();
        }
    }
}