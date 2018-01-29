namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// interface for a stream command handler
    /// </summary>
    public interface IStreamCommandHandler {

        /// <summary>
        /// processes a stream command
        /// </summary>
        /// <param name="command"></param>
        void ProcessStreamCommand(StreamCommand command);

        /// <summary>
        /// provides help for a stream command
        /// </summary>
        /// <param name="command">command to provide help for</param>
        string ProvideHelp(string command);
    }
}