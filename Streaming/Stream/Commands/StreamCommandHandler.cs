using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// base implementation for a stream command handler
    /// </summary>
    public abstract class StreamCommandHandler : IStreamCommandHandler {

        /// <summary>
        /// executes a <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which command was received</param>
        /// <param name="command">command to execute</param>
        public abstract void ExecuteCommand(IChatChannel channel, StreamCommand command);

        /// <summary>
        /// provides help for the 
        /// </summary>
        /// <param name="channel">channel from which help request was received</param>
        /// <param name="user">use which requested help</param>
        public abstract void ProvideHelp(IChatChannel channel, string user);

        /// <summary>
        /// flags channel has to provide for a command to be accepted
        /// </summary>
        public abstract ChannelFlags RequiredFlags { get; }

        /// <summary>
        /// sends a message to a user in a channel (in public chat)
        /// </summary>
        /// <param name="channel">channel to which to send message</param>
        /// <param name="user">name of user</param>
        /// <param name="message">message to send</param>
        protected void SendMessage(IChatChannel channel, string user, string message) {
            channel.SendMessage($"@{user}: {message}");
        }
    }
}