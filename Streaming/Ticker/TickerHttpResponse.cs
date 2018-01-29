using System;
using StreamRC.Streaming.Notifications;

namespace StreamRC.Streaming.Ticker {
    public class TickerHttpResponse {
        public TickerMessage[] Messages { get; set; }
        public DateTime Timestamp { get; set; }
    }
}