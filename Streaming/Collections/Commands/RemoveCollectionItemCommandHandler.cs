using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Collections.Commands {
    public class RemoveCollectionItemCommandHandler : StreamCommandHandler {
        CollectionModule module;

        public RemoveCollectionItemCommandHandler(CollectionModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.RemoveItem(command.User, command.Arguments[0].ToLower(), command.Arguments[1].ToLower());
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Removes a personal item from a collection. Syntax: !remove <collection> <item>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}