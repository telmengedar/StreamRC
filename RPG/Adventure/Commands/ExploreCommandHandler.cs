using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Adventure.Commands {
    public class ExploreCommandHandler : StreamCommandHandler {
        readonly PlayerModule playermodule;
        readonly AdventureModule module;

        public ExploreCommandHandler(AdventureModule module, PlayerModule playermodule) {
            this.playermodule = playermodule;
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.AddAdventurer(playermodule.GetExistingPlayer(command.Service, command.User));
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Starts exploring the wilderness.");
        }

        /// <summary>
        /// flags channel has to provide for a command to be accepted
        /// </summary>
        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}