using System.Linq;
using NightlyCode.Core.Logs;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Polls.Commands {
    public class PollResultCommandHandler : StreamCommandHandler {
        readonly PollModule module;

        public PollResultCommandHandler(PollModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            string pollkey;
            if (command.Arguments.Length == 0)
            {
                Logger.Info(this, $"Starting heuristic poll result estimation for '{command.User}'");
                ActivePoll leadingpoll = module.GetMostActivePoll();
                if (leadingpoll == null)
                {
                    SendMessage(channel, command.User, "Since no one voted for anything i can't show you any poll.");
                    return;
                }

                SendMessage(channel, command.User, $"You seem to be too lazy to tell me which poll you want to know something about. I just guess you want to see poll '{leadingpoll.Name}' since it is the most active poll.");
                pollkey = leadingpoll.Name;
            }
            else pollkey = command.Arguments[0];

            Poll poll = module.GetPoll(pollkey);

            if (poll == null)
            {
                SendMessage(channel, command.User, $"There is no poll named '{pollkey}'");
                return;
            }

            PollDiagramData data = new PollDiagramData(module.GetWeightedVotes(pollkey));

            string message = $"Results for {pollkey}: {string.Join(", ", data.GetItems(100).Where(r => r.Count > 0).Select(r => $"{r.Item} [{r.Count}]"))}";
            SendMessage(channel, command.User, message);
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}