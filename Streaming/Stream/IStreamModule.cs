using NightlyCode.Modules;

namespace StreamRC.Streaming.Stream {
    /// <summary>
    /// interface for a module which implements streaming capabilities
    /// </summary>
    public interface IStreamModule : IModule {

        /// <summary>
        /// sends a message to the author of the specified command
        /// </summary>
        /// <param name="command">original command which led to the message</param>
        /// <param name="message">message to send</param>
        void SendMessage(StreamCommand command, string message);

        /// <summary>
        /// sends a message to a user of a service
        /// </summary>
        /// <param name="service">service to send message to</param>
        /// <param name="user">user to send message to</param>
        /// <param name="message">message to send</param>
        /// <param name="iswhisper">whether to send a private message</param>
        void SendMessage(string service, string user, string message, bool iswhisper = false);

        /// <summary>
        /// sends a message to all connected services
        /// </summary>
        /// <param name="message">message to send</param>
        void BroadcastMessage(string message);

        /// <summary>
        /// sends a message to a service
        /// </summary>
        /// <param name="service">service to send message to</param>
        /// <param name="message">message to send</param>
        void SendMessage(string service, string message);

        /// <summary>
        /// sends a private message to a user of a service
        /// </summary>
        /// <param name="service">service user is registered to</param>
        /// <param name="user">user to send message to</param>
        /// <param name="message">message to send</param>
        void SendPrivateMessage(string service, string user, string message);

        /// <summary>
        /// registers a command handler for a command
        /// </summary>
        /// <param name="handler">handler used to process commands</param>
        /// <param name="commands">commands for which to register handler</param>
        void RegisterCommandHandler(IStreamCommandHandler handler, params string[] commands);

        /// <summary>
        /// unregisters a command handler
        /// </summary>
        /// <param name="handler">handler to unregister</param>
        void UnregisterCommandHandler(IStreamCommandHandler handler);

    }
}