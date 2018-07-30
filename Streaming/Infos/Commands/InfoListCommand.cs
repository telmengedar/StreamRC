using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Infos.Commands {
    public class InfoListCommand : StreamCommandHandler {
        readonly InfoModule module;

        public InfoListCommand(InfoModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            SendMessage(channel, command.User, $"List of available infos: {string.Join(", ", module.GetInfos().Select(i => i.Key))}");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Provides a list of available infos. Syntax: !infos");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}