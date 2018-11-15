using System;
using NightlyCode.Modules;

namespace StreamRC.Core.Messages {
    /// <summary>
    /// module managing system messages
    /// </summary>
    [Module(Key="messages")]
    public class MessageModule : IMessageModule {

        /// <summary>
        /// triggered when a message was received
        /// </summary>
        public event Action<Message> Message;

        /// <summary>
        /// adds a message 
        /// </summary>
        /// <param name="message">message to add</param>
        public void AddMessage(Message message) {
            Message?.Invoke(message);
        }
    }
}