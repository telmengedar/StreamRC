using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Inventory.Commands {
    public class CraftItemCommandHandler : StreamCommandHandler {
        InventoryModule module;

        public CraftItemCommandHandler(InventoryModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.CraftItem(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Tries to craft a new item using items from your inventory.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}