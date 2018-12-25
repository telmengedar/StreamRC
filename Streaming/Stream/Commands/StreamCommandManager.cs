using System;
using System.Collections.Generic;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;

namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// manages commands available to stream
    /// </summary>
    [Module]
    public class StreamCommandManager {
        readonly object commandhandlerlock = new object();
        readonly Dictionary<string, CommandHandlerInformation> commandhandlers = new Dictionary<string, CommandHandlerInformation>();
        readonly IModuleContext context;

        /// <summary>
        /// creates a new <see cref="StreamCommandManager"/>
        /// </summary>
        /// <param name="context">access to module context used to get stream handlers</param>
        public StreamCommandManager(IModuleContext context) {
            this.context = context;
        }

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
        public void AddCommandHandler(string command, Type handler) {
            lock (commandhandlerlock)
            {
                if (commandhandlers.ContainsKey(command))
                    Logger.Warning(this, $"'{command}' already registered to '{commandhandlers[command].Type.Name}'. Handler will be replaced by '{handler.Name}'");
                context.AddModule(handler);
                commandhandlers[command] = new CommandHandlerInformation(handler);
            }
        }

        /// <summary>
        /// adds a command handler to management
        /// </summary>
        /// <param name="command">command under which handler is to be added</param>
        /// <param name="handler">handler to be added</param>
        public void AddCommandHandler(string command, IStreamCommandHandler handler)
        {
            lock (commandhandlerlock)
            {
                if (commandhandlers.ContainsKey(command))
                    Logger.Warning(this, $"'{command}' already registered to '{commandhandlers[command].Type.Name}'. Handler will be replaced by '{handler.GetType().Name}'");
                context.AddModule(handler.GetType(), provider => handler);
                commandhandlers[command] = new CommandHandlerInformation(handler.GetType(), handler);
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
            lock(commandhandlerlock) {
                IStreamCommandHandler handler = GetCommandHandlerOrDefault(command);
                if(handler == null)
                    throw new Exception($"Handler for {command} not found");
                return handler;
            }
        }

        /// <summary>
        /// get command handler for specified command or null if no command handler is found
        /// </summary>
        /// <param name="command">command under which to search for a handler</param>
        /// <returns>command handler found for command or null if no handler is found</returns>
        public IStreamCommandHandler GetCommandHandlerOrDefault(string command) {
            lock (commandhandlerlock) {
                if(!commandhandlers.TryGetValue(command, out CommandHandlerInformation info))
                    return null;

                if(info.Handler == null)
                    info.Handler = (IStreamCommandHandler)context.GetModule(info.Type);

                return info.Handler;
            }
        }
    }
}