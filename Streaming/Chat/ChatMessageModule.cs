using NightlyCode.Core.Collections;
using NightlyCode.Modules;
using StreamRC.Core.Messages;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Chat {

    [Module]
    public class ChatMessageModule {
        readonly StreamModule streammodule;
        readonly MessageModule messagemodule;

        public ChatMessageModule(StreamModule streammodule, MessageModule messagemodule) {
            this.streammodule = streammodule;
            this.messagemodule = messagemodule;
        }

        /// <summary>
        /// sends a game message to all channels
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="flags">flags for channels to match to send a message to</param>
        public void SendMessage(Message message, ChannelFlags flags=ChannelFlags.None)
        {
            messagemodule.AddMessage(message);
            if (flags != ChannelFlags.None)
                streammodule.GetChannels(flags).Foreach(c => c.SendMessage(message.ToString()));
        }

    }
}