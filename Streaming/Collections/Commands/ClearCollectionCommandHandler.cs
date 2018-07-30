using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Collections.Commands {
    public class ClearCollectionCommandHandler : StreamCommandHandler {
        CollectionModule module;

        public ClearCollectionCommandHandler(CollectionModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Clear(command.User, command.Arguments[0].ToLower());
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Clears all items personally added to a collection. Syntax: !clear <collection>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}