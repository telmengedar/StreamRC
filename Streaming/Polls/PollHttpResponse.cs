namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// response for poll request
    /// </summary>
    public class PollHttpResponse {

        /// <summary>
        /// name of poll
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// poll description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// items in poll
        /// </summary>
        public DiagramItem[] Items { get; set; }
    }
}