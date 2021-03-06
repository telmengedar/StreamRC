using NightlyCode.Modules;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Stream {
    /// <summary>
    /// interface for a module which implements streaming capabilities
    /// </summary>
    public interface IStreamModule : IModule {

        /// <summary>
        /// registers a command handler for a command
        /// </summary>
        /// <param name="handler">handler used to process commands</param>
        /// <param name="command">command for which to register handler</param>
        void RegisterCommandHandler(string command, IStreamCommandHandler handler);

        /// <summary>
        /// unregisters a command handler
        /// </summary>
        /// <param name="command">command of which to remove command handler</param>
        void UnregisterCommandHandler(string command);

        void SendMessage(string service, string channel, string user, string message);
    }
}