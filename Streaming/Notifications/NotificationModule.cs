using System;
using System.Linq;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Stream;

namespace StreamRC.Streaming.Notifications {

    /// <summary>
    /// module routing notifications
    /// </summary>
    [ModuleKey("notifications")]
    public class NotificationModule : IModule, ICommandModule {
        readonly Context context;

        /// <summary>
        /// creates a new <see cref="NotificationModule"/>
        /// </summary>
        /// <param name="context"></param>
        public NotificationModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// triggered when a notification was created
        /// </summary>
        public event Action<Notification> Notification;

        /// <summary>
        /// triggers a notification event
        /// </summary>
        /// <param name="notification">notification to show</param>
        public void ShowNotification(Notification notification) {
            Notification?.Invoke(notification);
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "show":
                    ShowNotification(arguments);
                    break;
                default:
                    throw new StreamCommandException($"Command '{command}' not supported by this module.");
            }
        }

        void ShowNotification(string[] arguments) {
            ShowNotification(new Notification {
                Title = arguments[0],
                Content = Message.Parse(arguments[1])
            });
        }
    }
}