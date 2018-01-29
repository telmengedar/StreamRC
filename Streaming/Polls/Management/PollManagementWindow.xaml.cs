using System;
using System.Linq;
using System.Windows;
using NightlyCode.Core.Collections;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Streaming.Polls.Management
{
    /// <summary>
    /// Interaction logic for PollManagementWindow.xaml
    /// </summary>
    [Dependency(ModuleKeys.MainWindow, DependencyType.Key)]
    public partial class PollManagementWindow : Window, IRunnableModule {
        readonly Context context;

        readonly NotificationList<PollEditor> polls=new NotificationList<PollEditor>();
        readonly NotificationList<PollOptionEditor> options=new NotificationList<PollOptionEditor>();
        readonly NotificationList<PollVote> votes=new NotificationList<PollVote>();

        string selectedpoll;

        /// <summary>
        /// creates a new <see cref="PollManagementWindow"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public PollManagementWindow(Context context) {
            this.context = context;
            InitializeComponent();

            polls.ItemChanged += OnEditCollectionItemChanged;
            options.ItemChanged += OnEditOptionsChanged;

            grdPolls.ItemsSource = polls;
            grdPollOptions.ItemsSource = options;
            grdItems.ItemsSource = votes;

            Closing += (sender, args) => {
                Visibility = Visibility.Hidden;
                args.Cancel = true;
            };
        }

        void OnPollCleared(Poll poll) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(poll.Name != selectedpoll)
                    return;

                votes.Clear();
            }));
        }

        void OnEditOptionsChanged(PollOptionEditor option, string property) {
            switch (property)
            {
                case "Key":
                    if(string.IsNullOrEmpty(option.OldKey)) {
                        option.Poll = selectedpoll;
                        context.GetModule<PollModule>().CreatePollOption(option.Poll, option.Key, option.Description);
                    }
                    else context.GetModule<PollModule>().ChangePollOption(option.Poll, option.OldKey, option.Key, option.Description);
                    votes.Where(v => v.Vote == option.OldKey).ToArray().Foreach(v => v.Vote = option.Key);
                    break;
                case "Locked":
                    context.GetModule<PollModule>().LockOption(option.Poll, option.Key, option.Locked);
                    break;
                default:
                    context.GetModule<PollModule>().ChangePollOption(option.Poll, option.OldKey, option.Key, option.Description);
                    break;
            }
            option.Apply();
        }

        void OnEditCollectionItemChanged(PollEditor poll, string property) {
            switch(property) {
                case "Name":
                    if(string.IsNullOrEmpty(poll.OldName))
                        context.GetModule<PollModule>().CreatePoll(poll.Name, poll.Description);
                    else context.GetModule<PollModule>().ChangePoll(poll.OldName, poll.Name, poll.Description);
                    ReloadPollData(poll);                    
                    break;
                default:
                    context.GetModule<PollModule>().ChangePoll(poll.OldName, poll.Name, poll.Description);
                    break;
            }
            poll.Apply();
        }

        void OnOptionRemoved(PollOption option) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(option.Poll != selectedpoll)
                    return;

                options.RemoveWhere(i => i.Key == option.Key);
            }));
        }

        void OnOptionAdded(PollOption option) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(option.Poll != selectedpoll || options.Any(i=>i.Key==option.Key))
                    return;

                options.RemoveWhere(i => i.Key == option.Key);
                options.Add(new PollOptionEditor(option));
            }));
        }

        void OnVoteRemoved(PollVote vote) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(vote.Poll != selectedpoll)
                    return;

                votes.RemoveWhere(i => i.User == vote.User && i.Vote == vote.Vote);
            }));
        }

        void OnVoteAdded(PollVote vote) {
            Dispatcher.BeginInvoke((Action)(() => {
                if(vote.Poll != selectedpoll || votes.Any(i => i.User == vote.User && i.Vote == vote.Vote))
                    return;

                votes.Add(vote);
            }));
        }

        void OnPollRemoved(Poll poll) {
            Dispatcher.BeginInvoke((Action)(() => {
                polls.RemoveWhere(c => c.Name == poll.Name);
            }));
        }

        void OnPollAdded(Poll poll) {
            Dispatcher.BeginInvoke((Action)(() => {
                if (polls.Any(c => c.Name == poll.Name))
                    return;

                polls.Add(new PollEditor(poll));
            }));
        }

        void grdPolls_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Poll poll = grdPolls.SelectedItem as Poll;
            ReloadPollData(poll);
        }

        void ReloadPollData(Poll poll) {
            selectedpoll = null;

            votes.Clear();
            options.Clear();
            if(poll == null) {
                ctxRemovePoll.IsEnabled = false;
                ctxClearPoll.IsEnabled = false;
                return;
            }

            selectedpoll = poll.Name;
            foreach (PollVote vote in context.GetModule<PollModule>().GetVotes(poll.Name))
                votes.Add(vote);
            foreach (PollOption option in context.GetModule<PollModule>().GetOptions(poll.Name))
                options.Add(new PollOptionEditor(option));

            ctxRemovePoll.IsEnabled = true;
            ctxClearPoll.IsEnabled = true;
        }

        void Context_RemoveOption(object sender, RoutedEventArgs e) {
            PollOptionEditor option = grdPollOptions.SelectedItem as PollOptionEditor;
            if (option == null)
                return;

            context.GetModule<PollModule>().RemovePollOption(option.Poll, option.Key);
        }

        void Context_RemovePoll(object sender, RoutedEventArgs e) {
            PollEditor poll=grdPolls.SelectedItem as PollEditor;
            if(poll == null)
                return;

            context.GetModule<PollModule>().RemovePoll(poll.Name);
        }

        void grdPollOptions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            PollOptionEditor item = grdPollOptions.SelectedItem as PollOptionEditor;
            ctxRemoveOption.IsEnabled = item != null;
        }

        void Context_ClearPoll(object sender, RoutedEventArgs e) {
            PollEditor poll = grdPolls.SelectedItem as PollEditor;
            if (poll == null)
                return;

            context.GetModule<PollModule>().ClearPoll(poll.Name);
        }

        void Context_ClearCollection(object sender, RoutedEventArgs e) {
            PollEditor poll = grdPolls.SelectedItem as PollEditor;
            if(poll != null)
                context.GetModule<PollModule>().ClearPoll(poll.Name);
        }

        public void Start() {
            PollModule module = context.GetModule<PollModule>();

            module.PollCreated += OnPollAdded;
            module.PollRemoved += OnPollRemoved;
            module.PollCleared += OnPollCleared;
            module.OptionAdded += OnOptionAdded;
            module.OptionRemoved += OnOptionRemoved;

            module.VoteAdded += OnVoteAdded;
            module.VoteRemoved += OnVoteRemoved;

            polls.Clear();
            foreach (Poll poll in module.GetPolls())
                polls.Add(new PollEditor(poll));

            context.GetModuleByKey<IMainWindow>(ModuleKeys.MainWindow).AddMenuItem("Manage.Polls", (sender, args) => Show());
        }

        public void Stop() {
            PollModule module = context.GetModule<PollModule>();

            module.PollCreated -= OnPollAdded;
            module.PollRemoved -= OnPollRemoved;
            module.PollCleared -= OnPollCleared;
            module.OptionAdded -= OnOptionAdded;
            module.OptionRemoved -= OnOptionRemoved;

            module.VoteAdded -= OnVoteAdded;
            module.VoteRemoved -= OnVoteRemoved;
        }
    }
}
