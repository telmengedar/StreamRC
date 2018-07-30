using StreamRC.RPG.Players.Skills;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Players.Commands {
    public class SkillListCommandHandler : StreamCommandHandler {
        SkillModule module;

        public SkillListCommandHandler(SkillModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.ShowSkillList(command.Service, command.Channel, command.User);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Shows the skillist in chat.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}