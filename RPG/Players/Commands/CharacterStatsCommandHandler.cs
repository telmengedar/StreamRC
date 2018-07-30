using StreamRC.RPG.Data;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players.Commands {
    public class CharacterStatsCommandHandler : StreamCommandHandler {
        UserModule usermodule;
        PlayerModule module;

        public CharacterStatsCommandHandler(PlayerModule module, UserModule usermodule) {
            this.module = module;
            this.usermodule = usermodule;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            Player player = module.GetExistingPlayer(command.Service, command.User);
            if (player == null)
            {
                SendMessage(channel, command.User, "No character data found for your user.");
                return;
            }

            PlayerAscension ascension = module.GetPlayerAscension(player.UserID);
            SendMessage(channel, command.User, $"You're level {player.Level} with {player.Experience.ToString("F0")}/{ascension?.NextLevel.ToString("F0")} experience. HP {player.CurrentHP}/{player.MaximumHP}, MP {player.CurrentMP}/{player.MaximumMP}. Strength {player.Strength}, Dexterity {player.Dexterity}, Fitness {player.Fitness}, Luck {player.Luck}. {player.Gold} Gold");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Displays your character statistics in chat.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.Game;
    }
}