namespace NightlyCode.StreamRC.Modules {

    /// <summary>
    /// interface for a message sender
    /// </summary>
    public interface IMessageSender {

        /// <summary>
        /// sends a message
        /// </summary>
        /// <param name="message">message to send</param>
        void SendMessage(string message);
    }
}