namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// donation of a user
    /// </summary>
    public class MicroPresent {

        public string Service { get; set; }

        public string Username { get; set; }

        /// <summary>
        /// currency of the present (bits for twitch)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// amount transfered
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// message sent with bits
        /// </summary>
        public string Message { get; set; }
    }
}