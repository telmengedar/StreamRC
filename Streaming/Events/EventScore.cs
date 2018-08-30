namespace StreamRC.Streaming.Events {

    /// <summary>
    /// score for an event
    /// </summary>
    public class EventScore {

        /// <summary>
        /// id of user which achieved the score
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        /// score for this type
        /// </summary>
        public double Score { get; set; } 
    }
}