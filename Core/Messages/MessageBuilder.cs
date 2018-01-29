using System.Collections.Generic;
using System.Drawing;

namespace StreamRC.Core.Messages {
    public class MessageBuilder {
        string currentcolor;
        FontWeight currentweight = FontWeight.Default;
        readonly List<MessageChunk> chunks=new List<MessageChunk>();

        string ColorToString(Color color) {
            return $"rgb({(int)color.R},{(int)color.G},{(int)color.B})";
        }

        public MessageBuilder Text(string text) {
            chunks.Add(new MessageChunk(MessageChunkType.Text, text, currentcolor, currentweight));
            return this;
        }

        public MessageBuilder Text(string text, string color)
        {
            chunks.Add(new MessageChunk(MessageChunkType.Text, text, color, currentweight));
            return this;
        }

        public MessageBuilder Text(string text, string color, FontWeight weight)
        {
            chunks.Add(new MessageChunk(MessageChunkType.Text, text, color, weight));
            return this;
        }

        public MessageBuilder Text(string text, Color color)
        {
            chunks.Add(new MessageChunk(MessageChunkType.Text, text, ColorToString(color), currentweight));
            return this;
        }

        public MessageBuilder Text(string text, Color color, FontWeight weight)
        {
            chunks.Add(new MessageChunk(MessageChunkType.Text, text, ColorToString(color), weight));
            return this;
        }

        public MessageBuilder Image(string imageurl) {
            if(string.IsNullOrEmpty(imageurl))
                return this;

            chunks.Add(new MessageChunk(MessageChunkType.Emoticon, imageurl));
            return this;
        }

        public MessageBuilder Color(string color) {
            if(string.IsNullOrEmpty(color))
                return this;

            currentcolor = color;
            return this;
        }

        public MessageBuilder Color(Color color) {
            currentcolor = ColorToString(color);
            return this;
        }

        public MessageBuilder Bold() {
            currentweight=FontWeight.Bold;
            return this;
        }

        public MessageBuilder Reset() {
            currentcolor = null;
            currentweight=FontWeight.Default;
            return this;
        }

        public bool HasText() {
            return chunks.Count > 0;
        }

        public Message BuildMessage() {
            return new Message(chunks.ToArray());
        }
    }
}