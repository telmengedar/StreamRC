using System;
using NightlyCode.Database.Entities.Attributes;

namespace StreamRC.Streaming.Events {

    /// <summary>
    /// event in stream
    /// </summary>
    public class StreamEvent {

        /// <summary>
        /// time when event happened
        /// </summary>
        [Index("time")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// type of event
        /// </summary>
        [DefaultValue(0)]
        [Index("type")]
        public StreamEventType Type { get; set; }

        /// <summary>
        /// user if applicable
        /// </summary>
        [DefaultValue(0)]
        public long UserID { get; set; }

        /// <summary>
        /// value for certain stream event types
        /// </summary>
        [DefaultValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// factor with which to multiplicate value
        /// </summary>
        [DefaultValue(1)]
        public double Multiplicator { get; set; }

        /// <summary>
        /// secondary argument for event content
        /// </summary>
        public string Argument { get; set; }

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