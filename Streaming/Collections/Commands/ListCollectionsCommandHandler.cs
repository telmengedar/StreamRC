using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Collections.Commands {
    public class ListCollectionsCommandHandler : StreamCommandHandler {
        readonly CollectionModule module;

        public ListCollectionsCommandHandler(CollectionModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            string message = string.Join(", ", module.GetCollectionNames());
            if (message.Length == 0)
                message = "There are no open collections";
            else message = "Open collections: " + message;

            SendMessage(channel, command.User, message);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Lists all available collections. Syntax: !collections");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}