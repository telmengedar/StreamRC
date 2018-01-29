using System;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Events {
    public class StreamHttpEvent {

        /// <summary>
        /// time when event happened
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// title for the event
        /// </summary>
        public Message Title { get; set; }

        /// <summary>
        /// message which represents the event (in message format)
        /// </summary>
        public Message Message { get; set; }

    }
}