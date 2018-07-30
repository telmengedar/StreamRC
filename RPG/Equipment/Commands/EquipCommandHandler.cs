using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Equipment.Commands {
    public class EquipCommandHandler : StreamCommandHandler {
        readonly EquipmentModule module;

        public EquipCommandHandler(EquipmentModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Equip(command.Service, command.Channel, command.User, string.Join(" ", command.Arguments));
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Equips an item which is stored in your inventory. Syntax: !equip <itemname>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}