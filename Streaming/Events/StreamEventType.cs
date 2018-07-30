namespace StreamRC.Streaming.Events {

    /// <summary>
    /// possible stream event types
    /// </summary>
    public enum StreamEventType {

        /// <summary>
        /// custom event title and text
        /// </summary>
        Custom=0,

        /// <summary>
        /// user hosted the stream
        /// </summary>
        Host=1,

        /// <summary>
        /// user followed the stream
        /// </summary>
        Follow=2,

        /// <summary>
        /// user subscribed to the stream
        /// </summary>
        Subscription=3,

        /// <summary>
        /// user donated to the stream
        /// </summary>
        Donation=4,

        /// <summary>
        /// user pointed out a bug in some software
        /// </summary>
        BugReport=5,

        /// <summary>
        /// user raided the stream
        /// </summary>
        Raid=6
    }
}