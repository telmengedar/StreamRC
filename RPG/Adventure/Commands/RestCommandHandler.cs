using StreamRC.RPG.Players;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Adventure.Commands {
    public class RestCommandHandler : StreamCommandHandler {
        readonly PlayerModule playermodule;
        readonly AdventureModule module;

        public RestCommandHandler(AdventureModule module, PlayerModule playermodule) {
            this.playermodule = playermodule;
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            module.RemoveAdventurer(playermodule.GetExistingPlayer(command.Service, command.User).UserID);
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Takes a rest from adventuring.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}