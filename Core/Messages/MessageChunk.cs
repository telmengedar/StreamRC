
using System.Windows.Media;

namespace StreamRC.Core.Messages {

    /// <summary>
    /// chunk of a text message
    /// </summary>
    public class MessageChunk {

        /// <summary>
        /// creates a new <see cref="MessageChunk"/>
        /// </summary>
        /// <param name="content">chunk content</param>
        /// <param name="color">chunk color (optional)</param>
        /// <param name="weight">weight of font</param>
        public MessageChunk(string content, string color = null, FontWeight weight = FontWeight.Default)
            : this(MessageChunkType.Text, content, color, weight)
        {
        }

        /// <summary>
        /// creates a new <see cref="MessageChunk"/>
        /// </summary>
        /// <param name="content">chunk content</param>
        /// <param name="color">chunk color (optional)</param>
        /// <param name="weight">weight of font</param>
        public MessageChunk(string content, Color color, FontWeight weight = FontWeight.Default)
            : this(MessageChunkType.Text, content, $"rgb({color.R},{color.G}, {color.B})", weight)
        {
        }

        /// <summary>
        /// creates a new <see cref="MessageChunk"/>
        /// </summary>
        /// <param name="type">type of chunk</param>
        /// <param name="content">chunk content</param>
        /// <param name="color">chunk color (optional)</param>
        /// <param name="weight">weight of font</param>
        public MessageChunk(MessageChunkType type, string content, string color=null, FontWeight weight=FontWeight.Default) {
            Type = type;
            Content = content;
            Color = color;
            FontWeight = weight;
        }

        /// <summary>
        /// creates a new <see cref="MessageChunk"/>
        /// </summary>
        /// <param name="type">type of chunk</param>
        /// <param name="content">chunk content</param>
        /// <param name="color">chunk color (optional)</param>
        /// <param name="weight">weight of font</param>
        public MessageChunk(MessageChunkType type, string content, Color color, FontWeight weight = FontWeight.Default)
            : this(type, content, $"rgb({color.R},{color.G}, {color.B})", weight)
        {
        }

        /// <summary>
        /// creates a new <see cref="MessageChunk"/>
        /// </summary>
        public MessageChunk() { }

        /// <summary>
        /// type of chunk
        /// </summary>
        public MessageChunkType Type { get; set; }

        /// <summary>
        /// weight of the font if type is text
        /// </summary>
        public FontWeight FontWeight { get; set; }

        /// <summary>
        /// element content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// alternative content to display if content can't be displayed (text for images)
        /// </summary>
        public string Alternative { get; set; }

        /// <summary>
        /// color of element if applicable
        /// </summary>
        public string Color { get; set; }
    }
}