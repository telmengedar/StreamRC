using System;

namespace StreamRC.Core.Scripts {

    /// <summary>
    /// attribute used to declare methods to be callable from stream
    /// </summary>
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Property, AllowMultiple = true)]
    public class CommandAttribute : Attribute {

        /// <summary>
        /// creates a new <see cref="CommandAttribute"/>
        /// </summary>
        /// <param name="streamCommand">text under which command is registered in stream</param>
        /// <param name="arguments">parsed command used to execute script</param>
        public CommandAttribute(string streamCommand, params object[] arguments) {
            StreamCommand = streamCommand;
            Arguments = arguments;
        }

        /// <summary>
        /// text under which command is registered in stream
        /// </summary>
        public string StreamCommand { get; }

        /// <summary>
        /// arguments to command in script format used to generate script to be parsed
        /// </summary>
        public object[] Arguments { get; }
    }
}