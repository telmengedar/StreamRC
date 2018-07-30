using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Notifications;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// module sending stream events to notification window
    /// </summary>
    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(NotificationModule))]
    public class StreamNotificationModule : IInitializableModule {
        readonly Context context;
        NotificationModule notificationwindow;
        UserModule usermodule;
        ImageCacheModule imagemodule;

        /// <summary>
        /// creates a new <see cref="StreamNotificationModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StreamNotificationModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            StreamModule streammodule = context.GetModule<StreamModule>();
            notificationwindow = context.GetModule<NotificationModule>();
            usermodule = context.GetModule<UserModule>();
            imagemodule = context.GetModule<ImageCacheModule>();

            streammodule.NewFollower += information => notificationwindow.ShowNotification(
                new MessageBuilder().Text("New Follower").BuildMessage(),
                new MessageBuilder()
                    .User(usermodule.GetExistingUser(information.Service, information.Username), u=>imagemodule.AddImage(u.Avatar))
                    .Text(" started to follow.")
                    .BuildMessage()
            );

            streammodule.NewSubscriber += information => notificationwindow.ShowNotification(
                new MessageBuilder().Text("New Subscriber").BuildMessage(),
                new MessageBuilder()
                    .User(usermodule.GetExistingUser(information.Service, information.Username), u => imagemodule.AddImage(u.Avatar))
                    .Text(" subscribed with ")
                    .Text(information.PlanName, StreamColors.Option, FontWeight.Bold)
                    .Text(" to this channel.")
                    .BuildMessage()
            );

            streammodule.Hosted += information => notificationwindow.ShowNotification(
                new MessageBuilder().Text("New Host").BuildMessage(),
                information.Viewers > 0 ?
                    new MessageBuilder().User(usermodule.GetExistingUser(information.Service, information.Channel), u => imagemodule.AddImage(u.Avatar)).Text(" is hosting this channel to ").Text(information.Viewers.ToString(), StreamColors.Option, FontWeight.Bold).Text(" viewers.").BuildMessage() :
                    new MessageBuilder().User(usermodule.GetExistingUser(information.Service, information.Channel), u => imagemodule.AddImage(u.Avatar)).Text(" is hosting this channel.").BuildMessage()
            );

            streammodule.Raid += OnRaid;
        }

        void OnRaid(RaidInformation raid) {
            User user = usermodule.GetExistingUser(raid.Service, raid.Login);
            notificationwindow.ShowNotification(
                new MessageBuilder().Text("New Raid").BuildMessage(),
                raid.RaiderCount > 0 ?
                    new MessageBuilder().User(user, imagemodule.AddImage(user.Avatar)).Text(" is raiding this channel with ").Text(raid.RaiderCount.ToString(), StreamColors.Option, FontWeight.Bold).Text(" raiders.").BuildMessage() :
                    new MessageBuilder().User(user, imagemodule.AddImage(user.Avatar)).Text(" is raiding this channel.").BuildMessage()
                );
        }
    }
}