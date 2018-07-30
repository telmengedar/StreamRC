using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Collections.Commands {
    public class CollectionInfoCommandHandler : StreamCommandHandler {
        readonly CollectionModule module;

        public CollectionInfoCommandHandler(CollectionModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            string collectionname = command.Arguments[0];
            Collection collection = module.GetCollection(collectionname);
            if (collection == null)
                throw new StreamCommandException($"There is no collection named '{collectionname}'");

            string itemsperuser = collection.ItemsPerUser > 0 ? $"max {collection.ItemsPerUser} per user." : "unlimited items per user";
            SendMessage(channel, command.User, $"Collection {collectionname}: {collection.Description} - {itemsperuser}");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Provides info about a collection. Syntax: !collectioninfo <collection>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}