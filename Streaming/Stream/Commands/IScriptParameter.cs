namespace StreamRC.Streaming.Stream.Commands {

    /// <summary>
    /// parameter for script call
    /// </summary>
    public interface IScriptParameter {

        /// <summary>
        /// representation of parameter in stream command
        /// </summary>
        string Parameter { get; }
    }
}