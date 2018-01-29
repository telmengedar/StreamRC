using System;

namespace StreamRC.Streaming.Notifications {
    public class NotificationHttpResponse {
        public Notification[] Notifications { get; set; }
        public DateTime Timestamp { get; set; }
    }
}