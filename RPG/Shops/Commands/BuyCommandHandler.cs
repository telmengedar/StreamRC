using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Shops.Commands {
    public class BuyCommandHandler : StreamCommandHandler {
        ShopModule module;

        public BuyCommandHandler(ShopModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Buy(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Buys an item from the shop. Syntax: !buy <item> [amount]. If no amount is specified 1 is considered to be the quantity to buy.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}