using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Items.Commands {
    public class ItemInfoCommandHandler : StreamCommandHandler {
        ItemModule module;

        public ItemInfoCommandHandler(ItemModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.PrintItemInfo(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Provides info about an item in the chat. Syntax: !iteminfo <itemname>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}