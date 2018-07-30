using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Polls.Commands {
    public class ListPollsCommandHandler : StreamCommandHandler {
        readonly PollModule module;

        public ListPollsCommandHandler(PollModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            string polllist = string.Join(", ", module.GetPolls().Select(p => p.Name));
            SendMessage(channel, command.User, $"Currently running polls: {polllist}");
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Returns a list of currently running polls. Syntax: !polls");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}