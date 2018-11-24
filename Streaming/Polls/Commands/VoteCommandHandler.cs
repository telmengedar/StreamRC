using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Polls.Commands {

    /// <summary>
    /// executes user votes for polls
    /// </summary>
    [Module]
    public class VoteCommandHandler : StreamCommandHandler {
        readonly PollModule module;

        /// <summary>
        /// creates a new <see cref="VoteCommandHandler"/>
        /// </summary>
        /// <param name="module">access to <see cref="PollModule"/></param>
        public VoteCommandHandler(PollModule module) {
            this.module = module;
        }

        void HeuristicVote(IChatChannel channel, StreamCommand command)
        {
            Logger.Info(this, $"Executing heuristic vote for '{command}'");


            PollOption[] options = module.FindOptions(command.Arguments);

            string optionname = string.Join(" ", command.Arguments);
            if (options.Length > 1)
                throw new StreamCommandException($"Sadly there is more than one poll which contains an option '{optionname}' so you need to specify in which poll you want to vote ({string.Join(", ", options.Select(o => o.Poll))}).", false);

            if (options.Length == 0) {
                Poll poll = module.GetPoll(optionname);
                if (poll == null)
                    throw new StreamCommandException($"There is no poll and no option named '{optionname}' so i have no idea what you want to vote for.", false);

                options = module.GetOptions(poll.Name);
                throw new StreamCommandException($"You need to specify the option to vote for. The following options are available. {string.Join(", ", options.Select(o => $"'{o.Key}' for '{o.Description}'"))}", false);
            }

            module.ExecuteVote(options[0].Poll, command.User, options[0].Key);
            SendMessage(channel, command.User, $"You voted successfully for '{options[0].Description}' in poll '{options[0].Poll}'.");
        }

        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            if(command.Arguments.Length != 2) {
                HeuristicVote(channel, command);
                return;
            }

            string poll = command.Arguments[0].ToLower();
            if(!module.ExistsPoll(poll)) {
                HeuristicVote(channel, command);
                return;
            }

            string vote = command.Arguments[1].ToLower();

            if(module.HasOptions(poll) && !module.ExistsOption(poll, vote)) {
                HeuristicVote(channel, command);
                return;
            }

            module.ExecuteVote(poll, command.User, vote);

            SendMessage(channel, command.User, $"You voted successfully for {vote} in poll {poll}.");
        }

        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}