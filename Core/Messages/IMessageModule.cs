using System;

namespace StreamRC.Core.Messages {
    /// <summary>
    /// interface used to send messages
    /// </summary>
    public interface IMessageModule {

        /// <summary>
        /// triggered when a message was received
        /// </summary>
        event Action<Message> Message;

        /// <summary>
        /// adds a message 
        /// </summary>
        /// <param name="message">message to add</param>
        void AddMessage(Message message);
    }
}