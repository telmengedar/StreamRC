namespace StreamRC.Streaming.Events {

    /// <summary>
    /// highlighted events of stream
    /// </summary>
    public class StreamHttpEvents {

        /// <summary>
        /// leader of last month
        /// </summary>
        public StreamHttpEvent Leader { get; set; }

        /// <summary>
        /// biggest donor of last month
        /// </summary>
        public StreamHttpEvent Donor { get; set; }

        /// <summary>
        /// biggest hoster of last month
        /// </summary>
        public StreamHttpEvent Hoster { get; set; }

        /// <summary>
        /// most social actions of last month
        /// </summary>
        public StreamHttpEvent Social { get; set; }

        /// <summary>
        /// biggest supporter (bugreports etc.) of last month
        /// </summary>
        public StreamHttpEvent Support { get; set; }

        /// <summary>
        /// last event occured in stream
        /// </summary>
        public StreamHttpEvent LastEvent { get; set; }

        public StreamHttpEvent LastDonation { get; set; }
    }
}