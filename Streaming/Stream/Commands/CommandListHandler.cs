using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// lists available commands
    /// </summary>
    public class CommandListHandler : StreamCommandHandler {
        readonly StreamCommandManager manager;

        /// <summary>
        /// creates a new <see cref="CommandListHandler"/>
        /// </summary>
        /// <param name="manager">manager for command handlers</param>
        public CommandListHandler(StreamCommandManager manager) {
            this.manager = manager;
        }

        /// <summary>
        /// executes a <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which command was received</param>
        /// <param name="command">command to execute</param>
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            channel.SendMessage($"Available commands (try !help <command> for more info): {string.Join(", ", manager.Commands)}");
        }

        /// <summary>
        /// provides help for the 
        /// </summary>
        /// <param name="channel">channel from which help request was received</param>
        /// <param name="user">use which requested help</param>
        public override void ProvideHelp(IChatChannel channel, string user) {
            channel.SendMessage($"@{user}: Prints a list of supported commands.Syntax: !commands");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}