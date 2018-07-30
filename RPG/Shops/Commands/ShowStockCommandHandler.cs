using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Shops.Commands {
    public class ShowStockCommandHandler : StreamCommandHandler {
        ShopModule module;

        public ShowStockCommandHandler(ShopModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Stock(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Determines how many units of an item are available in shop. Syntax: !stock <item>.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}