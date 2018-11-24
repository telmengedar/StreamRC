using System.Collections.Generic;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// variables used for stream command
    /// </summary>
    public class StreamCommandVariables {
        readonly IChatChannel channel;
        readonly StreamCommand command;

        /// <summary>
        /// creates a new <see cref="StreamCommandVariables"/>
        /// </summary>
        /// <param name="channel">channel where command was received</param>
        /// <param name="command">received command</param>
        public StreamCommandVariables(IChatChannel channel, StreamCommand command) {
            this.channel = channel;
            this.command = command;
        }

        /// <summary>
        /// name of service where channel is connected to
        /// </summary>
        public string Service => channel.Service;

        /// <summary>
        /// name of chat channel
        /// </summary>
        public string Name => channel.Name;

        /// <summary>
        /// channel flags
        /// </summary>
        public ChannelFlags Flags => channel.Flags;

        /// <summary>
        /// users currently in chat
        /// </summary>
        public IEnumerable<string> Users => channel.Users;

        /// <summary>
        /// user which sent the command
        /// </summary>
        public string User => command.User;

        /// <summary>
        /// command name
        /// </summary>
        public string Command => command.Command;

        /// <summary>
        /// arguments to command
        /// </summary>
        public string[] Arguments => command.Arguments;

        /// <summary>
        /// determines whether the command was whispered
        /// </summary>
        public bool IsWhispered => command.IsWhispered;

        /// <summary>
        /// determines whether the command is a system command
        /// </summary>
        public bool IsSystemCommand => command.IsSystemCommand;

    }
}