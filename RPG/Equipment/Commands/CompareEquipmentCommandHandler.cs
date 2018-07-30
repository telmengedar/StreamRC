using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Equipment.Commands {
    public class CompareEquipmentCommandHandler : StreamCommandHandler {
        readonly EquipmentModule module;

        public CompareEquipmentCommandHandler(EquipmentModule module) {
            this.module = module;
        }
        
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Compare(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Compares an item to the current equipment.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}