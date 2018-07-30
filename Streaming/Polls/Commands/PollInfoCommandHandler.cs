using System.Linq;
using System.Text;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Polls.Commands {
    public class PollInfoCommandHandler : StreamCommandHandler {
        readonly PollModule module;

        public PollInfoCommandHandler(PollModule module) {
            this.module = module;
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if (command.Arguments.Length != 1)
                throw new StreamCommandException("Invalid command syntax");

            string pollname = command.Arguments[0];

            Poll poll = module.GetPoll(pollname);
            if (poll == null)
                throw new StreamCommandException($"There is no active poll named '{pollname}'");

            PollOption[] options = module.GetOptions(pollname);
            StringBuilder message = new StringBuilder(poll.Description).Append(": ");
            if (options.Length == 0)
            {
                message.Append("This is an unrestricted poll, so please vote for 'penis' when you're out of ideas");
            }
            else
            {
                message.Append(string.Join(", ", options.Select(o => $"{o.Key} - {o.Description}")));
                message.Append(". Usually there is more info available by typing !info <option>");
            }

            SendMessage(channel, command.User, message.ToString());
        }

        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Returns info about an active poll. Syntax: !pollinfo <poll>");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}