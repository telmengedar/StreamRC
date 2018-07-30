using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Infos.Commands {
    public class InfoCommand : StreamCommandHandler {
        readonly InfoModule module;

        public InfoCommand(InfoModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if (command.Arguments.Length != 1)
                throw new StreamCommandException("Invalid command syntax. Expected syntax: !info <item>");

            string key = command.Arguments[0];
            Info info = module.GetInfo(key);

            SendMessage(channel, command.User, info == null ? $"There is no info for '{key}'" : info.Text);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Provides info about a predefined topic. Syntax: !info <topic>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}