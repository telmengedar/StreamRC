using System;

namespace StreamRC.Streaming.Events {

    /// <summary>
    /// event in stream
    /// </summary>
    public class StreamEvent {

        /// <summary>
        /// time when event happened
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// title for the event
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// message which represents the event (in message format)
        /// </summary>
        public string Message { get; set; } 
    }
}