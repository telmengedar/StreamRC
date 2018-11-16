using System;
using NightlyCode.Core.Collections;
using NightlyCode.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.RPG.Messages {

    [Module]
    public class GameMessageModule : IMessageModule {
        readonly StreamModule streammodule;
        readonly MessageModule messagemodule;

        /// <summary>
        /// creates a new <see cref="GameMessageModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public GameMessageModule(StreamModule streammodule, MessageModule messagemodule) {
            this.streammodule = streammodule;
            this.messagemodule = messagemodule;
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