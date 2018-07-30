using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Inventory.Commands {
    public class DropItemCommandHandler : StreamCommandHandler {
        InventoryModule module;

        public DropItemCommandHandler(InventoryModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.DropItem(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Drops an item from your inventory.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}