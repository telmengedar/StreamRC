using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Ticker {

    /// <summary>
    /// a message to be displayed in ticker form
    /// </summary>
    public class TickerMessage {

        /// <summary>
        /// message content
        /// </summary>
        public Message Content { get; set; } 
    }
}