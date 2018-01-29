using System;

namespace StreamRC.Streaming.Notifications {
    public class NotificationHttpMessage {

        public Notification Notification { get; set; }

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// time for message until it is removed from buffer
        /// </summary>
        public double Decay { get; set; }

    }
}