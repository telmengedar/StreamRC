using StreamRC.Core.Messages;
using StreamRC.Streaming.Notifications;

namespace StreamRC.Streaming.Polls.Notifications {

    /// <summary>
    /// notification generator for <see cref="PollModule"/>
    /// </summary>
    public class PollNotificationGenerator {
        readonly NotificationModule notifications;

        /// <summary>
        /// creates a new <see cref="PollNotificationGenerator"/>
        /// </summary>
        /// <param name="module">access to poll module</param>
        /// <param name="notifications">access to notification module</param>
        public PollNotificationGenerator(PollModule module, NotificationModule notifications) {
            this.notifications = notifications;

            module.PollCreated += OnPollCreated;
            module.PollRemoved += OnPollRemoved;
            module.PollCleared += OnPollCleared;
            module.OptionAdded += OnOptionAdded;
            module.OptionRemoved += OnOptionRemoved;
            module.OptionReset += OnOptionReset;
        }

        void OnOptionRemoved(PollOption option) {
            notifications.ShowNotification(new Notification {
                Title = "Poll Option Removed",
                Content = new MessageBuilder()
                    .Bold().Color(StreamColors.Option).Text(option.Key).Reset()
                    .Text(" was removed from poll ")
                    .Bold().Color(StreamColors.Option).Text(option.Poll).Reset()
                    .BuildMessage()
            });
        }

        void OnOptionReset(PollOption option) {
            notifications.ShowNotification(new Notification() {
                Title = "Poll Option Reset",
                Content = new MessageBuilder()
                    .Bold().Color(StreamColors.Option).Text(option.Key).Reset()
                    .Text(" of poll ")
                    .Bold().Color(StreamColors.Option).Text(option.Poll).Reset()
                    .Text(" was reset.")
                    .BuildMessage()
            });
        }

        void OnOptionAdded(PollOption option) {
            notifications.ShowNotification(new Notification {
                Title = "Poll Option Added",
                Content = new MessageBuilder()
                    .Bold().Color(StreamColors.Game).Text(option.Description).Reset()
                    .Text(" with key ")
                    .Bold().Color(StreamColors.Option).Text(option.Key).Reset()
                    .Text(" was added to poll ")
                    .Bold().Color(StreamColors.Option).Text(option.Poll).Reset()
                    .BuildMessage()
            });
        }

        void OnPollCleared(Poll poll) {
            notifications.ShowNotification(new Notification
            {
                Title = "Poll Cleared",
                Content = new MessageBuilder()
                    .Bold().Color(StreamColors.Option).Text(poll.Name).Reset()
                    .Text(" was cleared.")
                    .BuildMessage()
            });
        }

        void OnPollRemoved(Poll poll) {
            notifications.ShowNotification(new Notification
            {
                Title = "Poll Removed",
                Content = new MessageBuilder()
                    .Bold().Color(StreamColors.Option).Text(poll.Name).Reset()
                    .Text(" was removed.")
                    .BuildMessage()
            });
        }

        void OnPollCreated(Poll poll) {
            notifications.ShowNotification(new Notification
            {
                Title = "New Poll",
                Content = new MessageBuilder()
                    .Bold().Color(StreamColors.Game).Text(poll.Description).Reset()
                    .Text(" was created with key ")
                    .Bold().Color(StreamColors.Option).Text(poll.Name).Reset()
                    .BuildMessage()
            });
        }
    }
}