using NightlyCode.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Cache;
using StreamRC.Streaming.Notifications;
using StreamRC.Streaming.Users;

namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// module sending stream events to notification window
    /// </summary>
    [Module(AutoCreate = true)]
    public class StreamNotificationModule {
        readonly NotificationModule notifications;
        readonly UserModule usermodule;
        readonly ImageCacheModule imagemodule;

        /// <summary>
        /// creates a new <see cref="StreamNotificationModule"/>
        /// </summary>
        /// <param name="context">access to module context</param>
        public StreamNotificationModule(StreamModule stream, NotificationModule notifications, ImageCacheModule imagemodule, UserModule usermodule) {
            this.notifications = notifications;
            this.imagemodule = imagemodule;
            this.usermodule = usermodule;

            stream.NewFollower += information => notifications.ShowNotification(
                new MessageBuilder().Text("New Follower").BuildMessage(),
                new MessageBuilder()
                    .User(usermodule.GetExistingUser(information.Service, information.Username), u => imagemodule.GetImageByUrl(u.Avatar))
                    .Text(" started to follow.")
                    .BuildMessage()
            );

            stream.NewSubscriber += information => notifications.ShowNotification(
                new MessageBuilder().Text("New Subscriber").BuildMessage(),
                new MessageBuilder()
                    .User(usermodule.GetExistingUser(information.Service, information.Username), u => imagemodule.GetImageByUrl(u.Avatar))
                    .Text(" subscribed with ")
                    .Text(information.PlanName, StreamColors.Option, FontWeight.Bold)
                    .Text(" to this channel.")
                    .BuildMessage()
            );

            stream.Hosted += information => notifications.ShowNotification(
                new MessageBuilder().Text("New Host").BuildMessage(),
                information.Viewers > 0 ?
                    new MessageBuilder().User(usermodule.GetExistingUser(information.Service, information.Channel), u => imagemodule.GetImageByUrl(u.Avatar)).Text(" is hosting this channel to ").Text(information.Viewers.ToString(), StreamColors.Option, FontWeight.Bold).Text(" viewers.").BuildMessage() :
                    new MessageBuilder().User(usermodule.GetExistingUser(information.Service, information.Channel), u => imagemodule.GetImageByUrl(u.Avatar)).Text(" is hosting this channel.").BuildMessage()
            );

            stream.Raid += OnRaid;
        }

        void OnRaid(RaidInformation raid) {
            User user = usermodule.GetExistingUser(raid.Service, raid.Login);
            notifications.ShowNotification(
                new MessageBuilder().Text("New Raid").BuildMessage(),
                raid.RaiderCount > 0 ?
                    new MessageBuilder().User(user, imagemodule.GetImageByUrl(user.Avatar)).Text(" is raiding this channel with ").Text(raid.RaiderCount.ToString(), StreamColors.Option, FontWeight.Bold).Text(" raiders.").BuildMessage() :
                    new MessageBuilder().User(user, imagemodule.GetImageByUrl(user.Avatar)).Text(" is raiding this channel.").BuildMessage()
                );
        }
    }
}