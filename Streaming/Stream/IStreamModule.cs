using System;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Stream.Commands;

namespace StreamRC.Streaming.Stream {
    /// <summary>
    /// interface for a module which implements streaming capabilities
    /// </summary>
    public interface IStreamModule {

        /// <summary>
        /// adds a channel to the stream module
        /// </summary>
        /// <param name="channel">channel to add</param>
        void AddChannel(IChatChannel channel);

        /// <summary>
        /// adds a service to the collection
        /// </summary>
        /// <param name="type">type of service</param>
        /// <param name="module">module handling service calls</param>
        void AddService(string type, IStreamServiceModule module);

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

        /// <summary>
        /// triggered when a chat message was received
        /// </summary>
        event Action<IChatChannel, ChatMessage> ChatMessage;
    }
}