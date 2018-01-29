namespace StreamRC.Streaming.Stream {

    /// <summary>
    /// information about user
    /// </summary>
    public class UserInformation {

        /// <summary>
        /// streaming service
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// name of user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// link to avatar
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// color for username representation
        /// </summary>
        public string Color { get; set; }
    }
}