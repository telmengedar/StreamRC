using System.Windows.Media;
using StreamRC.Streaming.Chat;

namespace StreamRC.Streaming.Stream.Chat {

    /// <summary>
    /// message received in chat
    /// </summary>
    public class ChatMessage {

        /// <summary>
        /// service from which message was received
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// channel from which message was received
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// link to avatar representing user
        /// </summary>
        public string AvatarLink { get; set; }

        /// <summary>
        /// name of user who created message
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// color for user
        /// </summary>
        public Color UserColor { get; set; }

        /// <summary>
        /// message content
        /// </summary>
        public string Message { get; set; }

        public bool IsWhisper { get; set; }

        /// <summary>
        /// emotes in message
        /// </summary>
        public ChatEmote[] Emotes { get; set; }

        /// <summary>
        /// attachements in message
        /// </summary>
        public MessageAttachement[] Attachements { get; set; }
    }
}