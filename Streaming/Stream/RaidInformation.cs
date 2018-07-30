namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// notice of a raid for a channel
    /// </summary>
    public class RaidInformation {

        /// <summary>
        /// service which triggered the raid
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// login name of raider
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// display name of raider
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// link to avatar picture of raider
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// user color of raider
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// number of raiders
        /// </summary>
        public int RaiderCount { get; set; }

        /// <summary>
        /// room id where raiders joined
        /// </summary>
        public string RoomID { get; set; }

        /// <summary>
        /// channel which is raided
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// custom system message
        /// </summary>
        public string SystemMessage { get; set; }
    }
}