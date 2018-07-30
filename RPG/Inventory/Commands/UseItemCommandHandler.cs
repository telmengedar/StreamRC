using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Inventory.Commands {
    public class UseItemCommandHandler : StreamCommandHandler {
        InventoryModule module;

        public UseItemCommandHandler(InventoryModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.UseItem(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Uses an item in your inventory.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}