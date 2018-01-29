using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Notifications {

    /// <summary>
    /// notification data
    /// </summary>
    public class Notification {

        /// <summary>
        /// notification title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// notification content
        /// </summary>
        public Message Content { get; set; } 
    }
}