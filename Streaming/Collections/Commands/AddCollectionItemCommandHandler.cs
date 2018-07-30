using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Collections.Commands {
    public class AddCollectionItemCommandHandler : StreamCommandHandler {
        CollectionModule module;

        public AddCollectionItemCommandHandler(CollectionModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.AddItem(command.User, command.Arguments[0].ToLower(), command.Arguments[1].ToLower());
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Adds an item to a collection. Syntax: !add <collection> <item>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}