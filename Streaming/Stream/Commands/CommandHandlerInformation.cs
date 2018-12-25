using System;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// information about a command handler
    /// </summary>
    public class CommandHandlerInformation {

        /// <summary>
        /// creates new <see cref="CommandHandlerInformation"/>
        /// </summary>
        /// <param name="type">type of command handler to instantiate</param>
        /// <param name="handler">command handler instance</param>
        public CommandHandlerInformation(Type type, IStreamCommandHandler handler=null) {
            Type = type;
            Handler = handler;
        }

        /// <summary>
        /// type of handler
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// instantiated handler
        /// </summary>
        public IStreamCommandHandler Handler { get; set; }
    }
}