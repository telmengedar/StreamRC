using System;
using NightlyCode.Modules;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Notifications {

    /// <summary>
    /// module routing notifications
    /// </summary>
    [Module(Key ="notifications")]
    public class NotificationModule {

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

        /// <summary>
        /// shows a notification
        /// </summary>
        /// <param name="title">notification title</param>
        /// <param name="text">notification text</param>
        public void ShowNotification(Message title, Message text) {
            ShowNotification(new Notification {
                Title = title,
                Text = text
            });
        }

        /// <summary>
        /// shows a notification
        /// </summary>
        /// <param name="title">notification title</param>
        /// <param name="text">notification text</param>
        /// <param name="formatted">whether strings are in message format</param>
        public void ShowNotification(string title, string text, bool formatted=false) {
            if(formatted) {
                ShowNotification(Message.Parse(title), Message.Parse(text));
            }
            else {
                ShowNotification(
                    new Message(new[] {new MessageChunk(MessageChunkType.Text, title)}),
                    new Message(new[] {new MessageChunk(MessageChunkType.Text, text)})
                    );
            }
        }
    }
}