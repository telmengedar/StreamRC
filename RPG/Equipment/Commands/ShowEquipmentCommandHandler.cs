using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Equipment.Commands {
    public class ShowEquipmentCommandHandler : StreamCommandHandler {
        readonly EquipmentModule module;

        public ShowEquipmentCommandHandler(EquipmentModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.ShowEquipment(command.Service, command.Channel, command.User);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Shows your current equipment.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}