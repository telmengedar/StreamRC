﻿using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Notifications {

    /// <summary>
    /// notification data
    /// </summary>
    public class Notification {

        /// <summary>
        /// notification title
        /// </summary>
        public Message Title { get; set; }

        /// <summary>
        /// notification content
        /// </summary>
        public Message Text { get; set; } 
    }
}