using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Players.Commands {
    public class HealCommandHandler : StreamCommandHandler {
        ConvenienceModule module;

        public HealCommandHandler(ConvenienceModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.Heal(command.Service, command.Channel, command.User);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Heals your character with the best method available.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}