using System;
using System.Linq;
using System.Text;
using NightlyCode.Core.ComponentModel;
using NightlyCode.Core.Conversion;
using NightlyCode.Core.Logs;
using NightlyCode.Core.Randoms;
using NightlyCode.DB.Entities.Operations;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Timer;
using StreamRC.Streaming.Collections;
using StreamRC.Streaming.Notifications;
using StreamRC.Streaming.Polls.Notifications;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Ticker;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// manages user polls for stream
    /// </summary>
    [ModuleKey("poll")]
    [Dependency(nameof(CollectionModule), DependencyType.Type)]
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(TickerModule), DependencyType.Type)]
    [Dependency(nameof(TimerModule), DependencyType.Type)]
    public class PollModule : IInitializableModule, ICommandModule, IStreamCommandHandler, IRunnableModule, ITimerService
    {
        readonly Context context;

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
        public PollModule(Context context)
        {
            this.context = context;
            tickergenerator = new PollTickerGenerator(context, this);
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
                context.GetModule<TickerModule>().AddSource(tickergenerator);
        }

        void OnOptionRemoved(PollOption option)
        {
            OptionRemoved?.Invoke(option);
            if (tickergenerator.PollOptionCount == 0)
                context.GetModule<TickerModule>().RemoveSource(tickergenerator);
        }

        /// <summary>
        /// determines whether a vote automatically shows the corresponding poll in stream
        /// </summary>
        public bool ShowPollAfterVote
        {
            get { return showPollAfterVote; }
            set
            {
                showPollAfterVote = value;
                context.Settings.Set(this, "showpollaftervote", value);
            }
        }

        /// <summary>
        /// time for next poll to show
        /// </summary>
        public float Period
        {
            get { return period; }
            set
            {
                period = value;
                context.Settings.Set(this, "period", value);
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
                Poll poll = context.Database.LoadEntities<Poll>().Where(p => p.Name == pollname).Execute().FirstOrDefault();
                PollShown?.Invoke(poll);
            }
        }

        /// <summary>
        /// clears a poll of all votes
        /// </summary>
        /// <param name="pollname"></param>
        public void ClearPoll(string pollname)
        {
            context.Database.Delete<PollVote>().Where(p => p.Poll == pollname).Execute();

            Logger.Info(this, $"'{pollname}' cleared.");
            PollCleared?.Invoke(new Poll {
                Name = pollname
            });
        }

        void ShowCollection(Collection collection = null)
        {
            if (collection == null)
                collection = context.GetModule<CollectionModule>().GetCollections().RandomItem(RNG.XORShift64);

            if (collection == null)
                return;

            WeightedCollectionItem[] items = context.GetModule<CollectionModule>().GetWeightedItems(collection.Name);
            // TODO: Show collection graph
        }

        void ReloadAvailablePolls()
        {
            lock (availablelock)
                availablepolls = context.Database.Load<ActivePoll>(p => p.Name).ExecuteSet<string>().ToArray();
        }

        void ExecuteVote(string poll, string user, string option) {
            if (context.Database.Update<PollVote>().Set(p => p.Vote == option).Where(p => p.Poll == poll && p.User == user).Execute() == 0)
                context.Database.Insert<PollVote>().Columns(p => p.Poll, p => p.User, p => p.Vote).Values(poll, user, option).Execute();

            if (ShowPollAfterVote)
                ShowPoll(poll);

            // if this is a vote for a new poll, include poll in polls to be shown
            if (availablepolls.All(p => p != poll))
                ReloadAvailablePolls();
        }

        PollOption[] FindOptions(string[] arguments) {
            string optionname = string.Join(" ", arguments.Select(a => a.ToLower()));
            PollOption[] options = context.Database.LoadEntities<PollOption>().Where(o => o.Description.ToLower() == optionname).Execute().ToArray();
            if (options.Length == 0)
                options = context.Database.LoadEntities<PollOption>().Where(o => o.Key == optionname).Execute().ToArray();
            return options;
        }

        void HeuristicVote(StreamCommand command) {
            Logger.Info(this, $"Executing heuristic vote for '{command}'");

            
            PollOption[] options = FindOptions(command.Arguments);

            string optionname = string.Join(" ", command.Arguments);
            if (options.Length > 1)
                throw new StreamCommandException($"Sadly there is more than one poll which contains an option '{optionname}' so you need to specify in which poll you want to vote ({string.Join(", ", options.Select(o => o.Poll))}).", false);

            if(options.Length == 0) {
                Poll poll = context.Database.LoadEntities<Poll>().Where(p => p.Name == optionname).Execute().FirstOrDefault();
                if(poll == null)
                    throw new StreamCommandException($"There is no poll and no option named '{optionname}' so i have no idea what you want to vote for.", false);

                options = GetOptions(poll.Name);
                throw new StreamCommandException($"You need to specify the option to vote for. The following options are available. {string.Join(", ", options.Select(o => $"'{o.Key}' for '{o.Description}'"))}", false);
            }

            ExecuteVote(options[0].Poll, command.User, options[0].Key);

            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, $"You voted successfully for '{options[0].Description}' in poll '{options[0].Poll}'.", command.IsWhispered);
            VoteAdded?.Invoke(new PollVote
            {
                Poll = options[0].Poll,
                User = command.User,
                Vote = options[0].Key
            });
        }

        void Vote(StreamCommand command)
        {
            if(command.Arguments.Length != 2) {
                HeuristicVote(command);
                return;
            }

            string poll = command.Arguments[0].ToLower();
            if(context.Database.Load<Poll>(DBFunction.Count).Where(p => p.Name == poll).ExecuteScalar<long>() == 0) {
                HeuristicVote(command);
                return;
            }

            string vote = command.Arguments[1].ToLower();

            if(context.Database.Load<PollOption>(DBFunction.Count).Where(o => o.Poll == poll).ExecuteScalar<long>() > 0 && context.Database.Load<PollOption>(DBFunction.Count).Where(o => o.Poll == poll && o.Key == vote).ExecuteScalar<long>() == 0) {
                HeuristicVote(command);
                return;
            }

            ExecuteVote(poll, command.User, vote);

            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, $"You voted successfully for {vote} in poll {poll}.", command.IsWhispered);
            VoteAdded?.Invoke(new PollVote
            {
                Poll = poll,
                User = command.User,
                Vote = vote
            });
        }

        void IInitializableModule.Initialize()
        {
            context.Database.UpdateSchema<Poll>();
            context.Database.UpdateSchema<PollVote>();
            context.Database.UpdateSchema<PollOption>();
            context.Database.UpdateSchema<WeightedVote>();
            context.Database.UpdateSchema<ActivePoll>();

            LoadSettings();

            notificationgenerator = new PollNotificationGenerator(this, context.GetModule<NotificationModule>());
            ReloadAvailablePolls();

            if (tickergenerator.PollOptionCount > 0)
                context.GetModule<TickerModule>().AddSource(tickergenerator);

            context.GetModule<StreamModule>().RegisterCommandHandler(this, "vote", "revoke", "pollresult", "polls", "pollinfo");
            context.GetModule<CollectionModule>().ItemAdded += OnCollectionItemAdded;
            context.GetModule<CollectionModule>().ItemRemoved += OnCollectionItemRemoved;
            context.GetModule<CollectionModule>().ItemBlocked += OnCollectionItemBlocked;
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


        void GetPollResult(StreamCommand command)
        {
            if(command.Arguments.Length == 0) {
                Logger.Info(this, $"Starting heuristic poll result estimation for '{command.User}'");
                ActivePoll leadingpoll = context.Database.LoadEntities<ActivePoll>().OrderBy(new OrderByCriteria(EntityField.Create<ActivePoll>(p => p.Votes), false)).Execute().FirstOrDefault();
                if(leadingpoll == null) {
                    context.GetModule<StreamModule>().SendMessage(command.Service, command.User, "Since no one voted for anything i can't show you any poll.", command.IsWhispered);
                }
                else {
                    context.GetModule<StreamModule>().SendMessage(command.Service, command.User, $"You seem to be too lazy to tell me which poll you want to know something about. I just guess you want to see poll '{leadingpoll.Name}' since it is the most active poll.", command.IsWhispered);
                    GetPollResult(new StreamCommand {
                        User = command.User,
                        Service = command.Service,
                        Command = command.Command,
                        IsWhispered = command.IsWhispered,
                        Arguments = new[] {
                            leadingpoll.Name
                        }
                    });
                }
                return;
            }

            string pollkey = command.Arguments[0];
            Poll poll = context.Database.LoadEntities<Poll>().Where(p => p.Name == pollkey).Execute().FirstOrDefault();

            if (poll == null)
            {
                context.GetModule<StreamModule>().SendMessage(command.Service, command.User, $"There is no poll named '{pollkey}'", command.IsWhispered);
                return;
            }

            PollDiagramData data = new PollDiagramData(context.Database.LoadEntities<WeightedVote>().Where(p => p.Poll == pollkey).Execute());

            string message = $"Results for {pollkey}: {string.Join(", ", data.GetItems(100).Where(r => r.Count > 0).Select(r => $"{r.Item} [{r.Count}]"))}";
            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, message, command.IsWhispered);
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
                    ShowCollection(context.GetModule<CollectionModule>().GetCollection(arguments[0].ToLower()));
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
            context.Database.Delete<PollVote>().Where(p => p.Poll == poll && p.Vote == option).Execute();
            Logger.Info(this, $"Option '{option}' of poll '{poll}' was reset.");
            OptionReset?.Invoke(new PollOption() {
                Poll = poll,
                Key = option
            });
        }

        void RemoveVote(string poll, string user)
        {
            context.Database.Delete<PollVote>().Where(v => v.Poll == poll && v.User == user).Execute();
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
            context.Database.Delete<Poll>().Where(p => p.Name == poll).Execute();
            context.Database.Delete<PollVote>().Where(p => p.Poll == poll).Execute();
            context.Database.Delete<PollOption>().Where(p => p.Poll == poll).Execute();
            ReloadAvailablePolls();

            Logger.Info(this, $"Poll '{poll}' removed.");
            OnPollRemoved(new Poll
            {
                Name = poll
            });
        }

        public void ChangePoll(string oldname, string newname, string description)
        {
            context.Database.Update<Poll>().Set(p => p.Name == newname, p => p.Description == description).Where(p => p.Name == oldname).Execute();

            if (oldname != newname)
            {
                context.Database.Update<PollOption>().Set(o => o.Poll == newname).Where(o => o.Poll == oldname).Execute();
                context.Database.Update<PollVote>().Set(v => v.Poll == newname).Where(v => v.Poll == oldname).Execute();
            }

            Logger.Info(this, $"Poll '{oldname}' changed.", $"{newname} - {description}");
        }

        public void ChangePollOption(string poll, string oldname, string newname, string description)
        {
            context.Database.Update<PollOption>().Set(p => p.Key == newname, p => p.Description == description).Where(p => p.Poll == poll && p.Key == oldname).Execute();

            if (oldname != newname)
                context.Database.Update<PollVote>().Set(p => p.Vote == newname).Where(p => p.Poll == poll && p.Vote == oldname).Execute();

            Logger.Info(this, $"Polloption '{oldname}' of '{poll}' changed.", $"{newname}, {description}");
        }

        void ChangeDescription(string poll, string description)
        {
            context.Database.Update<Poll>().Set(p => p.Description == description).Where(p => p.Name == poll).Execute();
        }

        public void RemovePollOption(string poll, string option)
        {
            context.Database.Delete<PollOption>().Where(o => o.Poll == poll && o.Key == option).Execute();
            context.Database.Delete<PollVote>().Where(o => o.Poll == poll && o.Vote == option).Execute();
            Logger.Info(this, $"Available options for '{poll}' changed", string.Join(", ", context.Database.Load<PollOption>(o => o.Key).Where(o => o.Poll == poll).ExecuteSet<string>()));
            ReloadAvailablePolls();
            OnOptionRemoved(new PollOption
            {
                Poll = poll,
                Key = option
            });
        }

        public void CreatePollOption(string poll, string option, string description)
        {
            if (context.Database.Load<Poll>(DBFunction.Count).Where(p => p.Name == poll).ExecuteScalar<long>() == 0)
                throw new Exception($"No poll named '{poll}' found");

            if (context.Database.Update<PollOption>().Set(o => o.Description == description).Where(o => o.Poll == poll && o.Key == option).Execute() == 0)
                context.Database.Insert<PollOption>().Columns(o => o.Poll, o => o.Key, o => o.Description).Values(poll, option, description).Execute();

            Logger.Info(this, $"Available options for '{poll}' changed", string.Join(", ", context.Database.Load<PollOption>(o => o.Key).Where(o => o.Poll == poll).ExecuteSet<string>()));
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
            if (context.Database.Load<Poll>(DBFunction.Count).Where(p => p.Name == name).ExecuteScalar<long>() > 0)
            {
                context.Database.Delete<PollVote>().Where(p => p.Poll == name).Execute();
                context.Database.Delete<PollOption>().Where(p => p.Poll == name).Execute();
                return;
            }

            context.Database.Insert<Poll>().Columns(p => p.Name, p => p.Description).Values(name, description).Execute();
            if (options != null)
            {
                foreach (string option in options.Split(';'))
                {
                    string[] kvp = option.Split(',');
                    if (kvp.Length != 2)
                        continue;

                    context.Database.Insert<PollOption>().Columns(o => o.Poll, o => o.Key, o => o.Description).Values(name, kvp[0], kvp[1]).Execute();
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
            return context.Database.LoadEntities<PollOption>().Where(p => p.Poll == pollname).Execute().ToArray();
        }

        public PollVote[] GetVotes(string pollname)
        {
            return context.Database.LoadEntities<PollVote>().Where(p => p.Poll == pollname).Execute().ToArray();
        }

        public WeightedVote[] GetWeightedVotes(string pollname) {
            return context.Database.LoadEntities<WeightedVote>().Where(p => p.Poll == pollname).Execute().ToArray();
        }

        /// <summary>
        /// get all polls currently stored in database
        /// </summary>
        /// <returns>polls</returns>
        public Poll[] GetPolls()
        {
            return context.Database.LoadEntities<Poll>().Execute().ToArray();
        }

        void IStreamCommandHandler.ProcessStreamCommand(StreamCommand command)
        {
            switch (command.Command)
            {
                case "myvote":
                    DisplayVote(command);
                    break;
                case "vote":
                    Vote(command);
                    break;
                case "revoke":
                    Revoke(command);
                    break;
                case "pollresult":
                    GetPollResult(command);
                    break;
                case "polls":
                    GetPollList(command);
                    break;
                case "pollinfo":
                    GetPollInfo(command);
                    break;
                default:
                    throw new StreamCommandException("Command not supported by this module");
            }
        }

        void DisplayVote(StreamCommand command) {
            PollVote[] votes;
            if(command.Arguments.Length > 0)
                votes = context.Database.LoadEntities<PollVote>().Where(v => v.User == command.User && v.Poll == command.Arguments[0]).Execute().ToArray();
            else votes = context.Database.LoadEntities<PollVote>().Where(v => v.User == command.User).Execute().ToArray();

            if(votes.Length == 0)
                context.GetModule<StreamModule>().SendMessage(command, $"You didn't vote for anything{(command.Arguments.Length > 0 ? $" in poll {command.Arguments[0]}" : "")}");
            else context.GetModule<StreamModule>().SendMessage(command, $"You voted for: {string.Join(", ", votes.Select(v => $"'{v.Vote}' in poll '{v.Poll}'"))}");
        }

        void ExecuteRevoke(string poll, string user) {
            context.Database.Delete<PollVote>().Where(p => p.Poll == poll && p.User == user).Execute();
            VoteRemoved?.Invoke(new PollVote {
                Poll = poll,
                User = user
            });
        }

        void Revoke(StreamCommand command) {
            PollVote[] votes;
            if (command.Arguments.Length == 0 || (command.Arguments.Length==1&&command.Arguments[0]=="all")) {
                votes = context.Database.LoadEntities<PollVote>().Where(o => o.User == command.User).Execute().ToArray();
                if(votes.Length == 0)
                    throw new StreamCommandException("You haven't voted for anything, so revoking a vote doesn't make any sense.", false);

                if(votes.Length > 1) {
                    if(command.Arguments.Length == 1 && command.Arguments[0] == "all") {
                        foreach(PollVote vote in votes)
                            ExecuteRevoke(vote.Poll, command.User);
                        context.GetModule<StreamModule>().SendMessage(command, $"You revoked your votes in polls '{string.Join(", ", votes.Select(v => v.Poll))}'");
                        return;
                    }
                    throw new StreamCommandException($"You have voted in more than one poll. Type !revoke all to remove all your votes. You voted in the following polls: {string.Join(", ", votes.Select(v => v.Poll))}");
                }

                ExecuteRevoke(votes[0].Poll, command.User);
                context.GetModule<StreamModule>().SendMessage(command, $"You revoked your vote in poll '{votes[0].Poll}'");
                return;
            }

            string poll = command.Arguments[0].ToLower();
            if(context.Database.Delete<PollVote>().Where(p => p.Poll == poll && p.User == command.User).Execute() > 0) {
                context.GetModule<StreamModule>().SendMessage(command, $"You revoked your vote in poll '{poll}'");
                return;
            }

            PollOption[] options = FindOptions(command.Arguments);            
            string[] keys = options.Select(o => o.Key).ToArray();
            votes = context.Database.LoadEntities<PollVote>().Where(v => keys.Contains(v.Vote)).Execute().ToArray();

            if(votes.Length == 0) {
                context.GetModule<StreamModule>().SendMessage(command, "No votes match your arguments so no clue what you want to revoke.");
                return;
            }

            foreach (PollVote vote in votes)
                ExecuteRevoke(vote.Poll, command.User);
            context.GetModule<StreamModule>().SendMessage(command, $"You revoked your votes in polls '{string.Join(", ", votes.Select(v => v.Poll))}'");
        }

        void GetPollInfo(StreamCommand command)
        {
            if (command.Arguments.Length != 1)
                throw new StreamCommandException("Invalid command syntax");

            string pollname = command.Arguments[0];

            Poll poll = context.Database.LoadEntities<Poll>().Where(p => p.Name == pollname).Execute().FirstOrDefault();
            if (poll == null)
                throw new StreamCommandException($"There is no active poll named '{pollname}'");

            PollOption[] options = GetOptions(pollname).Where(o => !o.Locked).ToArray();
            StringBuilder message = new StringBuilder(poll.Description).Append(": ");
            if (options.Length == 0)
            {
                message.Append("This is an unrestricted poll, so please vote for 'penis' when you're out of ideas");
            }
            else
            {
                message.Append(string.Join(", ", options.Select(o => $"{o.Key} - {o.Description}")));
                message.Append(". Usually there is more info available by typing !info <option>");
            }

            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, message.ToString(), command.IsWhispered);
        }

        void GetPollList(StreamCommand command)
        {
            string polllist = string.Join(", ", context.Database.Load<Poll>(p => p.Name).ExecuteSet<string>());
            context.GetModule<StreamModule>().SendMessage(command.Service, command.User, $"Currently running polls: {polllist}");
        }

        string IStreamCommandHandler.ProvideHelp(string command)
        {
            switch (command)
            {
                case "myvote":
                    return "Displays the options for which you have voted in chat. Syntax: !myvote [poll]";
                case "vote":
                    return "Registers a vote for a poll. Syntax: !vote <poll> <option>";
                case "revoke":
                    return "Removes a vote from a poll. Syntax: !revoke <poll>";
                case "pollresult":
                    return "Returns the results for a poll in chat. Syntax: !pollresult <poll>";
                case "polls":
                    return "Returns a list of currently running polls. Syntax: !polls";
                case "pollinfo":
                    return "Returns info about an active poll. Syntax: !pollinfo <poll>";
                default:
                    throw new StreamCommandException("Command not supported by this module");
            }
        }

        void LoadSettings()
        {
            Period = context.Settings.Get(this, "period", 1200.0f);
            ShowPollAfterVote = context.Settings.Get(this, "showpollaftervote", true);
        }

        public void LockOption(string poll, string option, bool locked)
        {
            Logger.Info(this, $"Option {option} of poll {poll} now {(locked ? "locked" : "unlocked")}");
            context.Database.Update<PollOption>().Set(o => o.Locked == locked).Where(p => p.Poll == poll && p.Key == option).Execute();
        }

        public void Start() {
            context.GetModule<TimerModule>().AddService(this, Period);
        }

        public void Stop() {
            context.GetModule<TimerModule>().RemoveService(this);
        }

        public void Process(double time) {
            Collection[] collections = context.GetModule<CollectionModule>().GetCollections();
            if (collections.Length > 0 && RNG.XORShift64.NextFloat() < 0.5f)
                ShowCollection(collections.RandomItem(RNG.XORShift64));
            else ShowPoll();
        }
    }
}