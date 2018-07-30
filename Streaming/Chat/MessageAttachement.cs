namespace StreamRC.Streaming.Chat {

    /// <summary>
    /// attachement for a message
    /// </summary>
    public class MessageAttachement {

        /// <summary>
        /// type of attachement
        /// </summary>
        public AttachmentType Type { get; set; }

        /// <summary>
        /// original source of data (url)
        /// </summary>
        public string OriginalSource { get; set; }

        /// <summary>
        /// url to attachement
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// width of image (if type is <see cref="AttachmentType.Image"/>)
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// height of image (if type is <see cref="AttachmentType.Image"/>)
        /// </summary>
        public int Height { get; set; }
    }
}