using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Polls.Commands {
    public class UserVoteCommandHandler : StreamCommandHandler {
        readonly PollModule module;

        public UserVoteCommandHandler(PollModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            PollVote[] votes = module.GetUserVotes(command.User, command.Arguments.Length > 0 ? command.Arguments[0] : null).ToArray();

            if (votes.Length == 0)
                SendMessage(channel, command.User, $"You didn't vote for anything{(command.Arguments.Length > 0 ? $" in poll {command.Arguments[0]}" : "")}");
            else SendMessage(channel, command.User, $"You voted for: {string.Join(", ", votes.Select(v => $"'{v.Vote}' in poll '{v.Poll}'"))}");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Displays the options for which you have voted in chat. Syntax: !myvote [poll]");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}