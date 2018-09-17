namespace StreamRC.Streaming.Users.Commands {

    /// <summary>
    /// custom command for stream chat
    /// </summary>
    public class CustomCommand {

        /// <summary>
        /// command in chat
        /// </summary>
        public string ChatCommand { get; set; }

        /// <summary>
        /// command to execute
        /// </summary>
        public string SystemCommand { get; set; }

        /// <summary>
        /// permissions required to execute command
        /// </summary>
        public string Permissions { get; set; }
    }
}