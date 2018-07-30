using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {
    public class HelpCommandHandler : IStreamCommandHandler {
        readonly StreamCommandManager manager;

        public HelpCommandHandler(StreamCommandManager manager) {
            this.manager = manager;
        }

        public void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            string helpcommand = "help";
            if (command.Arguments.Length > 0)
                helpcommand = command.Arguments[0];

            IStreamCommandHandler handler = manager[helpcommand];

            if (handler == null)
                channel.SendMessage($"@{command.User}: Unknown command '{helpcommand}', try !commands for a list of commands.");
            else {
                handler.ProvideHelp(channel, command.User);
            }
        }

        public void ProvideHelp(IChatChannel channel, string user) {
            channel.SendMessage($"@{user}: Returns help on how to use a command.Syntax: !help <command>");
        }

        public ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}