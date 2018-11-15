using System.Linq;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Shouts {

    /// <summary>
    /// lists avaiable shouts in chat
    /// </summary>
    public class ListShoutsHandler : StreamCommandHandler {
        readonly ShoutModule module;

        /// <summary>
        /// creates a new <see cref="ListShoutsHandler"/>
        /// </summary>
        /// <param name="module">access to shout module</param>
        public ListShoutsHandler(ShoutModule module) {
            this.module = module;
        }

        /// <summary>
        /// executes a <see cref="StreamCommand"/>
        /// </summary>
        /// <param name="channel">channel from which command was received</param>
        /// <param name="command">command to execute</param>
        public override void ExecuteCommand(IChatChannel channel, StreamCommand command) {
            SendMessage(channel, command.User, string.Join(",", module.Shouts.Select(s => s.Term)));
        }

        /// <summary>
        /// provides help for the 
        /// </summary>
        /// <param name="channel">channel from which help request was received</param>
        /// <param name="user">use which requested help</param>
        public override void ProvideHelp(IChatChannel channel, string user) {
            SendMessage(channel, user, "Lists available shouts in chat");
        }

        /// <summary>
        /// flags channel has to provide for a command to be accepted
        /// </summary>
        public override ChannelFlags RequiredFlags => ChannelFlags.None;
    }
}