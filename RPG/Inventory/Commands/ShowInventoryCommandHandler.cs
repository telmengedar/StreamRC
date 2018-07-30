using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Inventory.Commands {
    public class ShowInventoryCommandHandler : StreamCommandHandler {
        InventoryModule module;

        public ShowInventoryCommandHandler(InventoryModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.ShowInventory(command.Service, command.Channel, command.User);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Displays all items currently in your inventory.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}