using System;
using System.Collections.Generic;

namespace StreamRC.Streaming.Stream.Chat {

    /// <summary>
    /// interface for a chat channel
    /// </summary>
    public interface IChatChannel {

        /// <summary>
        /// user has joined channel
        /// </summary>
        event Action<IChatChannel, UserInformation> UserJoined;

        /// <summary>
        /// user has left channel
        /// </summary>
        event Action<IChatChannel, UserInformation> UserLeft;

        /// <summary>
        /// a message was received
        /// </summary>
        event Action<IChatChannel, ChatMessage> ChatMessage;

        /// <summary>
        /// name of service where channel is connected to
        /// </summary>
        string Service { get; }

        /// <summary>
        /// name of chat channel
        /// </summary>
        string Name { get; }

        /// <summary>
        /// channel flags
        /// </summary>
        ChannelFlags Flags { get; }

        /// <summary>
        /// users currently in chat
        /// </summary>
        IEnumerable<string> Users { get; }

        /// <summary>
        /// sends a message to the channel
        /// </summary>
        /// <param name="message">message to send</param>
        void SendMessage(string message);

        /// <summary>
        /// initializes the channel after it was added
        /// </summary>
        void Initialize();
    }
}