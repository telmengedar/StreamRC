using System.Collections.Generic;
using NightlyCode.Core.Logs;

namespace StreamRC.Streaming.Stream.Commands {
    public class StreamCommandManager {
        readonly object commandhandlerlock = new object();
        readonly Dictionary<string, IStreamCommandHandler> commandhandlers = new Dictionary<string, IStreamCommandHandler>();

        /// <summary>
        /// indexer for access to handlers
        /// </summary>
        /// <param name="command">command for which to find handler</param>
        /// <returns>commandhandler for command or null if no commandhandler is found</returns>
        public IStreamCommandHandler this[string command] => GetCommandHandlerOrDefault(command);

        /// <summary>
        /// adds a command handler to management
        /// </summary>
        /// <param name="command">command under which handler is to be added</param>
        /// <param name="handler">handler to be added</param>
        public void AddCommandHandler(string command, IStreamCommandHandler handler) {
            lock (commandhandlerlock)
            {
                if (commandhandlers.ContainsKey(command))
                    Logger.Warning(this, $"'{command}' already registered to '{commandhandlers[command].GetType().Name}'. Handler will be replaced by '{handler.GetType().Name}'");
                commandhandlers[command] = handler;
            }
        }

        /// <summary>
        /// removes a command handler from management
        /// </summary>
        /// <param name="command">command for which command handler is to be removed</param>
        public void RemoveCommandHandler(string command)
        {
            lock (commandhandlerlock)
                commandhandlers.Remove(command);
        }

        /// <summary>
        /// list of added commands
        /// </summary>
        public IEnumerable<string> Commands
        {
            get
            {
                lock(commandhandlerlock)
                    foreach(string command in commandhandlers.Keys)
                        yield return command;
            }
        }

        /// <summary>
        /// get command handler for the specified command
        /// </summary>
        /// <param name="command">command under which a command handler is registered</param>
        /// <returns>command handler found for command</returns>
        public IStreamCommandHandler GetCommandHandler(string command) {
            lock(commandhandlerlock)
                return commandhandlers[command];
        }

        /// <summary>
        /// get command handler for specified command or null if no command handler is found
        /// </summary>
        /// <param name="command">command under which to search for a handler</param>
        /// <returns>command handler found for command or null if no handler is found</returns>
        public IStreamCommandHandler GetCommandHandlerOrDefault(string command) {
            lock(commandhandlerlock) {
                IStreamCommandHandler handler;
                commandhandlers.TryGetValue(command, out handler);
                return handler;
            }
        }
    }
}