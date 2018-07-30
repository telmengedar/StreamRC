using System;
using NightlyCode.Core.Collections;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.RPG.Messages {

    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(MessageModule))]
    public class GameMessageModule : IInitializableModule, IMessageModule {
        readonly Context context;
        StreamModule streammodule;
        MessageModule messagemodule;

        /// <summary>
        /// creates a new <see cref="GameMessageModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public GameMessageModule(Context context) {
            this.context = context;
        }

        void IInitializableModule.Initialize() {
            streammodule = context.GetModule<StreamModule>();
            messagemodule = context.GetModule<MessageModule>();
        }

        /// <summary>
        /// sends a game message to all channels
        /// </summary>
        /// <param name="message">message to send</param>
        public void SendGameMessage(Message message) {
            messagemodule.AddMessage(message);
            streammodule.GetChannels(ChannelFlags.Game).Foreach(c => c.SendMessage(message.ToString()));
        }

        public event Action<Message> Message;

        void IMessageModule.AddMessage(Message message) {
            SendGameMessage(message);
            Message?.Invoke(message);
        }
    }
}