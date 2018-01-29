using System;
using StreamRC.Core.Messages;

namespace StreamRC.Streaming.Chat {

    /// <summary>
    /// message in chat for http service
    /// </summary>
    public class ChatHttpMessage {

        /// <summary>
        /// chunks which build up the message
        /// </summary>
        public MessageChunk[] Content { get; set; }

        /// <summary>
        /// timestamp of this message
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}