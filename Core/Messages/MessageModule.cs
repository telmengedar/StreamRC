using System;
using NightlyCode.Modules;
using NightlyCode.StreamRC.Modules;

namespace StreamRC.Core.Messages {
    /// <summary>
    /// module managing system messages
    /// </summary>
    [ModuleKey("messages")]
    public class MessageModule : IModule, IMessageModule {

        /// <summary>
        /// creates a new <see cref="MessageModule"/>
        /// </summary>
        /// <param name="context"></param>
        public MessageModule(Context context) {}

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