using System.Windows.Media;

namespace StreamRC.Streaming.Stream {

    public class ChatMessage {
        public string Service { get; set; }
        public string AvatarLink { get; set; }
        public string User { get; set; }
        public Color UserColor { get; set; }
        public string Message { get; set; }
        public bool IsWhisper { get; set; }

        /// <summary>
        /// emotes in message
        /// </summary>
        public ChatEmote[] Emotes { get; set; }
    }
}