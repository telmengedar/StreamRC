using System.Linq;

namespace StreamRC.Core.Messages {

    /// <summary>
    /// system message
    /// </summary>
    public class Message {

        /// <summary>
        /// creates a new <see cref="Message"/>
        /// </summary>
        public Message() { }

        /// <summary>
        /// creates a new <see cref="Message"/>
        /// </summary>
        /// <param name="chunks">chunks building up the message</param>
        public Message(MessageChunk[] chunks) {
            Chunks = chunks;
        }

        /// <summary>
        /// chunks building up the message
        /// </summary>
        public MessageChunk[] Chunks { get; set; }

        /// <summary>
        /// used to parse a message from string
        /// </summary>
        /// <remarks>
        /// this just calls <see cref="MessageExtensions.Parse"/>
        /// </remarks>
        /// <param name="message">message to parse</param>
        /// <param name="arguments">arguments for parser</param>
        /// <returns>parsed <see cref="Message"/></returns>
        public static Message Parse(string message, params object[] arguments) {
            return MessageExtensions.Parse(message, arguments);
        }

        public override string ToString() {
            return string.Join("", Chunks.Where(c => c.Type == MessageChunkType.Text).Select(c => c.Content));
        }
    }
}