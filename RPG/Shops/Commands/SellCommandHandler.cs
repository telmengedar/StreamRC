using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Shops.Commands {
    public class SellCommandHandler : StreamCommandHandler {
        ShopModule module;

        public SellCommandHandler(ShopModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Sell(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Sells an item to the shop. Syntax: !sell <item> [amount]. If no amount is specified, all items will be sold.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}