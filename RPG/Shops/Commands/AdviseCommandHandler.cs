using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Shops.Commands {
    public class AdviseCommandHandler : StreamCommandHandler {
        readonly ShopModule module;

        public AdviseCommandHandler(ShopModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Advise(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Gets an advise which equipment the shopkeeper has on stock which would improve your stats.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}