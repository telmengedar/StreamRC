using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Players.Commands {
    public class CastSpellCommandHandler : StreamCommandHandler {
        SkillModule module;

        public CastSpellCommandHandler(SkillModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.CastSpell(command.Service, command.Channel, command.User, command.Arguments);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Casts a spell. Syntax !cast <spell>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}