using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Polls.Commands {
    public class RevokeCommandHandler : StreamCommandHandler {
        readonly PollModule module;

        public RevokeCommandHandler(PollModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            PollVote[] votes;
            if (command.Arguments.Length == 0 || (command.Arguments.Length == 1 && command.Arguments[0] == "all"))
            {
                votes = module.GetUserVotes(command.User).ToArray();
                if (votes.Length == 0)
                    throw new StreamCommandException("You haven't voted for anything, so revoking a vote doesn't make any sense.", false);

                if (votes.Length > 1)
                {
                    if (command.Arguments.Length == 1 && command.Arguments[0] == "all")
                    {
                        foreach (PollVote vote in votes)
                            module.ExecuteRevoke(vote.Poll, command.User);
                        SendMessage(channel, command.User, $"You revoked your votes in polls '{string.Join(", ", votes.Select(v => v.Poll))}'");
                        return;
                    }
                    throw new StreamCommandException($"You have voted in more than one poll. Type !revoke all to remove all your votes. You voted in the following polls: {string.Join(", ", votes.Select(v => v.Poll))}");
                }

                module.ExecuteRevoke(votes[0].Poll, command.User);
                SendMessage(channel, command.User, $"You revoked your vote in poll '{votes[0].Poll}'");
                return;
            }

            string poll = command.Arguments[0].ToLower();
            if (module.RevokeVote(command.User, poll))
            {
                SendMessage(channel, command.User, $"You revoked your vote in poll '{poll}'");
                return;
            }

            PollOption[] options = module.FindOptions(command.Arguments);
            string[] keys = options.Select(o => o.Key).ToArray();
            votes = module.GetUserVotes(command.User, keys).ToArray();

            if (votes.Length == 0)
            {
                SendMessage(channel, command.User, "No votes match your arguments so no clue what you want to revoke.");
                return;
            }

            foreach (PollVote vote in votes)
                module.ExecuteRevoke(vote.Poll, command.User);
            SendMessage(channel, command.User, $"You revoked your votes in polls '{string.Join(", ", votes.Select(v => v.Poll))}'");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Removes a vote from a poll. Syntax: !revoke <poll>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}