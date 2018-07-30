using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Inventory.Commands {
    public class GiveCommandHandler : StreamCommandHandler {
        InventoryModule module;

        public GiveCommandHandler(InventoryModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.DonateItem(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Gives an item to another player.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}