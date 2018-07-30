namespace StreamRC.Core {

    /// <summary>
    /// used to send chat messages
    /// </summary>
    public interface IChatMessageSender {

        /// <summary>
        /// sends a message to the primary chat channel
        /// </summary>
        void SendChatMessage(string message);
    }
}