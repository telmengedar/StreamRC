using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// interface for a stream command
    /// </summary>
    public interface IStreamCommandHandler {

        /// <summary>
        /// executes a <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which command was received</param>
        /// <param name="command">command to execute</param>
        void ExecuteCommand(IChatChannel channel, StreamCommand command);

        /// <summary>
        /// provides help for the <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which help request was received</param>
        /// <param name="user">use which requested help</param>
        void ProvideHelp(IChatChannel channel, string user);

        /// <summary>
        /// flags channel has to provide for a command to be accepted
        /// </summary>
        ChannelFlags RequiredFlags { get; }
    }
}