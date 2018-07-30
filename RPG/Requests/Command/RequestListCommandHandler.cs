using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.RPG.Requests.Command {
    public class RequestListCommandHandler : StreamCommandHandler {
        readonly GameRequestModule module;

        public RequestListCommandHandler(GameRequestModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            GameRequest[] gamerequests = module.GetRequests();
            if (gamerequests.Length == 0)
            {
                SendMessage(channel, command.User, "No requests currently in queue.");
                return;
            }

            SendMessage(channel, command.User, $"Game Requests: {string.Join(", ", gamerequests.Select(r => $"{r.Game} ({r.Platform}{(string.IsNullOrEmpty(r.Conditions) ? "" : ", " + r.Conditions)})"))}");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Displays the request queue in chat");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}