namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// command in a stream
    /// </summary>
    public class StreamCommand {

        /// <summary>
        /// service command was sent from
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// user which sent the command
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// command name
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// arguments to command
        /// </summary>
        public string[] Arguments { get; set; }

        /// <summary>
        /// determines whether the command was whispered
        /// </summary>
        public bool IsWhispered { get; set; }

        public override string ToString() {
            return $"{User}: !{Command} {string.Join(" ", Arguments)}";
        }
    }
}