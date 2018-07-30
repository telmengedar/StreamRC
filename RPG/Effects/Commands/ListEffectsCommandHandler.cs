using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Effects.Commands {
    public class ListEffectsCommandHandler : StreamCommandHandler {
        readonly EffectModule module;

        public ListEffectsCommandHandler(EffectModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.ListEffects(command.Service, command.Channel, command.User);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Lists all active effects on rpg player.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}