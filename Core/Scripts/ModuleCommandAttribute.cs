using System;

namespace StreamRC.Core.Scripts {

    /// <summary>
    /// specified commands on module level
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModuleCommandAttribute : Attribute {
        /// <summary>
        /// creates a new <see cref="CommandAttribute"/>
        /// </summary>
        /// <param name="command">name of command to add</param>
        /// <param name="handler">handler used to call method</param>
        public ModuleCommandAttribute(string command, Type handler) {
            Command = command;
            Handler = handler;
        }

        /// <summary>
        /// name of command to add
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// handler to instantiate
        /// </summary>
        public Type Handler { get; }
    }
}