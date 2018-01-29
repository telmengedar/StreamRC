using System;

namespace StreamRC.Streaming.Ticker {
    public class TickerHttpMessage {

        public TickerMessage Message { get; set; }

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// time for message until it is removed from buffer
        /// </summary>
        public double Decay { get; set; }

    }
}