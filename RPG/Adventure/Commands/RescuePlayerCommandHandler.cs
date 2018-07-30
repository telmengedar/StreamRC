using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Adventure.Commands {
    public class RescuePlayerCommandHandler : StreamCommandHandler {
        readonly AdventureModule module;

        public RescuePlayerCommandHandler(AdventureModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Rescue(command.Service, command.Channel, command.User);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Rescues a dead player.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}