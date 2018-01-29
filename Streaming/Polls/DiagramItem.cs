namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// result of a poll
    /// </summary>
    public class DiagramItem {

        /// <summary>
        /// poll option for which was voted
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// item weight
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// weight in percent (0..1)
        /// </summary>
        public float Percentage { get; set; }
    }
}