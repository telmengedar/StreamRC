using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Notifications;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// module sending stream events to notification window
    /// </summary>
    [Dependency(nameof(StreamModule), DependencyType.Type)]
    [Dependency(nameof(NotificationModule), DependencyType.Type)]
    public class StreamNotificationModule : IInitializableModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="StreamNotificationModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StreamNotificationModule(Context context) {
            this.context = context;
        }

        public void Initialize() {
            StreamModule streammodule = context.GetModule<StreamModule>();

            NotificationModule notificationwindow = context.GetModule<NotificationModule>();

            streammodule.NewFollower += information => notificationwindow.ShowNotification(new Notification {
                Title = "New Follower",
                Content = new MessageBuilder()
                    .Text(information.Username, StreamColors.Option, FontWeight.Bold)
                    .Text(" started to follow.")
                    .BuildMessage()
            });

            streammodule.NewSubscriber += information => notificationwindow.ShowNotification(new Notification {
                Title = "New Subscriber",
                Content = new MessageBuilder()
                    .Text(information.Username, StreamColors.Option, FontWeight.Bold)
                    .Text(" subscribed with ")
                    .Text(information.PlanName, StreamColors.Option, FontWeight.Bold)
                    .Text(" to this channel.")
                    .BuildMessage()
            });

            streammodule.Hosted += information => notificationwindow.ShowNotification(new Notification {
                Title = "New Host",
                Content = information.Viewers > 0 ?
                    new MessageBuilder().Text(information.Channel, StreamColors.Option, FontWeight.Bold).Text(" is hosting this channel to ").Text(information.Viewers.ToString(), StreamColors.Option, FontWeight.Bold).Text(" viewers.").BuildMessage() :
                    new MessageBuilder().Text(information.Channel, StreamColors.Option, FontWeight.Bold).Text(" is hosting this channel.").BuildMessage()
            });
        }
    }
}