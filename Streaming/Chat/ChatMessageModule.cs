using System.Threading.Tasks;
using NightlyCode.Core.Collections;
using NightlyCode.Modules;
using StreamRC.Core.Messages;
using StreamRC.Core.TTS;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;

namespace StreamRC.Streaming.Chat {

    [Module]
    public class ChatMessageModule {
        readonly StreamModule streammodule;
        readonly MessageModule messagemodule;
        readonly TTSModule ttsmodule;

        public ChatMessageModule(StreamModule streammodule, MessageModule messagemodule, TTSModule ttsmodule) {
            this.streammodule = streammodule;
            this.messagemodule = messagemodule;
            this.ttsmodule = ttsmodule;
        }

        /// <summary>
        /// sends a game message to all channels
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="flags">flags for channels to match to send a message toB</param>
        public void SendMessage(Message message, ChannelFlags flags=ChannelFlags.None, string tts=null)
        {
            messagemodule.AddMessage(message);
            if (flags != ChannelFlags.None)
                foreach(IChatChannel channel in streammodule.GetChannels(flags))
                    Task.Run(()=> channel.SendMessage(message.ToString()));

            if (tts != null)
                ttsmodule.Speak(message.ToString(), tts);
        }

    }
}