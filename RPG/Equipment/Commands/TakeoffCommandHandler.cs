using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Equipment.Commands {
    public class TakeoffCommandHandler : StreamCommandHandler {
        EquipmentModule module;

        public TakeoffCommandHandler(EquipmentModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.TakeOff(command.Service, command.Channel, command.User, string.Join(" ", command.Arguments));
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Takes off an equipped item and puts it back in your inventory. !takeoff <itemname|slot>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}