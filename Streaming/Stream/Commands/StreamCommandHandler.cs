using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// base implementation for <see cref="IStreamCommandHandler"/>
    /// </summary>
    public abstract class StreamCommandHandler : IStreamCommandHandler {

        /// <inheritdoc />
        public abstract void ExecuteCommand(IChatChannel channel, StreamCommand command);

        /// <inheritdoc />
        public abstract ChannelFlags RequiredFlags { get; }

        /// <summary>
        /// sends a message to a user
        /// </summary>
        /// <param name="channel">channel to send message to</param>
        /// <param name="user">user to send message to</param>
        /// <param name="message">message to send</param>
        protected void SendMessage(IChatChannel channel, string user, string message)
        {
            channel.SendMessage($"@{user}: {message}");
        }

    }
}