using System;

namespace StreamRC.Streaming.Stream.Chat {

    /// <summary>
    /// interface for a chat channel controlled by a bot
    /// </summary>
    public interface IBotChatChannel : IChatChannel {

        /// <summary>
        /// a command was received
        /// </summary>
        event Action<IBotChatChannel, StreamCommand> CommandReceived;
    }
}